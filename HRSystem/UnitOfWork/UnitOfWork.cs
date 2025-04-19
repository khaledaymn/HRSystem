#region Usings

using HRSystem.DataBase;
using HRSystem.Extend;
using HRSystem.Repository;
using HRSystem.Services.AttendanceServices;
using HRSystem.Services.AuthenticationServices;
using HRSystem.Services.BranchServices;
using HRSystem.Services.EmailServices;
using HRSystem.Services.OfficialVacationServices;
using HRSystem.Services.RolesServices;
using HRSystem.Services.ShiftServices;
using HRSystem.Services.UsersServices;
using HRSystem.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

#endregion

namespace HRSystem.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork, IDisposable
    {
        #region Private Properties

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IEmailServices _emailServices;
        private readonly IOptions<AdminLogin> _adminLogin;
        private readonly IOptions<JWT> _jwt;
        private readonly ILogger<UnitOfWork> _logger;
        private readonly ConcurrentDictionary<string, object> _repositories;

        #region Services

        private IAuthenticationServices _authenticationService;

        private IRolesServices _rolesServices;

        private IUsersServices _usersServices;

        private IBranchServices _branchServices;

        private IShiftServices _shiftServices;

        private IOfficialVacationServices _officialVacationServices;

        private IAttendanceAndLeaveServices _attendanceAndLeaveServices;
        #endregion

        #endregion


        #region Public Properties

        #region Authentication Service
        public IAuthenticationServices AuthenticationService
        {
            get
            {
                if (_authenticationService == null)
                {
                    _authenticationService = new AuthenticationServices(
                        _userManager,
                        _signInManager,
                        _emailServices,
                        _adminLogin,
                        _jwt,
                        _usersServices,
                        _context,
                        new Logger<AuthenticationServices>(new LoggerFactory())
                    );
                }
                return _authenticationService;
            }
        }

        #endregion

        #region Roles Service

        public IRolesServices RolesServices
        {
            get
            {
                if (_rolesServices == null)
                {
                    _rolesServices = new RolesServices(
                        _roleManager,
                        _userManager,
                        this,
                        new Logger<RolesServices>(new LoggerFactory())
                    );
                }
                return _rolesServices;
            }
        }

        #endregion

        #region Users Service

        public IUsersServices UsersServices
        {
            get
            {
                if (_usersServices == null)
                {
                    _usersServices = new UsersServices(
                        _userManager,
                       this,
                       _roleManager,
                       new Logger<UsersServices>(new LoggerFactory())
                    );
                }
                return _usersServices;
            }
        }

        #endregion

        #region Branch Service

        public IBranchServices BranchServices
        {
            get
            {
                if (_branchServices == null)
                {
                    _branchServices = new BranchServices(
                        this, new Logger<BranchServices>(new LoggerFactory()));
                }
                return _branchServices;
            }
        }

        #endregion

        #region Shift Service

        public IShiftServices ShiftServices
        {
            get
            {
                if (_shiftServices == null)
                {
                    _shiftServices = new ShiftServices(
                        new Logger<ShiftServices>(new LoggerFactory()), this);
                }
                return _shiftServices;
            }
        }

        #endregion

        #region Official Vacation Service

        public IOfficialVacationServices OfficialVacationServices
        {
            get
            {
                if (_officialVacationServices == null)
                {
                    _officialVacationServices = new OfficialVacationServices(
                    _context,this, new Logger<OfficialVacationServices>(new LoggerFactory()));
                }
                return _officialVacationServices;
            }
        }
        #endregion

        #region Attendance Service

        public IAttendanceAndLeaveServices AttendanceAndLeaveServices
        {
            get
            {
                if (_attendanceAndLeaveServices == null)
                {
                    _attendanceAndLeaveServices = new AttendanceAndLeaveServices(_context, this,
                        new Logger<AttendanceAndLeaveServices>(new LoggerFactory()));
                }
                return _attendanceAndLeaveServices;
            }
        }

        #endregion

        #endregion


        #region Constructor

        public UnitOfWork(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailServices emailServices,
            IOptions<AdminLogin> adminLogin,
            IOptions<JWT> jwt,
            ILogger<UnitOfWork> logger,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _roleManager = roleManager;
            _userManager = userManager;
            _signInManager = signInManager;
            _emailServices = emailServices;
            _adminLogin = adminLogin;
            _jwt = jwt;
            _logger = logger;
            _repositories = new ConcurrentDictionary<string, object>();
        }


        #endregion


        #region Repository

        public IGenaricRepo<T> Repository<T>() where T : class
        {
            _logger.LogInformation("Attempting to retrieve or create repository for entity type: {EntityType}", typeof(T).Name);

            try
            {
                // Use the entity type name as the key and create a new repository if it doesn't exist
                var repository = (IGenaricRepo<T>)_repositories.GetOrAdd(typeof(T).Name, _ => new GenaricRepo<T>(_context));

                _logger.LogDebug("Successfully retrieved or created repository for entity type: {EntityType}", typeof(T).Name);
                return repository;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve or create repository for entity type: {EntityType}. Error: {Message}",
                    typeof(T).Name, ex.Message);
                throw;
            }
        }

        #endregion


        #region Begin Transaction

        private IDbContextTransaction _transaction;
        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            try
            {
                if (_context == null)
                {
                    _logger.LogError("Database context is null.");
                    throw new InvalidOperationException("Database context is not initialized.");
                }

                if (_context.Database == null)
                {
                    _logger.LogError("Database connection is null.");
                    throw new InvalidOperationException("Database connection is not initialized.");
                }
                if (_transaction != null)
                {
                    _logger.LogWarning("A transaction is already active. Returning the existing transaction.");
                    return _transaction;
                }
                _transaction = await _context.Database.BeginTransactionAsync();
                _logger.LogInformation("Transaction started successfully.");
                return _transaction;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while starting a transaction.");
                throw new Exception("An error occurred while starting a transaction.", ex);
            }
        }

        #endregion


        #region Commit Transaction

        public async Task CommitAsync()
        {
            try
            {
                await _context.Database.CommitTransactionAsync();
                _logger.LogInformation("Transaction committed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while committing the transaction.");
                throw new Exception("An error occurred while committing the transaction.", ex);
            }
        }

        #endregion


        #region Rollback Transaction

        public async Task RollbackAsync()
        {
            try
            {
                await _context.Database.RollbackTransactionAsync();
                _logger.LogInformation("Transaction rolled back successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while rolling back the transaction.");
                throw new Exception("An error occurred while rolling back the transaction.", ex);
            }
        }

        #endregion


        #region Save Changes
        public async Task<int> Save()
        {
            try
            {
                int result = await _context.SaveChangesAsync();
                _logger.LogInformation("Changes saved successfully. Rows affected: {Rows}", result);
                return result;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error occurred while saving changes.");
                throw new Exception("Concurrency error occurred while saving changes.", ex);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database update error occurred while saving changes.");
                throw new Exception("An error occurred while updating the database.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while saving changes.");
                throw new Exception("An unexpected error occurred while saving changes.", ex);
            }
        }


        #endregion


        #region Get DbContext

        public IdentityDbContext<ApplicationUser> GetDbContext()
        {
            return _context;
        }

        #endregion


        #region Dispose
        public void Dispose()
        {
            if (_context != null)
            {
                _logger.LogDebug("Disposing UnitOfWork and database context.");
                _context.Dispose();
            }
        }

        #endregion
    }
}