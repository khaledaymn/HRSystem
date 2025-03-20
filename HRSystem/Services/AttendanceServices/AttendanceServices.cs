using HRSystem.DataBase;
using HRSystem.Models;
using HRSystem.DTO;
using Microsoft.EntityFrameworkCore;

namespace HRSystem.Services.AttendanceServices
{

    public class AttendanceServices : IAttendanceServices
    {
        private readonly ApplicationDbContext _context;

        public AttendanceServices(ApplicationDbContext context) => _context = context;

        #region Take Attendance

        public async Task<bool> AddAttendance(AttendanceDto model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            var attendance = new Attendance
            {
                TimeOfAttend = model.TimeOfAttend,
                Latitude = model.Latitude,
                Longitude = model.Longitude,
                Radius = model.Radius,  
                EmployeeId = model.EmployeeId
            };

            try
            {
                await _context.Attendance.AddAsync(attendance);
                var result = await _context.SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding attendance: {ex.Message}");
                return false;
            }
        }

        #endregion


        #region Get All Attendances

        public async Task<IEnumerable<AttendanceDto>> GetAllAttendancesAsync()
        {
            return await _context.Attendance
                .Include(e => e.Employee) 
                .OrderBy(e => e.Employee.Name)
                .AsNoTracking()
                .Select(a => new AttendanceDto
                {
                    TimeOfAttend = a.TimeOfAttend,
                    Latitude = a.Latitude,
                    Longitude = a.Longitude,
                    Radius = a.Radius,
                    EmployeeId = a.EmployeeId
                })
                .ToListAsync();
        }

        #endregion


        #region Get Employee Attendances

        public async Task<IEnumerable<AttendanceDto>> GetEmployeeAttendancesAsync(string empId)
        {
            if (string.IsNullOrWhiteSpace(empId))
                return Enumerable.Empty<AttendanceDto>(); 

            return await _context.Attendance
                .Where(a => a.EmployeeId == empId)
                .AsNoTracking()
                .Select(a => new AttendanceDto
                {
                    TimeOfAttend = a.TimeOfAttend,
                    Latitude = a.Latitude,
                    Longitude = a.Longitude,
                    Radius = a.Radius,
                    EmployeeId = a.EmployeeId
                })
                .ToListAsync();
        }

        #endregion


    }

}
