using HRSystem.DataBase;
using HRSystem.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using HRSystem.Helper;
using HRSystem.UnitOfWork;
using Microsoft.Extensions.Logging;
using HRSystem.DTO.AttendanceDTOs;
using HRSystem.Extend;
using HRSystem.DTO.AttendanceAndLeaveDTOs;

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
            try
            {
                // Validate the input model
                ValidateInputModel(model, model.TimeOfAttend, "AddAttendance", model.EmployeeId);
                // Fetch and validate the employee
                var employee = await GetAndValidateEmployee(model.EmployeeId, "AddAttendance");

                // Validate that the employee is assigned to shifts
                ValidateEmployeeShifts(employee.EmployeeShifts, model.EmployeeId, "AddAttendance");

                var attendanceTime = model.TimeOfAttend.TimeOfDay;
                var attendanceDate = model.TimeOfAttend.Date;
                var earlyArrivalWindow = TimeSpan.FromMinutes(30);
                var lateAttendanceCutoff = TimeSpan.FromHours(1);
                var lateThreshold = TimeSpan.FromMinutes(15);

                // Find the matching shift and check for lateness
                TimeSpan? lateDuration = null;
                EmployeeShift matchingShift = null;
                var shifts = employee.EmployeeShifts.ToList();

                foreach (var es in shifts)
                {
                    var shiftStartTime = es.Shift.StartTime.TimeOfDay;
                    var shiftEndTime = es.Shift.EndTime.TimeOfDay;

                    // Check if shift is overnight (end time before start time)
                    bool isOvernight = shiftEndTime < shiftStartTime;

                    // Define the allowed time window for attendance
                    var earliestAllowedTime = shiftStartTime - earlyArrivalWindow;
                    var latestAllowedTime = isOvernight ? shiftEndTime : shiftEndTime - lateAttendanceCutoff;

                    // Check if attendance time is within the valid window
                    bool isValid = false;
                    if (isOvernight)
                    {
                        // Check if attendance time is in the early arrival window (before shift start)
                        if (attendanceTime >= earliestAllowedTime)
                        {
                            isValid = true; // Attendance before midnight (e.g., 21:45)
                        }
                        // Check if attendance time is after midnight but before shift end
                        else if (attendanceTime <= latestAllowedTime)
                        {
                            isValid = true; // Attendance after midnight (e.g., 00:30)
                        }
                    }
                    else
                    {
                        // Non-overnight shift: check within the same day
                        isValid = attendanceTime >= earliestAllowedTime &&
                                  attendanceTime <= latestAllowedTime;
                    }

                    if (isValid)
                    {
                        // If we already have a candidate shift, compare to pick the most appropriate one
                        if (matchingShift == null || es.Shift.StartTime < matchingShift.Shift.StartTime)
                        {
                            matchingShift = es; // Update to the better matching shift
                                                // Calculate lateness if attendance is after shift start time by more than 15 minutes
                            if (attendanceTime >= shiftStartTime)
                            {
                                var potentialLateDuration = attendanceTime - shiftStartTime;
                                if (potentialLateDuration >= lateThreshold)
                                {
                                    lateDuration = potentialLateDuration;
                                }
                            }
                        }
                    }
                }

                if (matchingShift == null)
                {
                    _logger.LogWarning("AddAttendance: Attendance time {AttendanceTime} on {AttendanceDate} for EmployeeId: {EmployeeId} is outside allowed window (30 minutes before shift start to 1 hour before shift end).",
                        model.TimeOfAttend, model.TimeOfAttend.Date, model.EmployeeId);
                    throw new InvalidOperationException("Attendance must be recorded between 30 minutes before the shift starts and 1 hour before the shift ends.");
                }

                // Validate that the employee has not already recorded attendance for this shift
                var isOvernightShift = matchingShift.Shift.EndTime.TimeOfDay < matchingShift.Shift.StartTime.TimeOfDay;
                var checkDates = isOvernightShift ? new[] { attendanceDate, attendanceDate.AddDays(-1), attendanceDate.AddDays(1) } : new[] { attendanceDate };
                
                //var b =  _context.AttendancesAndLeaves
                //.Where(a =>
                //    a.EmployeeId == model.EmployeeId &&
                //    checkDates.Contains(a.Time.Date) && a.Type == "Attendance");

                //var c = b.Where(a => isOvernightShift &&
                //         (
                //             (a.Time.Date == attendanceDate && a.Time.TimeOfDay >= (matchingShift.Shift.StartTime.TimeOfDay - earlyArrivalWindow)) ||
                //             (a.Time.Date == attendanceDate.AddDays(1) && a.Time.TimeOfDay <= (matchingShift.Shift.EndTime.TimeOfDay <= lateAttendanceCutoff ? matchingShift.Shift.EndTime.TimeOfDay : matchingShift.Shift.EndTime.TimeOfDay - lateAttendanceCutoff))
                //         ));

                var hasAttendedForShift = await _context.AttendancesAndLeaves
                .AnyAsync(a =>
                    a.EmployeeId == model.EmployeeId &&
                    checkDates.Contains(a.Time.Date) &&
                    a.Type == "Attendance" &&
                    (
                        (isOvernightShift &&
                         (
                             (a.Time.Date == attendanceDate && a.Time.TimeOfDay >= (matchingShift.Shift.StartTime.TimeOfDay - earlyArrivalWindow)) ||
                             (a.Time.Date == attendanceDate.AddDays(1) && a.Time.TimeOfDay <= (matchingShift.Shift.EndTime.TimeOfDay <= lateAttendanceCutoff ? matchingShift.Shift.EndTime.TimeOfDay : matchingShift.Shift.EndTime.TimeOfDay - lateAttendanceCutoff))
                         )) ||
                        (!isOvernightShift &&
                         a.Time.TimeOfDay >= (matchingShift.Shift.StartTime.TimeOfDay - earlyArrivalWindow) &&
                         a.Time.TimeOfDay <= (matchingShift.Shift.EndTime.TimeOfDay - lateAttendanceCutoff))
                    ));

                //var c = _context.AttendancesAndLeaves
                //.Where(a =>
                //    a.EmployeeId == model.EmployeeId &&
                //    checkDates.Contains(a.Time.Date) &&
                //    a.Type == "Attendance" &&
                //    (
                //        (isOvernightShift &&
                //         (
                //             (a.Time.Date == attendanceDate && a.Time.TimeOfDay >= (matchingShift.Shift.StartTime.TimeOfDay - earlyArrivalWindow)) ||
                //             (a.Time.Date == attendanceDate.AddDays(1) && a.Time.TimeOfDay <= (matchingShift.Shift.EndTime.TimeOfDay - lateAttendanceCutoff))
                //         )) ||
                //        (!isOvernightShift &&
                //         a.Time.TimeOfDay >= (matchingShift.Shift.StartTime.TimeOfDay - earlyArrivalWindow) &&
                //         a.Time.TimeOfDay <= (matchingShift.Shift.EndTime.TimeOfDay - lateAttendanceCutoff))
                //    ));


                var user = await _unitOfWork.Repository<ApplicationUser>().GetById(model.EmployeeId);

                if (hasAttendedForShift)
                {
                    _logger.LogWarning("AddAttendance: Employee {EmployeeId} has already recorded attendance for shift starting at {ShiftStart} on {Date}.",
                        model.EmployeeId, matchingShift.Shift.StartTime, attendanceDate);
                    throw new InvalidOperationException($"Attendance for the shift starting at {matchingShift.Shift.StartTime:hh\\:mm} has already been recorded with Date {attendanceDate} for Employee {user.Name}.");
                }

                // Validate that the employee is assigned to a branch
                ValidateEmployeeBranch(employee, model.EmployeeId, "AddAttendance");

                // Validate the employee's location
                ValidateEmployeeLocation(employee, model.Latitude, model.Longitude, model.EmployeeId, "AddAttendance");

                // Record the attendance
                var success = await RecordAttendanceOrLeave(model.TimeOfAttend, model.Latitude, model.Longitude, model.EmployeeId, "Attendance", "AddAttendance");
                if (!success)
                    return false;

                // Record lateness if applicable
                if (lateDuration.HasValue && lateDuration.Value > lateThreshold)
                {
                    await RecordLatenessForAttendance(model.EmployeeId, attendanceDate, lateDuration.Value);
                }

                return true;
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "AddAttendance: Database error while saving attendance for EmployeeId: {EmployeeId}", model.EmployeeId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AddAttendance: Unexpected error while processing attendance for EmployeeId: {EmployeeId}", model.EmployeeId);
                throw;
            }
        }

        #endregion


        #region Take Leave
        public async Task<bool> AddLeave(LeaveDTO model)
        {
            try
            {
                // Validate the input model
                ValidateInputModel(model, model.TimeOfLeave, "AddLeave", model.EmployeeId);

                // Fetch and validate the employee
                var employee = await GetAndValidateEmployee(model.EmployeeId, "AddLeave");

                // Validate that the employee is assigned to a branch
                ValidateEmployeeBranch(employee, model.EmployeeId, "AddLeave");

                // Validate the employee's location
                ValidateEmployeeLocation(employee, model.Latitude, model.Longitude, model.EmployeeId, "AddLeave");

                // Validate that the employee is assigned to shifts
                ValidateEmployeeShifts(employee.EmployeeShifts, model.EmployeeId, "AddLeave");

                var leaveTime = model.TimeOfLeave.TimeOfDay;
                var leaveDate = model.TimeOfLeave.Date;
                var earlyArrivalWindow = TimeSpan.FromMinutes(30);
                var lateAttendanceCutoff = TimeSpan.FromHours(1);

                //var shift = employee.EmployeeShifts.FirstOrDefault();
                var shifts = employee.EmployeeShifts.ToList();
                var leaveTimeOfDay = leaveTime; // Extract TimeOfDay from leaveTime for comparison
                EmployeeShift matchingShift = null;

                foreach (var es in shifts)
                {
                    var shiftStartTime = es.Shift.StartTime.TimeOfDay; // e.g., 23:00:00
                    var shiftEndTime = es.Shift.EndTime.TimeOfDay; // e.g., 00:55:00

                    // Check if shift is overnight
                    bool isOvernight = shiftEndTime < shiftStartTime; // true if end time is before start time
                    var adjustedShiftEndTime = isOvernight ? shiftEndTime.Add(TimeSpan.FromHours(24)) : shiftEndTime;

                    // Define the time window for the shift
                    var earliestAllowedTime = shiftEndTime; // e.g., 00:55:00 (day 2 for overnight shift)
                    var NextShift = GetNextShift(employee.Id, es.Shift.StartTime).Result;
                    var latestAllowedTime = NextShift?.StartTime.TimeOfDay ?? adjustedShiftEndTime + TimeSpan.FromHours(2); // Fallback if no previous shift

                    // Check if leaveTime falls within the shift's valid time range
                    if (leaveTimeOfDay >= earliestAllowedTime && leaveTimeOfDay <= latestAllowedTime)
                    {
                        // If we already have a candidate shift, compare to ensure we pick the most appropriate one
                        if (matchingShift == null || es.Shift.StartTime < matchingShift.Shift.StartTime)
                        {
                            matchingShift = es; // Update to the better matching shift (e.g., earlier start time)
                        }
                    }
                }

                // matchingShift now contains the most appropriate shift or null if no shift matches

                if (matchingShift == null)
                {
                    _logger.LogWarning("AddLeave: Leave time {TimeOfLeave} for EmployeeId: {EmployeeId} does not match any shift.", model.TimeOfLeave, model.EmployeeId);
                    throw new InvalidOperationException("Leave time does not correspond to any of your shifts.");
                }

                // Validate that the employee has recorded attendance for this shift
                var isOvernightShift = matchingShift.Shift.EndTime.TimeOfDay < matchingShift.Shift.StartTime.TimeOfDay;
                var checkDates = isOvernightShift ? new[] { leaveDate, leaveDate.AddDays(-1),leaveDate.AddDays(1) } : new[] { leaveDate };


                var hasAttendedForShift = await _context.AttendancesAndLeaves
                .AnyAsync(a =>
                    a.EmployeeId == model.EmployeeId &&
                    a.Type == "Attendance" &&
                    (
                        (isOvernightShift &&
                         (
                             // Check attendance on the previous day (before shift start)
                             (a.Time.Date == leaveDate.AddDays(-1) &&
                              a.Time.TimeOfDay >= (matchingShift.Shift.StartTime.TimeOfDay - earlyArrivalWindow)) ||
                             // Check attendance on the same day (after midnight, before shift end)
                             (a.Time.Date == leaveDate &&
                              a.Time.TimeOfDay <= (matchingShift.Shift.EndTime.TimeOfDay))
                         )) ||
                        (!isOvernightShift &&
                         a.Time.Date == leaveDate &&
                         a.Time.TimeOfDay >= (matchingShift.Shift.StartTime.TimeOfDay - earlyArrivalWindow) &&
                         a.Time.TimeOfDay <= (matchingShift.Shift.EndTime.TimeOfDay))
                    ));

                if (!hasAttendedForShift)
                {
                    _logger.LogWarning("AddLeave: Employee {EmployeeId} has not recorded attendance for shift starting at {ShiftStart} on {Date}.",
                        model.EmployeeId, matchingShift.Shift.StartTime, leaveDate);
                    throw new InvalidOperationException($"You must record attendance for the shift starting at {matchingShift.Shift.StartTime:hh\\:mm} before requesting a leave.");
                }
                var checkDatesForleave = isOvernightShift ? new[] { leaveDate, leaveDate.AddDays(1) } : new[] { leaveDate };

                var nextShift = await GetNextShift(employee.Id, matchingShift.Shift.StartTime);
                // Validate that no leave has been recorded for this shift
                var alreadyOnLeaveForShift = await _context.AttendancesAndLeaves
                    .AnyAsync(a =>
                        a.EmployeeId == model.EmployeeId &&
                        checkDatesForleave.Contains(a.Time.Date) &&
                        a.Type == "Leave" &&
                        a.Time.TimeOfDay >= (matchingShift.Shift.EndTime.TimeOfDay) &&
                        a.Time.TimeOfDay <= nextShift.StartTime.TimeOfDay);

                
                if (alreadyOnLeaveForShift)
                {
                    _logger.LogWarning("AddLeave: Employee {EmployeeId} already recorded leave for shift starting at {ShiftStart} on {Date}.",
                        model.EmployeeId, matchingShift.Shift.StartTime, leaveDate);
                    throw new InvalidOperationException($"Leave for the shift starting at {matchingShift.Shift.StartTime:hh\\:mm} has already been recorded.");
                }

                // Record the leave
                var success = await RecordAttendanceOrLeave(model.TimeOfLeave, model.Latitude, model.Longitude, model.EmployeeId, "Leave", "AddLeave");
                if (!success)
                    return false;

                // Calculate and record overtime for the day after recording the leave
                await RecordOvertimeForDay(model.EmployeeId, leaveDate);

                return true;
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "AddLeave: Database error while saving leave for EmployeeId: {EmployeeId}", model.EmployeeId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AddLeave: Unexpected error while processing leave for EmployeeId: {EmployeeId}", model.EmployeeId);
                throw;
            }
        }

        #endregion


        #region Helper Methods

        #region Record Lateness For Attendance
        private async Task RecordLatenessForAttendance(string employeeId, DateTime attendanceDate, TimeSpan lateDuration)
        {
            try
            {
                // Convert late duration to hours (e.g., 1.25 hours)
                var lateHours = lateDuration.TotalHours;

                // Check if there's an existing late record for the employee on the given date
                var lateRecord = await _context.EmployeeExtraAndLateHours
                    .FirstOrDefaultAsync(o => o.EmployeeId == employeeId && o.Date == attendanceDate && o.Type == "Late");

                if (lateRecord == null)
                {
                    // Create a new late record
                    lateRecord = new EmployeeExtraAndLateHour
                    {
                        EmployeeId = employeeId,
                        Date = attendanceDate,
                        Hours = lateHours,
                        Type = "Late"
                    };
                    await _unitOfWork.Repository<EmployeeExtraAndLateHour>().ADD(lateRecord);
                }
                else
                {
                    // Update the existing late record
                    lateRecord.Hours += lateHours;
                    _context.EmployeeExtraAndLateHours.Update(lateRecord);
                }

                // Save the late record to the database
                await _unitOfWork.Save();
                _logger.LogInformation("Recorded {LateHours} late hours for EmployeeId: {EmployeeId} on {Date}", lateHours, employeeId, attendanceDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RecordLatenessForAttendance: Error while recording lateness for EmployeeId: {EmployeeId} on {Date}", employeeId, attendanceDate);
                throw;
            }
        }
        #endregion

        #region Record OverTime
        private async Task RecordOvertimeForDay(string employeeId, DateTime date)
        {
            try
            {
                // Check if the employee has any overnight shifts
                var hasOvernightShift = await _context.EmployeeShifts
                    .AnyAsync(es => es.EmployeeId == employeeId &&
                                    es.Shift.EndTime.TimeOfDay < es.Shift.StartTime.TimeOfDay);

                // Fetch records for the given date and the next day for overnight shifts
                var datesToCheck = hasOvernightShift ? new[] { date, date.AddDays(1) } : new[] { date };

                var shiftRecords = await _context.AttendancesAndLeaves
                    .Where(a => a.EmployeeId == employeeId &&
                                datesToCheck.Contains(a.Time.Date) &&
                                (a.Type == "Attendance" || a.Type == "Leave"))
                    .OrderBy(a => a.Time)
                    .ToListAsync();

                // Calculate total working hours by pairing Attendance and Leave records
                var shiftsWorked = new List<(DateTime AttendanceTime, DateTime LeaveTime)>();
                DateTime? lastAttendance = null;

                foreach (var record in shiftRecords)
                {
                    if (record.Type == "Attendance")
                    {
                        if (lastAttendance.HasValue)
                        {
                            _logger.LogWarning("RecordOvertimeForDay: Unpaired attendance record for EmployeeId: {EmployeeId} at {Time}", employeeId, record.Time);
                            continue;
                        }
                        lastAttendance = record.Time;
                    }
                    else if (record.Type == "Leave" && lastAttendance.HasValue)
                    {
                        shiftsWorked.Add((lastAttendance.Value, record.Time));
                        lastAttendance = null;
                    }
                }

                if (lastAttendance.HasValue)
                {
                    _logger.LogWarning("RecordOvertimeForDay: Unpaired attendance record for EmployeeId: {EmployeeId} at {Time}", employeeId, lastAttendance.Value);
                }

                // Calculate total working hours for the day
                var totalWorkHours = TimeSpan.Zero;
                foreach (var shift in shiftsWorked)
                {
                    var workDuration = shift.LeaveTime - shift.AttendanceTime;
                    if (workDuration < TimeSpan.Zero)
                        workDuration = workDuration.Add(TimeSpan.FromDays(1));
                    totalWorkHours += workDuration;
                }

                // Fetch the overtime threshold from general settings (default to 10 hours if not specified)
                var NumberOfDayWorkingHours = _context.GeneralSetting?.AsNoTracking()
                    .FirstOrDefault()?.NumberOfDayWorkingHours;
                var overtimeThreshold = TimeSpan.FromHours(NumberOfDayWorkingHours ?? 10);

                // Calculate overtime hours (if total work hours exceed the threshold)
                var overtimeHours = totalWorkHours > overtimeThreshold ? totalWorkHours - overtimeThreshold : TimeSpan.Zero;

                // If there are overtime hours, record them in EmployeeExtraAndLateHour
                if (overtimeHours > TimeSpan.Zero)
                {
                    var overtimeRecord = await _context.EmployeeExtraAndLateHours
                        .FirstOrDefaultAsync(o => o.EmployeeId == employeeId && o.Date == date && o.Type == "OverTime");

                    if (overtimeRecord == null)
                    {
                        // Create a new overtime record
                        overtimeRecord = new EmployeeExtraAndLateHour
                        {
                            EmployeeId = employeeId,
                            Date = date,
                            Hours = overtimeHours.TotalHours,
                            Type = "OverTime"
                        };
                        await _unitOfWork.Repository<EmployeeExtraAndLateHour>().ADD(overtimeRecord);
                    }
                    else
                    {
                        // Update the existing overtime record
                        overtimeRecord.Hours += overtimeHours.TotalHours;
                        _context.EmployeeExtraAndLateHours.Update(overtimeRecord);
                    }

                    // Save the overtime record to the database
                    await _unitOfWork.Save();
                    _logger.LogInformation("Recorded {OvertimeHours} overtime hours for EmployeeId: {EmployeeId} on {Date}", overtimeHours.TotalHours, employeeId, date);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RecordOvertimeForDay: Error while calculating or recording overtime for EmployeeId: {EmployeeId} on {Date}", employeeId, date);
                throw;
            }
        }
        #endregion

        #region Validate Input Model
        private void ValidateInputModel<T>(T model, DateTime eventTime, string eventName, string employeeId = null)
        {
            if (model == null)
            {
                _logger.LogWarning($"{eventName}: Received null DTO.");
                throw new ArgumentNullException(nameof(model));
            }

            if (eventTime == default)
            {
                _logger.LogWarning($"{eventName}: Invalid or missing time for EmployeeId: {employeeId ?? "unknown"}.");
                throw new ArgumentException($"{eventName} time is required.");
            }
        }
        #endregion

        #region Get And ValidateEmployee
        private async Task<ApplicationUser> GetAndValidateEmployee(string employeeId, string eventName)
        {
            var employee = await _context.Users
                .Include(u => u.EmployeeShifts).ThenInclude(es => es.Shift)
                .Include(u => u.Branch)
                .FirstOrDefaultAsync(e => e.Id == employeeId);

            if (employee == null)
            {
                _logger.LogWarning($"{eventName}: Employee with ID {employeeId} does not exist.");
                throw new InvalidOperationException("Employee does not exist.");
            }

            return employee;
        }
        #endregion

        #region Validate Employee Branch
        private void ValidateEmployeeBranch(ApplicationUser employee, string employeeId, string eventName)
        {
            if (employee.BranchId == null || employee.Branch == null)
            {
                _logger.LogWarning($"{eventName}: Employee {employeeId} is not assigned to any branch.");
                throw new InvalidOperationException("Employee is not assigned to any branch.");
            }
        }
        #endregion

        #region Validate Employee Location
        private void ValidateEmployeeLocation(ApplicationUser employee, double latitude, double longitude, string employeeId, string eventName)
        {
            bool isInsideBranch = GeoLocationChecker.IsPointInCircle(
                employee.Branch.Latitude, employee.Branch.Longitude,
                latitude, longitude,
                employee.Branch.Radius);

            if (!isInsideBranch)
            {
                _logger.LogWarning($"{eventName}: Employee {employeeId} is not within branch area. User Location: ({latitude}, {longitude}), Branch: ({employee.Branch.Latitude}, {employee.Branch.Longitude}), Radius: {employee.Branch.Radius}m");
                throw new InvalidOperationException("Employee is not within the branch's designated area.");
            }
        }
        #endregion

        #region Validate Employee Shift
        private void ValidateEmployeeShifts(ICollection<EmployeeShift> employeeShifts, string employeeId, string eventName)
        {
            if (employeeShifts == null || !employeeShifts.Any() || employeeShifts.All(es => es.Shift == null || es.Shift.StartTime == default || es.Shift.EndTime == default))
            {
                _logger.LogWarning($"{eventName}: Employee {employeeId} is not assigned to any valid shift.");
                throw new InvalidOperationException("Employee is not assigned to any valid shift.");
            }
        }
        #endregion

        #region Record Attendance Or Leave
        private async Task<bool> RecordAttendanceOrLeave(DateTime eventTime, double latitude, double longitude, string employeeId, string type, string eventName)
        {
            var record = new AttendanceAndLeave
            {
                Time = eventTime,
                Latitude = latitude,
                Longitude = longitude,
                EmployeeId = employeeId,
                Type = type
            };

            await _unitOfWork.Repository<AttendanceAndLeave>().ADD(record);
            var result = await _unitOfWork.Save();
            if (result <= 0)
            {
                _logger.LogWarning($"{eventName}: Failed to save {type.ToLower()} for EmployeeId: {employeeId}");
                return false;
            }

            _logger.LogInformation($"{eventName}: Successfully recorded {type.ToLower()} for EmployeeId: {employeeId} at {eventTime}", employeeId, eventTime);
            return true;
        }
        #endregion

        #region Get Next Shift
        public async Task<Shift> GetNextShift(string employeeId, DateTime currentShiftStartTime)
        {
            var employeeShiftRepository = _unitOfWork.Repository<EmployeeShift>();
            var employeeShifts = employeeShiftRepository.GetAll().Result
                .Where(es => es.EmployeeId == employeeId)
                .ToList();

            if (!employeeShifts.Any())
                return null;

            var shiftRepository = _unitOfWork.Repository<Shift>();
            var allShifts = await shiftRepository.GetAll();

            var shifts = employeeShifts
                .Where(es => es.Shift != null)
                .Select(es => new
                {
                    Shift = es.Shift,
                    AdjustedStartTime = es.Shift.StartTime < es.Shift.EndTime
                        ? new DateTime(
                            currentShiftStartTime.Year,
                            currentShiftStartTime.Month,
                            currentShiftStartTime.Day,
                            es.Shift.StartTime.Hour,
                            es.Shift.StartTime.Minute,
                            es.Shift.StartTime.Second)
                        : new DateTime(
                            currentShiftStartTime.Year,
                            currentShiftStartTime.Month,
                            currentShiftStartTime.Day,
                            es.Shift.StartTime.Hour,
                            es.Shift.StartTime.Minute,
                            es.Shift.StartTime.Second)
                            .AddDays(currentShiftStartTime.Date == es.Shift.EndTime.Date ? 1 : 0)
                });

            var nextShift = shifts
                .Where(x => x.AdjustedStartTime > currentShiftStartTime)
                .OrderBy(x => x.AdjustedStartTime)
                .Select(x => x.Shift)
                .FirstOrDefault();

            if (nextShift == null)
                nextShift = shifts
                    .Where(x => x.AdjustedStartTime <= currentShiftStartTime)
                    .OrderBy(x => x.AdjustedStartTime)
                    .Select(x => x.Shift)
                    .FirstOrDefault();

            return nextShift;
        }

        #endregion

        #endregion


        #region Add Leave by Admin

        public async Task<bool> AddLeaveByAdmin(LeaveByAdminDTO model)
        {
            if (model == null)
            {
                _logger.LogWarning("AddLeave: Received null LeaveDTO.");
                throw new ArgumentNullException(nameof(model));
            }

            var EmployeeShifts = _unitOfWork.Repository<EmployeeShift>();
            var employee = EmployeeShifts.Filter(es => es.EmployeeId == model.EmployeeId
            && es.ShiftId == model.ShiftId);

            if (employee == null || !employee.Any())
            {
                _logger.LogWarning("AddLeave: Employee with ID {EmployeeId} does not exist.", model.EmployeeId);
                throw new InvalidOperationException("Employee does not exist.");
            }

            var employeeShift = employee.FirstOrDefault();

            var leave = new AttendanceAndLeave
            {
                Time = employeeShift.Shift.EndTime,
                Latitude = employeeShift.Employee.Branch.Latitude,
                Longitude = employeeShift.Employee.Branch.Longitude,
                EmployeeId = model.EmployeeId,
                Type = "Leave"
            };

            try
            {
                await _unitOfWork.Repository<AttendanceAndLeave>().ADD(leave);
                var result = await _unitOfWork.Save();
                if (result <= 0)
                    return false;
                _logger.LogInformation("AddLeave: Successfully recorded leave for EmployeeId: {EmployeeId} at {Time}", model.EmployeeId, employeeShift.Shift.EndTime);
                return true;
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


        #region TO DO

        //#region Get Employees Without Leave

        //public async Task<List<string>> GetEmployeesWithoutLeave(DateTime Day)
        //{

        //    //var employeesWithAttendance = await _context.Attendance
        //    //    .Where(a => a.Time.Date == Day)
        //    //    .Select(a => a.EmployeeId)
        //    //    .Distinct()
        //    //    .AsNoTracking()
        //    //    .ToListAsync();

        //    ////var employeesWithLeave = await _context.Leave
        //    ////    .Where(l => l.TimeOfLeave.Date == Day)
        //    ////    .Select(l => l.EmployeeId)
        //    ////    .Distinct()
        //    ////    .AsNoTracking()
        //    ////    .ToListAsync();

        //    //var employeesWithoutCheckOut = employeesWithAttendance
        //    //    .Except(employeesWithLeave)
        //    //    .ToList();

        //    return new List<string>();
        //}

        //#endregion


        //#region Get Employees Without Leave

        //public async Task MarkEmployeesWithoutLeave(List<string> employeeIds)
        //{
        //    if (employeeIds == null || !employeeIds.Any())
        //        return;

        //    var today = DateTime.UtcNow.Date;

        //    var defaultLeaveTime = await _context.GeneralSetting
        //        .AsNoTracking()
        //        //.Select(s => s.TimeOfLeave)
        //        .FirstOrDefaultAsync();


        //    //var attendanceRecords = await _context.Attendance
        //    //.Where(a => employeeIds.Contains(a.EmployeeId) && a.Time.Date == today)
        //    //.Select(a => new
        //    //{
        //    //    a.EmployeeId,
        //    //    a.Latitude,
        //    //    a.Longitude
        //    //})
        //    //.DistinctBy(a => a.EmployeeId) 
        //    //.ToListAsync();

        //    //var newLeaves = attendanceRecords.Select(record => new Leave
        //    //{
        //    //    EmployeeId = record.EmployeeId,
        //    //    //TimeOfLeave = defaultLeaveTime,
        //    //    Branch = record.Branch,
        //    //    Latitude = record.Latitude,
        //    //    Longitude = record.Longitude
        //    //}).ToList();

        //    //_context.Leave.AddRange(newLeaves);
        //    //await _context.SaveChangesAsync();
        //}


        #endregion


    }
}
