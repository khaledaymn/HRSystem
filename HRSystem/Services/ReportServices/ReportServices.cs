using HRSystem.DataBase;
using HRSystem.DTO.AttendanceDTOs;
using HRSystem.DTO.EmployeeDTOs;
using HRSystem.DTO.ReportDTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace HRSystem.Services.ReportServices
{
    public class ReportServices : IReportServices
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReportServices> _logger;

        public ReportServices(ApplicationDbContext context, ILogger<ReportServices> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Get Overtime Report

        public async Task<List<ReportDTO>> GetMonthlyOvertimeReport()
        {
            var today = DateTime.Now.Date;
            var startDate = today.AddMonths(-1);
            var endDate = today;

            try
            {
                var overtimeRecords = await _context.EmployeeExtraAndLateHours
                    .Where(o => o.Date >= startDate && o.Date <= endDate)
                    .GroupBy(o => o.EmployeeId)
                    .Select(g => new ReportDTO
                    {
                        EmployeeId = g.Key,
                        EmployeeName = g.First().Employee.Name,
                        TotalHours = g.Sum(o => o.Hours)
                    })
                    .ToListAsync();

                _logger.LogInformation("Retrieved overtime report for period {StartDate} to {EndDate}. Found {Count} employees with overtime.", startDate, endDate, overtimeRecords.Count);
                return overtimeRecords;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving monthly overtime report for period {StartDate} to {EndDate}", startDate, endDate);
                throw new Exception("An error occurred while retrieving the overtime report.", ex);
            }
        }

        #endregion


        #region Get Late Times

        public async Task<List<ReportDTO>> GetMonthlyLateReport()
        {
            var today = DateTime.Now.Date;
            var startDate = today.AddMonths(-1);
            var endDate = today;
            var lateThreshold = TimeSpan.FromHours(1);

            try
            {
                var lateRecords = await _context.Users
                    .GroupJoin(_context.AttendancesAndLeaves
                        .Where(a => a.Type == "Attendance" && a.Time.Date >= startDate && a.Time.Date <= endDate),
                        u => u.Id,
                        a => a.EmployeeId,
                        (u, a) => new { User = u, Attendances = a })
                    .SelectMany(x => x.Attendances.DefaultIfEmpty(),
                        (u, a) => new { User = u.User, Attendance = a })
                    .Join(_context.EmployeeShifts,
                        x => x.User.Id,
                        es => es.EmployeeId,
                        (x, es) => new { x.User, x.Attendance, EmployeeShift = es })
                    .Join(_context.Shifts,
                        x => x.EmployeeShift.ShiftId,
                        s => s.Id,
                        (x, s) => new
                        {
                            x.User.Id,
                            x.User.Name,
                            AttendanceTime = x.Attendance != null ? x.Attendance.Time : (DateTime?)null,
                            ShiftStartTime = s.StartTime
                        })
                    .GroupBy(x => new { x.Id, x.Name })
                    .Select(g => new ReportDTO
                    {
                        EmployeeId = g.Key.Id,
                        EmployeeName = g.Key.Name,
                        TotalHours = g.Sum(x =>
                            x.AttendanceTime.HasValue &&
                            x.AttendanceTime.Value.TimeOfDay > (x.ShiftStartTime.TimeOfDay + lateThreshold) &&
                            x.AttendanceTime.Value.TimeOfDay >= (x.ShiftStartTime.TimeOfDay - TimeSpan.FromMinutes(30)) &&
                            x.AttendanceTime.Value.TimeOfDay <= (x.ShiftStartTime.TimeOfDay + TimeSpan.FromHours(24))
                            ? (x.AttendanceTime.Value.TimeOfDay - (x.ShiftStartTime.TimeOfDay + lateThreshold)).TotalHours
                            : 0.0)
                    })
                    .ToListAsync();

                _logger.LogInformation("Retrieved late report for period {StartDate} to {EndDate}. Found {Count} employees.", startDate, endDate, lateRecords.Count);
                return lateRecords;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving monthly late report for period {StartDate} to {EndDate}", startDate, endDate);
                throw new Exception("An error occurred while retrieving the late report.", ex);
            }
        }

        #endregion


        #region Get All Attendances And Leaves For Employee

        public async Task<List<AttendanceLeaveReportDTO>> GetAttendanceAndLeaveReport(AttendanceAndLeaveReportDTO dto)
        {
            try
            {
                // Validate input
                if (dto == null)
                {
                    _logger.LogWarning("GetAttendanceAndLeaveReport: Received null AttendanceAndLeaveReportDTO.");
                    throw new ArgumentNullException(nameof(dto));
                }

                if (dto.PageSize <= 0)
                {
                    _logger.LogWarning("GetAttendanceAndLeaveReport: Invalid PageSize {PageSize}.", dto.PageSize);
                    throw new ArgumentException("PageSize must be greater than zero.");
                }

                if (dto.PageNumber <= 0)
                {
                    _logger.LogWarning("GetAttendanceAndLeaveReport: Invalid PageNumber {PageNumber}.", dto.PageNumber);
                    throw new ArgumentException("PageNumber must be greater than zero.");
                }

                // Determine date range based on ReportType
                DateTime startDate;
                DateTime endDate;

                if (dto.ReportType == ReportType.Daily)
                {
                    startDate = dto.DayDate?.Date ?? DateTime.Now.Date;
                    endDate = startDate;
                }
                else if (dto.ReportType == ReportType.Monthly)
                {
                    if (dto.Month.HasValue)
                    {
                        int year = DateTime.Now.Year; // Default to current year
                        if (dto.FromDate.HasValue)
                            year = dto.FromDate.Value.Year;
                        startDate = new DateTime(year, dto.Month.Value, 1);
                        endDate = startDate.AddMonths(1).AddDays(-1);
                    }
                    else if (dto.FromDate.HasValue && dto.ToDate.HasValue)
                    {
                        if (dto.ToDate < dto.FromDate)
                        {
                            _logger.LogWarning("GetAttendanceAndLeaveReport: ToDate {ToDate} is before FromDate {FromDate}.", dto.ToDate, dto.FromDate);
                            throw new ArgumentException("ToDate must be greater than or equal to FromDate.");
                        }
                        startDate = dto.FromDate.Value.Date;
                        endDate = dto.ToDate.Value.Date;
                    }
                    else
                    {
                        // Default to current month
                        startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                        endDate = startDate.AddMonths(1).AddDays(-1);
                    }
                }
                else
                {
                    if (dto.FromDate.HasValue && dto.ToDate.HasValue)
                    {
                        if (dto.ToDate < dto.FromDate)
                        {
                            _logger.LogWarning("GetAttendanceAndLeaveReport: ToDate {ToDate} is before FromDate {FromDate}.", dto.ToDate, dto.FromDate);
                            throw new ArgumentException("ToDate must be greater than or equal to FromDate.");
                        }
                        startDate = dto.FromDate.Value.Date;
                        endDate = dto.ToDate.Value.Date;
                    }
                    else
                    {
                        _logger.LogWarning("GetAttendanceAndLeaveReport: ReportType is not specified.");
                        throw new ArgumentException("ReportType must be specified (Daily or Monthly).");
                    }
                }

                // Build query for attendance and leave records
                var query = _context.AttendancesAndLeaves
                        .Where(al => al.Time.Date >= startDate && al.Time.Date <= endDate);

                // Filter by EmployeeId if provided
                if (!string.IsNullOrEmpty(dto.EmployeeId))
                {
                    query = query.Where(x => x.Employee.Id == dto.EmployeeId);
                }

                // Group by EmployeeId and Date to aggregate attendance and leave
                var groupedQuery = from al in query
                                   group al by new { al.EmployeeId, al.Time.Date } into g
                                   select new
                                   {
                                       EmployeeId = g.Key.EmployeeId,
                                       Date = g.Key.Date,
                                       TimeOfAttend = g.Where(x => x.Type == "Attendance").Select(x => x.Time).FirstOrDefault(),
                                       TimeOfLeave = g.Where(x => x.Type == "Leave").Select(x => x.Time).FirstOrDefault()
                                   };

                // Apply pagination
                var totalRecords = await groupedQuery.CountAsync();
                var pagedQuery = groupedQuery
                    .OrderBy(x => x.Date)
                    .ThenBy(x => x.EmployeeId)
                    .Skip((dto.PageNumber - 1) * dto.PageSize)
                    .Take(dto.PageSize);

                // Execute query
                var records = await pagedQuery.ToListAsync();

                // Fetch additional data using Lazy Loading
                var report = new List<AttendanceLeaveReportDTO>();

                foreach (var record in records)
                {
                    // Fetch employee details using Lazy Loading
                    var employee = await _context.Users
                        .FirstOrDefaultAsync(u => u.Id == record.EmployeeId);

                    if (employee == null)
                    {
                        _logger.LogWarning("GetAttendanceAndLeaveReport: Employee with ID {EmployeeId} not found for Date {Date}.", record.EmployeeId, record.Date);
                        continue; // Skip if employee not found
                    }

                    // Access Branch through Lazy Loading
                    var branchName = employee.Branch?.Name ?? "Unknown";

                    // Fetch overtime and late hours
                    var overtimeHours = await _context.EmployeeExtraAndLateHours
                        .Where(o => o.EmployeeId == record.EmployeeId && o.Date == record.Date && o.Type == "OverTime")
                        .Select(o => o.Hours)
                        .FirstOrDefaultAsync();

                    var lateHours = await _context.EmployeeExtraAndLateHours
                        .Where(o => o.EmployeeId == record.EmployeeId && o.Date == record.Date && o.Type == "Late")
                        .Select(o => o.Hours)
                        .FirstOrDefaultAsync();

                    report.Add(new AttendanceLeaveReportDTO
                    {
                        EmployeeName = employee.Name,
                        Email = employee.Email,
                        Date = record.Date,
                        TimeOfAttend = record.TimeOfAttend != default ? record.TimeOfAttend.ToString("hh:mm tt") : null,
                        TimeOfLeave = record.TimeOfLeave != default ? record.TimeOfLeave.ToString("hh:mm tt") : null,
                        BranchName = branchName,
                        NumberOfOverTime = overtimeHours, // double
                        NumberOfLateHour = lateHours // double
                    });
                }

                _logger.LogInformation("GetAttendanceAndLeaveReport: Successfully retrieved {RecordCount} records for ReportType: {ReportType}, EmployeeId: {EmployeeId}, DateRange: {StartDate} to {EndDate}",
                    report.Count, dto.ReportType, dto.EmployeeId ?? "All", startDate, endDate);

                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAttendanceAndLeaveReport: Unexpected error while processing report for EmployeeId: {EmployeeId}", dto?.EmployeeId ?? "Unknown");
                throw new Exception("An unexpected error occurred while processing the report.", ex);
            }
        }


        #endregion


        #region Attendance Reports

        public async Task<List<AttendanceReportDTO>> AttendanceReport(ParamDTO dto)
        {
            try
            {
                // Validate input parameters to ensure page number and page size are positive
                if (dto.PageNumber < 1 || dto.PageSize < 1)
                {
                    throw new ArgumentException("Page number and page size must be greater than zero.");
                }

                // Determine the target month for the report (default to current month if not specified)
                var now = DateTime.Now;
                var year = dto.Year ?? now.Year;
                var month = dto.Month ?? now.Month;
                var startOfMonth = new DateTime(year, month, 1);
                var endOfMonth = startOfMonth.AddMonths(1).AddTicks(-1);

                // Fetch employees with pagination, ordered by name
                var employeesQuery = _context.Users
                    .Select(u => new { u.Id, u.Name, u.Email });

                var employees = await employeesQuery
                    .OrderBy(e => e.Name)
                    .Skip((dto.PageNumber - 1) * dto.PageSize)
                    .Take(dto.PageSize)
                    .ToListAsync();

                // Initialize the report list to store the results
                var report = new List<AttendanceReportDTO>();

                // Iterate over each employee to calculate their attendance metrics
                foreach (var emp in employees)
                {
                    // Fetch attendance and leave records for the employee within the target month
                    var attendanceRecords = await _context.AttendancesAndLeaves
                        .Where(al => al.EmployeeId == emp.Id
                            && al.Time >= startOfMonth
                            && al.Time <= endOfMonth
                            && (al.Type == "Attendance" || al.Type == "Leave"))
                        .OrderBy(al => al.Time) // Ensure records are ordered by time
                        .ToListAsync();

                    // Fetch absence records for the employee within the target month directly from EmployeeAbsent
                    var absences = await _context.EmployeeAbsents
                        .Where(a => a.EmployeeId == emp.Id
                            && a.AbsentDate >= startOfMonth
                            && a.AbsentDate <= endOfMonth)
                        .Select(a => a.AbsentDate.Date)
                        .Distinct() // Ensure we count distinct days only
                        .ToListAsync();

                    // Fetch vacation records for the employee within the target month directly from EmployeeVacation
                    var vacations = await _context.EmployeeVacations
                        .Where(v => v.UserId == emp.Id
                            && v.Date >= startOfMonth
                            && v.Date <= endOfMonth)
                        .Select(v => v.Date.Date)
                        .Distinct() // Ensure we count distinct days only
                        .ToListAsync();

                    // Fetch extra and late hours for the employee within the target month
                    var extraAndLateHours = await _context.EmployeeExtraAndLateHours
                        .Where(el => el.EmployeeId == emp.Id
                            && el.Date >= startOfMonth
                            && el.Date <= endOfMonth)
                        .ToListAsync();

                    // Initialize variables to store the calculated metrics
                    double workingHours = 0;
                    double lateHours = 0;
                    double absentDays = 0;
                    double vacationDays = 0;
                    double overTime = 0;

                    // Group attendance records by date to process daily data
                    var dailyAttendance = attendanceRecords
                        .GroupBy(r => r.Time.Date)
                        .ToList();

                    // Calculate working hours for each day, considering multiple shifts
                    foreach (var dayRecords in dailyAttendance)
                    {
                        var date = dayRecords.Key;

                        // Order the records by time to ensure correct pairing of Attendance and Leave
                        var orderedRecords = dayRecords.OrderBy(r => r.Time).ToList();

                        // Process each pair of Attendance and Leave to handle multiple shifts
                        DateTime? lastAttendanceTime = null;
                        double dailyWorkingHours = 0;

                        foreach (var record in orderedRecords)
                        {
                            if (record.Type == "Attendance")
                            {
                                // Store the attendance time and wait for the next Leave record
                                lastAttendanceTime = record.Time;
                            }
                            else if (record.Type == "Leave" && lastAttendanceTime.HasValue)
                            {
                                // Calculate the hours worked for this shift (Attendance to Leave)
                                var hoursWorkedForShift = (record.Time - lastAttendanceTime.Value).TotalHours;
                                if (hoursWorkedForShift > 0) // Ensure the leave time is after attendance
                                {
                                    dailyWorkingHours += hoursWorkedForShift;
                                }
                                lastAttendanceTime = null; // Reset for the next shift
                            }
                        }

                        // Add the daily working hours to the total
                        workingHours += dailyWorkingHours;
                    }

                    // Calculate late hours solely from the EmployeeExtraAndLateHour entity
                    lateHours = extraAndLateHours
                        .Where(el => el.Type == "Late")
                        .Sum(el => el.Hours);

                    // Calculate overtime solely from the EmployeeExtraAndLateHour entity
                    overTime = extraAndLateHours
                        .Where(el => el.Type == "OverTime")
                        .Sum(el => el.Hours);

                    // Calculate absence days directly from EmployeeAbsent without additional conditions
                    absentDays = absences.Count;

                    // Calculate vacation days directly from EmployeeVacation without additional conditions
                    vacationDays = vacations.Count;

                    // Add the employee's record to the report
                    report.Add(new AttendanceReportDTO
                    {
                        EmployeeId = emp.Id,
                        Name = emp.Name,
                        Email = emp.Email,
                        NumberOfMonthlyWorkingHours = Math.Round(workingHours, 2),
                        NumberOfLateHours = Math.Round(lateHours, 2),
                        NumberOfAbsentDays = absentDays,
                        NumberOfVacationDays = vacationDays,
                        NumberOfOverTime = Math.Round(overTime, 2)
                    });
                }

                // Log the successful retrieval of the report
                _logger.LogInformation("Retrieved attendance report. Generated {Count} records for page {PageNum}.", report.Count, dto.PageNumber);
                return report;
            }
            catch (Exception ex)
            {
                // Log any errors that occur during the process
                _logger.LogError(ex, "Error retrieving attendance report for page {PageNum}", dto.PageNumber);
                throw new Exception("An error occurred while retrieving the attendance report.", ex);
            }
        }

        #endregion


        #region Get Employee Absences

        public async Task<List<AbsenceReportDTO>> GetEmployeeAbsent(AttendanceAndLeaveReportDTO dto)
        {
            try
            {
                // Validate input
                if (dto == null)
                {
                    _logger.LogWarning("GetEmployeeAbsent: Received null AttendanceAndLeaveReportDTO.");
                    throw new ArgumentNullException(nameof(dto));
                }

                if (dto.PageSize <= 0)
                {
                    _logger.LogWarning("GetEmployeeAbsent: Invalid PageSize {PageSize}.", dto.PageSize);
                    throw new ArgumentException("PageSize must be greater than zero.");
                }

                if (dto.PageNumber <= 0)
                {
                    _logger.LogWarning("GetEmployeeAbsent: Invalid PageNumber {PageNumber}.", dto.PageNumber);
                    throw new ArgumentException("PageNumber must be greater than zero.");
                }

                // Determine date range based on ReportType
                DateTime startDate;
                DateTime endDate;

                if (dto.ReportType == ReportType.Daily)
                {
                    startDate = dto.DayDate?.Date ?? DateTime.Now.Date;
                    endDate = startDate;
                }
                else if (dto.ReportType == ReportType.Monthly)
                {
                    if (dto.Month.HasValue)
                    {
                        int year = DateTime.Now.Year; // Default to current year
                        if (dto.FromDate.HasValue)
                            year = dto.FromDate.Value.Year;
                        startDate = new DateTime(year, dto.Month.Value, 1);
                        endDate = startDate.AddMonths(1).AddDays(-1);
                    }
                    else if (dto.FromDate.HasValue && dto.ToDate.HasValue)
                    {
                        if (dto.ToDate < dto.FromDate)
                        {
                            _logger.LogWarning("GetEmployeeAbsent: ToDate {ToDate} is before FromDate {FromDate}.", dto.ToDate, dto.FromDate);
                            throw new ArgumentException("ToDate must be greater than or equal to FromDate.");
                        }
                        startDate = dto.FromDate.Value.Date;
                        endDate = dto.ToDate.Value.Date;
                    }
                    else
                    {
                        // Default to current month
                        startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                        endDate = startDate.AddMonths(1).AddDays(-1);
                    }
                }
                else
                {
                    if (dto.FromDate.HasValue && dto.ToDate.HasValue)
                    {
                        if (dto.ToDate < dto.FromDate)
                        {
                            _logger.LogWarning("GetEmployeeAbsent: ToDate {ToDate} is before FromDate {FromDate}.", dto.ToDate, dto.FromDate);
                            throw new ArgumentException("ToDate must be greater than or equal to FromDate.");
                        }
                        startDate = dto.FromDate.Value.Date;
                        endDate = dto.ToDate.Value.Date;
                    }
                    else
                    {
                        _logger.LogWarning("GetEmployeeAbsent: ReportType is not specified.");
                        throw new ArgumentException("ReportType must be specified (Daily or Monthly).");
                    }
                }

                // Build query for absence records
                var query = _context.EmployeeAbsents
                    .Where(a => a.AbsentDate.Date >= startDate && a.AbsentDate.Date <= endDate);

                // Filter by EmployeeId if provided
                if (!string.IsNullOrEmpty(dto.EmployeeId))
                {
                    query = query.Where(x => x.EmployeeId == dto.EmployeeId);
                }

                // Group by EmployeeId to fetch all absence records per employee
                var groupedQuery = from a in query
                                   group a by a.EmployeeId into g
                                   select new
                                   {
                                       EmployeeId = g.Key,
                                       Absences = g.ToList()
                                   };

                // Apply pagination
                var totalRecords = await groupedQuery.CountAsync();
                var pagedQuery = groupedQuery
                    .OrderBy(x => x.EmployeeId)
                    .Skip((dto.PageNumber - 1) * dto.PageSize)
                    .Take(dto.PageSize);

                // Execute query
                var records = await pagedQuery.ToListAsync();

                // Fetch additional data and build the report
                var report = new List<AbsenceReportDTO>();
                foreach (var record in records)
                {
                    // Fetch employee details using Lazy Loading
                    var employee = await _context.Users
                        .FirstOrDefaultAsync(u => u.Id == record.EmployeeId);

                    if (employee == null)
                    {
                        _logger.LogWarning("GetEmployeeAbsent: Employee with ID {EmployeeId} not found.", record.EmployeeId);
                        continue; // Skip if employee not found
                    }

                    report.Add(new AbsenceReportDTO
                    {
                        EmployeeName = employee.Name,
                        Email = employee.Email,
                        Data = record.Absences.Select(a => new EmployeeDetailsDTO
                        {
                            Hours = a.Hours,
                            Date = a.AbsentDate.ToString("yyyy-MM-dd") // Convert DateTime to string
                        }).ToList()
                    });
                }

                _logger.LogInformation("GetEmployeeAbsent: Successfully retrieved {RecordCount} absence records for ReportType: {ReportType}, EmployeeId: {EmployeeId}, DateRange: {StartDate} to {EndDate}",
                    report.Count, dto.ReportType, dto.EmployeeId ?? "All", startDate, endDate);

                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetEmployeeAbsent: Unexpected error while processing report for EmployeeId: {EmployeeId}", dto?.EmployeeId ?? "Unknown");
                throw new Exception("An unexpected error occurred while processing the absence report.", ex);
            }
        }

        #endregion


        #region Absence Reports

        public async Task<List<AbsenceSummaryReportDTO>> AbsenceReport(ParamDTO dto)
        {
            try
            {
                // Validate input parameters to ensure page number and page size are positive
                if (dto.PageNumber < 1 || dto.PageSize < 1)
                {
                    throw new ArgumentException("Page number and page size must be greater than zero.");
                }

                // Determine the target month for the report (default to current month if not specified)
                var now = DateTime.Now;
                var year = dto.Year ?? now.Year;
                var month = dto.Month ?? now.Month;
                var startOfMonth = new DateTime(year, month, 1);
                var endOfMonth = startOfMonth.AddMonths(1).AddTicks(-1);

                // Fetch employees with pagination, ordered by name
                var employeesQuery = _context.Users
                    .Select(u => new { u.Id, u.Name, u.Email });

                var employees = await employeesQuery
                    .OrderBy(e => e.Name)
                    .Skip((dto.PageNumber - 1) * dto.PageSize)
                    .Take(dto.PageSize)
                    .ToListAsync();

                // Initialize the report list to store the results
                var report = new List<AbsenceSummaryReportDTO>();

                // Iterate over each employee to calculate their absence metrics
                foreach (var emp in employees)
                {
                    // Fetch absence records for the employee within the target month directly from EmployeeAbsent
                    var absences = await _context.EmployeeAbsents
                        .Where(a => a.EmployeeId == emp.Id
                            && a.AbsentDate >= startOfMonth
                            && a.AbsentDate <= endOfMonth)
                        .ToListAsync();

                    // Calculate total hours and prepare daily details
                    double totalHours = absences.Sum(a => a.Hours);
                    var dailyDetails = absences
                        .GroupBy(a => a.AbsentDate.Date)
                        .Select(g => new EmployeeDetailsDTO
                        {
                            Hours = g.Sum(x => x.Hours),
                            Date = g.Key.ToString("yyyy-MM-dd") // Convert DateTime to string as per DTO
                        })
                        .FirstOrDefault(); // Take the first day's details as a sample (or adjust logic)

                    // Add the employee's record to the report
                    report.Add(new AbsenceSummaryReportDTO
                    {
                        BasicInformation = new ReportDTO
                        {
                            EmployeeId = emp.Id,
                            EmployeeName = emp.Name,
                            TotalHours = Math.Round(totalHours, 2)
                        },
                        OtherData = dailyDetails // Can be null if no absences found
                    });
                }

                // Log the successful retrieval of the report
                _logger.LogInformation("Retrieved absence report. Generated {Count} records for page {PageNum}.", report.Count, dto.PageNumber);
                return report;
            }
            catch (Exception ex)
            {
                // Log any errors that occur during the process
                _logger.LogError(ex, "Error retrieving absence report for page {PageNum}", dto.PageNumber);
                throw new Exception("An error occurred while retrieving the absence report.", ex);
            }
        }

        #endregion


        #region TO DO: Get Monthly Salary Report

        #endregion


        #region TO DO: Get Monthly Vacation Report

        #endregion

    }
}
