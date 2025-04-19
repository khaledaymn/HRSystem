using HRSystem.DataBase;
using HRSystem.Models;
using HRSystem.DTO;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using HRSystem.Helper;
using HRSystem.UnitOfWork;

namespace HRSystem.Services.AttendanceServices
{
    public class AttendanceAndLeaveServices : IAttendanceAndLeaveServices
    {
        private readonly ApplicationDbContext _context;
        private readonly IUnitOfWork _unitOfWork;
        private readonly Logger<AttendanceAndLeaveServices> _logger;
        public AttendanceAndLeaveServices(ApplicationDbContext context, IUnitOfWork unitOfWork, Logger<AttendanceAndLeaveServices> logger)
        {
            _context = context;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        #region Take Attendance
        public async Task<bool> AddAttendance(AttendanceDTO model)
        {
            if (model == null)
            {
                _logger.LogWarning("AddAttendance: Received null AttendanceDTO.");
                throw new ArgumentNullException(nameof(model));
            }

            if (!DateTime.TryParseExact(model.TimeOfAttend, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime timeOfAttend))
            {
                _logger.LogWarning("AddAttendance: Invalid time format for EmployeeId: {EmployeeId}, Time: {TimeOfAttend}", model.EmployeeId, model.TimeOfAttend);
                throw new FormatException("Invalid time format. Use HH:mm (24-hour).");
            }

            var employee = await _context.Users
                            .FirstOrDefaultAsync(e => e.Id == model.EmployeeId);

            if (employee == null)
            {
                _logger.LogWarning("AddAttendance: Employee with ID {EmployeeId} does not exist.", model.EmployeeId);
                throw new InvalidOperationException("Employee does not exist.");
            }

            var employeeShifts = employee.EmployeeShifts;
            if (employeeShifts == null || !employeeShifts.Any() || employeeShifts.All(es => es.Shift == null))
            {
                _logger.LogWarning("AddAttendance: Employee {EmployeeId} is not assigned to any shift.", model.EmployeeId);
                throw new InvalidOperationException("Employee is not assigned to any shift.");
            }

            var attendanceTime = timeOfAttend.TimeOfDay;
            var earlyArrivalWindow = TimeSpan.FromMinutes(30);
            var lateAttendanceCutoff = TimeSpan.FromHours(1);

            bool isValidShiftTime = employeeShifts.Any(es =>
            {
                var shiftStartTime = es.Shift.StartTime.TimeOfDay;
                var shiftEndTime = es.Shift.EndTime.TimeOfDay;

                if (shiftEndTime < shiftStartTime)
                    shiftEndTime = shiftEndTime.Add(TimeSpan.FromDays(1));

                var earliestAllowedTime = shiftStartTime - earlyArrivalWindow;
                var latestAllowedTime = shiftEndTime - lateAttendanceCutoff;

                return attendanceTime >= earliestAllowedTime && attendanceTime <= latestAllowedTime;
            });

            if (!isValidShiftTime)
            {
                _logger.LogWarning("AddAttendance: Attendance time {AttendanceTime} for EmployeeId: {EmployeeId} is outside allowed window (30 minutes before shift start to 1 hour before shift end).",
                    attendanceTime, model.EmployeeId);
                throw new InvalidOperationException("Attendance must be recorded between 30 minutes before the shift starts and 1 hour before the shift ends.");
            }

            if (employee.BranchId == null || employee.Branch == null)
            {
                _logger.LogWarning("AddAttendance: Employee {EmployeeId} is not assigned to any branch.", model.EmployeeId);
                throw new InvalidOperationException("Employee is not assigned to any branch.");
            }

            bool isInsideBranch = GeoLocationChecker.IsPointInCircle(
                employee.Branch.Latitude, employee.Branch.Longitude,
                model.Latitude, model.Longitude,
                employee.Branch.Radius);

            if (!isInsideBranch)
            {
                _logger.LogWarning("AddAttendance: Employee {EmployeeId} is not within branch area. User Location: ({Latitude}, {Longitude}), Branch: ({BranchLat}, {BranchLon}), Radius: {Radius}m",
                    model.EmployeeId, model.Latitude, model.Longitude, employee.Branch.Latitude, employee.Branch.Longitude, employee.Branch.Radius);
                throw new InvalidOperationException("Employee is not within the branch's designated area.");
            }

            var attendance = new AttendanceAndLeave
            {
                Time = timeOfAttend,
                Latitude = model.Latitude,
                Longitude = model.Longitude,
                EmployeeId = model.EmployeeId,
                Type = "Attendance"
            };

            try
            {
                await _unitOfWork.Repository<AttendanceAndLeave>().ADD(attendance);
                var result = await _unitOfWork.Save();
                if (result > 0)
                    _logger.LogInformation("AddAttendance: Successfully recorded attendance for EmployeeId: {EmployeeId} at {Time}", model.EmployeeId, timeOfAttend);
                else
                    _logger.LogWarning("AddAttendance: Failed to save attendance for EmployeeId: {EmployeeId}", model.EmployeeId);

                return result > 0;
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "AddAttendance: Database error while saving attendance for EmployeeId: {EmployeeId}", model.EmployeeId);
                throw new InvalidOperationException("An error occurred while saving attendance. Please try again.", dbEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AddAttendance: Unexpected error while processing attendance for EmployeeId: {EmployeeId}", model.EmployeeId);
                throw new Exception("An unexpected error occurred while processing your request.", ex);
            }
        }

        #endregion


        #region Take Leave

        public async Task<bool> AddLeave(LeaveDTO model)
        {
            if (model == null)
            {
                _logger.LogWarning("AddLeave: Received null LeaveDTO.");
                throw new ArgumentNullException(nameof(model));
            }

            if (!DateTime.TryParseExact(model.TimeOfLeave, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime timeOfLeave))
            {
                _logger.LogWarning("AddLeave: Invalid time format for EmployeeId: {EmployeeId}, Time: {TimeOfLeave}", model.EmployeeId, model.TimeOfLeave);
                throw new FormatException("Invalid time format. Use HH:mm (24-hour).");
            }

            var employee = await _context.Users
                          .FirstOrDefaultAsync(e => e.Id == model.EmployeeId);

            if (employee == null)
            {
                _logger.LogWarning("AddLeave: Employee with ID {EmployeeId} does not exist.", model.EmployeeId);
                throw new InvalidOperationException("Employee does not exist.");
            }

            if (employee.BranchId == null || employee.Branch == null)
            {
                _logger.LogWarning("AddLeave: Employee {EmployeeId} is not assigned to any branch.", model.EmployeeId);
                throw new InvalidOperationException("Employee is not assigned to any branch.");
            }

            bool isInsideBranch = GeoLocationChecker.IsPointInCircle(
                employee.Branch.Latitude, employee.Branch.Longitude,
                model.Latitude, model.Longitude,
                employee.Branch.Radius);

            if (!isInsideBranch)
            {
                _logger.LogWarning("AddLeave: Employee {EmployeeId} is not within branch area. User Location: ({Latitude}, {Longitude}), Branch: ({BranchLat}, {BranchLon}), Radius: {Radius}m",
                    model.EmployeeId, model.Latitude, model.Longitude, employee.Branch.Latitude, employee.Branch.Longitude, employee.Branch.Radius);
                throw new InvalidOperationException("Employee is not within the branch's designated area.");
            }

            var employeeShifts = employee.EmployeeShifts;
            if (employeeShifts == null || !employeeShifts.Any() || employeeShifts.All(es => es.Shift == null))
            {
                _logger.LogWarning("AddLeave: Employee {EmployeeId} is not assigned to any shift.", model.EmployeeId);
                throw new InvalidOperationException("Employee is not assigned to any shift.");
            }

            var leaveTime = timeOfLeave.TimeOfDay;
            var today = DateTime.UtcNow.Date;

            var earlyArrivalWindow = TimeSpan.FromMinutes(30);
            var lateAttendanceCutoff = TimeSpan.FromHours(1);

            var matchingShift = employeeShifts.FirstOrDefault(es =>
            {
                var shiftStartTime = es.Shift.StartTime.TimeOfDay;
                var shiftEndTime = es.Shift.EndTime.TimeOfDay;
                if (shiftEndTime < shiftStartTime)
                    shiftEndTime = shiftEndTime.Add(TimeSpan.FromDays(1));
                var earliestAllowedTime = shiftStartTime - earlyArrivalWindow;
                var latestAllowedTime = shiftEndTime.Add(TimeSpan.FromHours(24));
                return leaveTime >= earliestAllowedTime && leaveTime <= latestAllowedTime;
            });

            if (matchingShift == null)
            {
                _logger.LogWarning("AddLeave: Leave time {LeaveTime} for EmployeeId: {EmployeeId} does not match any shift.", leaveTime, model.EmployeeId);
                throw new InvalidOperationException("Leave time does not correspond to any of your shifts.");
            }

            var hasAttendedForShift = await _context.AttendancesAndLeaves
                .AnyAsync(a =>
                    a.EmployeeId == model.EmployeeId &&
                    a.Time.Date == today &&
                    a.Type == "Attendance" &&
                    a.Time.TimeOfDay >= (matchingShift.Shift.StartTime.TimeOfDay - earlyArrivalWindow) &&
                    a.Time.TimeOfDay <= (matchingShift.Shift.EndTime.TimeOfDay - lateAttendanceCutoff));

            if (!hasAttendedForShift)
            {
                _logger.LogWarning("AddLeave: Employee {EmployeeId} has not recorded attendance for shift starting at {ShiftStart} on {Date}.",
                    model.EmployeeId, matchingShift.Shift.StartTime, today);
                throw new InvalidOperationException($"You must record attendance for the shift starting at {matchingShift.Shift.StartTime:hh\\:mm} before requesting a leave.");
            }

            var alreadyOnLeaveForShift = await _context.AttendancesAndLeaves
                .AnyAsync(a =>
                    a.EmployeeId == model.EmployeeId &&
                    a.Time.Date == today &&
                    a.Type == "Leave" &&
                    a.Time.TimeOfDay >= (matchingShift.Shift.StartTime.TimeOfDay - earlyArrivalWindow) &&
                    a.Time.TimeOfDay <= matchingShift.Shift.EndTime.TimeOfDay.Add(TimeSpan.FromHours(24)));

            if (alreadyOnLeaveForShift)
            {
                _logger.LogWarning("AddLeave: Employee {EmployeeId} already recorded leave for shift starting at {ShiftStart} on {Date}.",
                    model.EmployeeId, matchingShift.Shift.StartTime, today);
                throw new InvalidOperationException($"Leave for the shift starting at {matchingShift.Shift.StartTime:hh\\:mm} has already been recorded today.");
            }

            var shiftEndTime = matchingShift.Shift.EndTime.TimeOfDay;
            if (matchingShift.Shift.EndTime.TimeOfDay < matchingShift.Shift.StartTime.TimeOfDay)
                shiftEndTime = shiftEndTime.Add(TimeSpan.FromDays(1));

            if (leaveTime < shiftEndTime)
            {
                _logger.LogWarning("AddLeave: Leave time {LeaveTime} for EmployeeId: {EmployeeId} is before shift end time {ShiftEnd}.",
                    leaveTime, model.EmployeeId, shiftEndTime);
                throw new InvalidOperationException($"Leave time must be after the shift end time ({shiftEndTime:hh\\:mm}).");
            }

            var leave = new AttendanceAndLeave
            {
                Time = timeOfLeave,
                Latitude = model.Latitude,
                Longitude = model.Longitude,
                EmployeeId = model.EmployeeId,
                Type = "Leave"
            };

            try
            {
                await _unitOfWork.Repository<AttendanceAndLeave>().ADD(leave);
                var result = await _unitOfWork.Save();
                if (result > 0)
                    _logger.LogInformation("AddLeave: Successfully recorded leave for EmployeeId: {EmployeeId} at {Time}", model.EmployeeId, timeOfLeave);
                else
                    _logger.LogWarning("AddLeave: Failed to save leave for EmployeeId: {EmployeeId}", model.EmployeeId);

                return result > 0;
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "AddLeave: Database error while saving leave for EmployeeId: {EmployeeId}", model.EmployeeId);
                throw new InvalidOperationException("An error occurred while saving leave. Please try again.", dbEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AddLeave: Unexpected error while processing leave for EmployeeId: {EmployeeId}", model.EmployeeId);
                throw new Exception("An unexpected error occurred while processing your request.", ex);
            }
        }

        #endregion


        #region Get All Attendances

        public async Task<IEnumerable<AttendanceWithLeavesDto>> GetAllAttendancesWithLeavesAsync()
        { 

            //var attendances = await _context.Attendance
            //    .AsNoTracking()
            //    .Select(a => new
            //    {
            //        TimeOfAttend = a.Time,
            //        LatitudeOfAttend = a.Latitude,
            //        LongitudeOfLeave = a.Longitude,
            //        EmployeeId = a.EmployeeId,
            //        EmployeeName = a.Employee.Name
            //    })
            //    .ToListAsync();

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
            //var attendances = await _context.Attendance
            //    .Where(a => a.EmployeeId == empId)
            //    .AsNoTracking()
            //    .Select(a => new
            //    {
            //        TimeOfAttend = a.Time,
            //        LatitudeOfAttend = a.Latitude,
            //        LongitudeOfLeave = a.Longitude,
            //        EmployeeId = a.EmployeeId,
            //        EmployeeName = a.Employee.Name
            //    })
            //    .ToListAsync();

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

            //var employeesWithAttendance = await _context.Attendance
            //    .Where(a => a.Time.Date == Day)
            //    .Select(a => a.EmployeeId)
            //    .Distinct()
            //    .AsNoTracking()
            //    .ToListAsync();

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


            //var attendanceRecords = await _context.Attendance
            //.Where(a => employeeIds.Contains(a.EmployeeId) && a.Time.Date == today)
            //.Select(a => new
            //{
            //    a.EmployeeId,
            //    a.Latitude,
            //    a.Longitude
            //})
            //.DistinctBy(a => a.EmployeeId) 
            //.ToListAsync();

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
