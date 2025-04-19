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
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace HRSystem.UnitOfWork
{
    public interface IUnitOfWork
    {
        IAuthenticationServices AuthenticationService { get; }
        IRolesServices RolesServices { get; }
        IUsersServices UsersServices { get; }
        IBranchServices BranchServices { get; }
        IShiftServices ShiftServices { get; }
        IAttendanceAndLeaveServices AttendanceAndLeaveServices { get; }
        IOfficialVacationServices OfficialVacationServices { get; }
        IGenaricRepo<T> Repository<T>() where T : class;
        Task<IDbContextTransaction> BeginTransactionAsync();
        Task CommitAsync();
        Task RollbackAsync();
        Task<int> Save();
        IdentityDbContext<ApplicationUser> GetDbContext();
    }
}
