using HRSystem.DataBase;
using HRSystem.Models;
using HRSystem.DTO;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Globalization;

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

            if (!DateTime.TryParseExact(model.TimeOfAttend, "hh:mm tt", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime timeOfAttend))
                throw new FormatException("Invalid time format. Use hh:mm AM/PM.");

            var employeeExists = await _context.Users.AnyAsync(e => e.Id == model.EmployeeId);
            if (!employeeExists)
                throw new InvalidOperationException("Employee does not exist.");

            var today = DateTime.UtcNow.Date;
            var alreadyAttended = await _context.Attendance.AnyAsync(a => a.EmployeeId == model.EmployeeId && a.Time.Date == today);
            if (alreadyAttended)
                throw new InvalidOperationException("Attendance for today has already been recorded.");

            var attendance = new AttendanceAndLeave
            {
                Time = timeOfAttend,
                Latitude = model.Latitude,
                Longitude = model.Longitude,
                EmployeeId = model.EmployeeId,
            };

            try
            {
                await _context.Attendance.AddAsync(attendance);
                var result = await _context.SaveChangesAsync();
                return result > 0;
            }
            catch (DbUpdateException dbEx)
            {
                throw new InvalidOperationException("An error occurred while saving attendance. Please try again.", dbEx);
            }
            catch (Exception ex)
            {
                throw new Exception("An unexpected error occurred while processing your request.", ex);
            }
        }

        #endregion


        #region Get All Attendances

        public async Task<IEnumerable<AttendanceWithLeavesDto>> GetAllAttendancesWithLeavesAsync()
        { 

            var attendances = await _context.Attendance
                .AsNoTracking()
                .Select(a => new
                {
                    TimeOfAttend = a.Time,
                    LatitudeOfAttend = a.Latitude,
                    LongitudeOfLeave = a.Longitude,
                    EmployeeId = a.EmployeeId,
                    EmployeeName = a.Employee.Name
                })
                .ToListAsync();
            ////var Leaves = await _context.Leave
            ////    .AsNoTracking()
            ////    .Select(l => new
            ////    {
            ////        TimeOfLeave = l.TimeOfLeave,
            ////        LatitudeOfLeave = l.Latitude,
            ////        LongitudeOfLeave = l.Longitude,
            ////        Branch = l.Branch,
            ////        EmployeeId = l.EmployeeId,
            ////        EmployeeName = l.Employee.Name
            ////    })
            ////    .ToListAsync();

            //var result = attendances
            //    .Join(Leaves,
            //        a => a.EmployeeId,
            //        l => l.EmployeeId,
            //        (a, l) => new AttendanceWithLeavesDto
            //        {
            //            TimeOfAttend = a.TimeOfAttend.ToString("HH:MM"),
            //            TimeOfLeave = l.TimeOfLeave.ToString("HH:MM"),
            //            LatitudeOfAttend = a.LatitudeOfAttend,
            //            LatitudeOfLeave = l.LatitudeOfLeave,
            //            LongitudeOfAttend = a.LongitudeOfLeave,
            //            LongitudeOfLeave = l.LongitudeOfLeave,
            //            EmployeeId = a.EmployeeId,
            //            EmployeeName = a.EmployeeName,
            //            Branch = a.Branch
            //        })
            //    .ToList();
            return new List<AttendanceWithLeavesDto>();
        }


        #endregion


        #region Get Employee Attendances And Leaves

        public async Task<IEnumerable<AttendanceWithLeavesDto>> GetEmployeeAttendancesAndLeavesAsync(string empId)
        {
            var attendances = await _context.Attendance
                .Where(a => a.EmployeeId == empId)
                .AsNoTracking()
                .Select(a => new
                {
                    TimeOfAttend = a.Time,
                    LatitudeOfAttend = a.Latitude,
                    LongitudeOfLeave = a.Longitude,
                    EmployeeId = a.EmployeeId,
                    EmployeeName = a.Employee.Name
                })
                .ToListAsync();
            //var Leaves = await _context.Leave
            //    .Where(l => l.EmployeeId == empId)
            //    .AsNoTracking()
            //    .Select(l => new
            //    {
            //        TimeOfLeave = l.TimeOfLeave,
            //        LatitudeOfLeave = l.Latitude,
            //        LongitudeOfLeave = l.Longitude,
            //        Branch = l.Branch,
            //        EmployeeId = l.EmployeeId,
            //        EmployeeName = l.Employee.Name
            //    })
            //    .ToListAsync();

            //var result = attendances
            //    .Join(Leaves,
            //        a => a.EmployeeId,
            //        l => l.EmployeeId,
            //        (a, l) => new AttendanceWithLeavesDto
            //        {
            //            TimeOfAttend = a.TimeOfAttend.ToString("HH:MM"),
            //            TimeOfLeave = l.TimeOfLeave.ToString("HH:MM"),
            //            LatitudeOfAttend = a.LatitudeOfAttend,
            //            LatitudeOfLeave = l.LatitudeOfLeave,
            //            LongitudeOfAttend = a.LongitudeOfLeave,
            //            LongitudeOfLeave = l.LongitudeOfLeave,
            //            EmployeeId = a.EmployeeId,
            //            EmployeeName = a.EmployeeName,
            //            Branch = a.Branch
            //        })
            //    .ToList();
            return new List<AttendanceWithLeavesDto>();
        }

        #endregion


        #region Get Employees Without Leave

        public async Task<List<string>> GetEmployeesWithoutLeave(DateTime Day)
        {

            var employeesWithAttendance = await _context.Attendance
                .Where(a => a.Time.Date == Day)
                .Select(a => a.EmployeeId)
                .Distinct()
                .AsNoTracking()
                .ToListAsync();

            ////var employeesWithLeave = await _context.Leave
            ////    .Where(l => l.TimeOfLeave.Date == Day)
            ////    .Select(l => l.EmployeeId)
            ////    .Distinct()
            ////    .AsNoTracking()
            ////    .ToListAsync();

            //var employeesWithoutCheckOut = employeesWithAttendance
            //    .Except(employeesWithLeave)
            //    .ToList();

            return new List<string>();
        }

        #endregion


        #region Get Employees Without Leave

        public async Task MarkEmployeesWithoutLeave(List<string> employeeIds)
        {
            if (employeeIds == null || !employeeIds.Any())
                return;

            var today = DateTime.UtcNow.Date;

            var defaultLeaveTime = await _context.GeneralSetting
                .AsNoTracking()
                //.Select(s => s.TimeOfLeave)
                .FirstOrDefaultAsync();


            var attendanceRecords = await _context.Attendance
            .Where(a => employeeIds.Contains(a.EmployeeId) && a.Time.Date == today)
            .Select(a => new
            {
                a.EmployeeId,
                a.Latitude,
                a.Longitude
            })
            .DistinctBy(a => a.EmployeeId) 
            .ToListAsync();

            //var newLeaves = attendanceRecords.Select(record => new Leave
            //{
            //    EmployeeId = record.EmployeeId,
            //    //TimeOfLeave = defaultLeaveTime,
            //    Branch = record.Branch,
            //    Latitude = record.Latitude,
            //    Longitude = record.Longitude
            //}).ToList();

            //_context.Leave.AddRange(newLeaves);
            //await _context.SaveChangesAsync();
        }


        #endregion
    }

}
