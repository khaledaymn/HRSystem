using HRSystem.DataBase;
using HRSystem.Models;
using HRSystem.DTO;
using Microsoft.EntityFrameworkCore;

namespace HRSystem.Services.LeaveServices
{
    public class LeaveServices : ILeaveServices
    {
        private readonly ApplicationDbContext _context;
        public LeaveServices(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Take Leave

        public async Task<bool> AddLeave(LeaveDTO model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            var leave = new Leave
            {
                TimeOfLeave = model.Time,
                Latitude = model.Latitude,
                Longitude = model.Longitude,
                Radius = model.Radius,
                EmployeeId = model.EmployeeId
            };

            try
            {
                await _context.Leave.AddAsync(leave);
                var result = await _context.SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        #endregion


        #region Get all Leaves

        public async Task<IEnumerable<LeaveDTO>> GetAllLeaves()
        {
            return await _context.Leave
                .Include(e => e.Employee) 
                .OrderBy(e => e.Employee.Name)
                .AsNoTracking()
                .Select(l => new LeaveDTO
                {
                    Time = l.TimeOfLeave,
                    Latitude = l.Latitude,
                    Longitude = l.Longitude,
                    Radius = l.Radius,
                    EmployeeId = l.EmployeeId
                })
                .ToListAsync();
        }

        #endregion


        #region Get Employee Leaves

        public async Task<IEnumerable<LeaveDTO>> GetEmployeeLeaves(string empId)
        {
            return await _context.Leave
                .Where(a => a.EmployeeId == empId)
                .AsNoTracking()
                .Select(l => new LeaveDTO
                {
                    Time = l.TimeOfLeave,
                    Latitude = l.Latitude,
                    Longitude = l.Longitude,
                    Radius = l.Radius,
                    EmployeeId = l.EmployeeId
                })
                .ToListAsync();
        }

        #endregion

    }
}
