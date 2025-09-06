using HRSystem.DataBase;
using HRSystem.DTO.NotificationDTOs;
using HRSystem.Models;
using HRSystem.Services.ShiftAnalysisServices;
using HRSystem.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace HRSystem.Services.ShiftAnalysisServices
{
    public class ShiftAnalysisService : IShiftAnalysisService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ShiftAnalysisService> _logger;
        private readonly ApplicationDbContext _context;

        public ShiftAnalysisService(
            IUnitOfWork unitOfWork,
            ILogger<ShiftAnalysisService> logger,
            ApplicationDbContext context)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _context = context;
        }


        #region Analysis Previous Shift For Employees
        public async Task AnalyzePreviousShiftForEmployees(DateTime shiftStartTime)
        {
            try
            {
                _logger.LogInformation("Starting previous shift analysis at {ShiftStartTime}", shiftStartTime);

                // Adjust shiftStartTime to consider only the date and time
                var targetDate = shiftStartTime.Date; // Use the date part of shiftStartTime
                var shiftRepository = _unitOfWork.Repository<Shift>();
                var employeeShiftRepository = _unitOfWork.Repository<EmployeeShift>();

                // Fetch EmployeeShifts directly with their Shifts for the given date and time
                var employeeShifts =  employeeShiftRepository.GetAll().Result
                    .Where(es => es.Shift != null && // Ensure Shift is not null
                                 es.Shift.StartTime.Hour == shiftStartTime.Hour &&
                                 es.Shift.StartTime.Minute == shiftStartTime.Minute)
                    .ToList();

                // Manually fetch Employees (since we can't use Include with IGenaricRepo)
                //var allEmployees = _context.Users;
                //foreach (var es in employeeShifts)
                //{
                //    es.Employee = allEmployees?.FirstOrDefault(e => e.Id == es.EmployeeId);
                //    es.Shift = shiftRepository.GetAll()?.Result
                //        .FirstOrDefault(s => s.Id == es.ShiftId);
                //}

                if (!employeeShifts.Any())
                {
                    _logger.LogInformation("No employees assigned to shifts starting at {ShiftStartTime}", shiftStartTime);
                    return;
                }

                foreach (var employeeShift in employeeShifts)
                {
                    var employee = employeeShift.Employee;
                    var currentShift = employeeShift.Shift;

                    if (employee == null || currentShift == null)
                    {
                        _logger.LogWarning("Employee or Shift is null for EmployeeShift with ShiftId: {ShiftId}", employeeShift.ShiftId);
                        continue;
                    }

                    try
                    {
                        var previousShift = await GetPreviousShift(employee.Id, shiftStartTime);
                        if (previousShift == null)
                        {
                            _logger.LogInformation("No previous shift found for EmployeeId: {EmployeeId} before {ShiftStartTime}", employee.Id, shiftStartTime);
                            continue;
                        }

                        var previousShiftStartDate = previousShift.StartTime.Date;
                        var previousShiftEndDate = previousShift.EndTime.Date;

                        if (previousShift.StartTime > previousShift.EndTime)
                        {
                            previousShiftEndDate = previousShiftStartDate.AddDays(1);
                        }

                        if (await _unitOfWork.OfficialVacationServices.IsOfficialVacationAsync(previousShiftStartDate) ||
                            await _unitOfWork.OfficialVacationServices.IsOfficialVacationAsync(previousShiftEndDate))
                        {
                            _logger.LogInformation("Previous shift from {StartDate} to {EndDate} for EmployeeId: {EmployeeId} includes an official holiday. Skipping analysis.", previousShiftStartDate, previousShiftEndDate, employee.Id);
                            continue;
                        }

                        var attendanceRepository = _unitOfWork.Repository<AttendanceAndLeave>();
                        var hasAttendance = await attendanceRepository.AnyAsync(a =>
                            a.EmployeeId == employee.Id &&
                            (a.Time.Date == previousShiftStartDate || a.Time.Date == previousShiftEndDate) &&
                            a.Type == "Attendance" &&
                            (a.Time.Date == previousShiftStartDate
                                ? a.Time.TimeOfDay >= previousShift.StartTime.TimeOfDay
                                : a.Time.TimeOfDay <= previousShift.EndTime.TimeOfDay));

                        if (!hasAttendance)
                        {
                            var shiftDuration = (previousShift.EndTime.TimeOfDay - previousShift.StartTime.TimeOfDay).TotalHours;
                            if (shiftDuration < 0)
                                shiftDuration += 24;

                            await _unitOfWork.VacationService.AddVacationOrAbsence(employee.Id, DateTime.Now.Date, shiftDuration);
                            _logger.LogInformation("Processed absence/vacation for EmployeeId: {EmployeeId} for previous shift starting on {Date}", employee.Id, previousShiftStartDate);
                        }
                        else
                        {
                            var hasLeave = await attendanceRepository.AnyAsync(a =>
                                a.EmployeeId == employee.Id &&
                                (a.Time.Date == previousShiftStartDate || a.Time.Date == previousShiftEndDate) &&
                                a.Type == "Leave" &&
                                (a.Time.Date == previousShiftStartDate
                                    ? a.Time.TimeOfDay >= previousShift.StartTime.TimeOfDay
                                    : a.Time.TimeOfDay <= previousShift.EndTime.TimeOfDay));

                            if (!hasLeave)
                            {
                                var message = $"الموظف {employee.Name} قد نسي تسجيل المغادرة لورديته يوم {previousShiftStartDate:yyyy-MM-dd} التي بدأت في الساعة {previousShift.StartTime:HH:mm} وانتهت في الساعه {previousShift.EndTime:HH:mm}.";
                                var AddNotification = new AddNotificationDTO
                                {
                                    EmployeeId = employee.Id,
                                    Message = message,
                                    StartTime = previousShift.StartTime.ToString("HH:mm"),
                                    EndTime = previousShift.EndTime.ToString("HH:mm"),
                                    Name = employee.Name,
                                    ShiftId = previousShift.Id,
                                    Title = "تذكير بتسجيل المغادرة",
                                };
                                await _unitOfWork.NotificationService.AddNotification(AddNotification);
                                _logger.LogInformation("Added notification for EmployeeId: {EmployeeId} for missing leave on {Date}", employee.Id, previousShiftStartDate);
                            }
                        }
                        await _unitOfWork.Save();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error analyzing previous shift for EmployeeId: {EmployeeId}", employee.Id);
                        continue;
                    }
                }
                _logger.LogInformation("Completed previous shift analysis at {ShiftStartTime}", shiftStartTime);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during previous shift analysis at {ShiftStartTime}", shiftStartTime);
            }
        }

        #endregion


        #region Get Previous Shift
        public async Task<Shift> GetPreviousShift(string employeeId, DateTime currentShiftStartTime)
        {
            var employeeShiftRepository = _unitOfWork.Repository<EmployeeShift>();
            var employeeShifts = employeeShiftRepository.GetAll().Result
                .Where(es => es.EmployeeId == employeeId)
                .ToList();

            if (!employeeShifts.Any())
                return null;

            var shiftRepository = _unitOfWork.Repository<Shift>();
            var allShifts = await shiftRepository.GetAll();

            //foreach (var es in employeeShifts)
            //{
            //    es.Shift = allShifts.FirstOrDefault(s => s.Id == es.ShiftId);
            //}
            var shifts = employeeShifts
                .Where(es => es.Shift != null) // Ensure Shift is not null
                .Select(es => new
                {
                    Shift = es.Shift,
                    AdjustedEndTime = es.Shift.StartTime > es.Shift.EndTime
                        ? new DateTime(
                            currentShiftStartTime.Year,
                            currentShiftStartTime.Month,
                            currentShiftStartTime.Day,
                            es.Shift.EndTime.Hour,
                            es.Shift.EndTime.Minute,
                            es.Shift.EndTime.Second).AddDays(currentShiftStartTime.Date == es.Shift.StartTime.Date ? 1 : 0)
                        : es.Shift.EndTime
                });

            var previousShift = shifts.Where(x => x.AdjustedEndTime < currentShiftStartTime)
                .OrderByDescending(x => x.AdjustedEndTime)
                .Select(x => x.Shift).FirstOrDefault();

            if (previousShift == null)
                previousShift = shifts.Where(x => x.AdjustedEndTime > currentShiftStartTime)
                .OrderByDescending(x => x.AdjustedEndTime)
                .Select(x => x.Shift).FirstOrDefault();

            return previousShift;
        }

        #endregion

    }
}