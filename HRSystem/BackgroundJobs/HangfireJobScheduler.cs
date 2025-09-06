using System;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using HRSystem.Models;
using HRSystem.Services.ShiftAnalysisServices;
using HRSystem.UnitOfWork;
using Microsoft.Extensions.Logging;

namespace HRSystem.BackgroundJobs
{
    public class HangfireJobScheduler
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<HangfireJobScheduler> _logger;
        private readonly IBackgroundJobClient _backgroundJobClient;

        public HangfireJobScheduler(
            IUnitOfWork unitOfWork,
            ILogger<HangfireJobScheduler> logger,
            IBackgroundJobClient backgroundJobClient)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _backgroundJobClient = backgroundJobClient;
        }

        public async Task ScheduleShiftAnalysisJobs()
        {
            try
            {
                _logger.LogInformation("Starting to schedule shift analysis jobs");

                var shiftRepository = _unitOfWork.Repository<Shift>();
                var shifts = await shiftRepository.GetAll();

                if (!shifts.Any())
                {
                    _logger.LogInformation("No shifts found to schedule analysis jobs");
                    return;
                }

                foreach (var shift in shifts)
                {
                    var now = DateTime.Now;
                    var shiftStartTimeToday = new DateTime(now.Year, now.Month, now.Day, shift.StartTime.Hour, shift.StartTime.Minute, 0);

                    if (shift.StartTime > shift.EndTime)
                    {
                        if (shiftStartTimeToday < now)
                        {
                            shiftStartTimeToday = shiftStartTimeToday.AddDays(1);
                        }
                    }
                    else
                    {
                        if (shiftStartTimeToday < now)
                        {
                            shiftStartTimeToday = shiftStartTimeToday.AddDays(1);
                        }
                    }

                    var delay = shiftStartTimeToday - now;

                    var jobId = $"AnalyzePreviousShift_{shift.Id}_{shiftStartTimeToday:yyyyMMddHHmm}";
                    _backgroundJobClient.Schedule<ShiftAnalysisService>(
                        service => service.AnalyzePreviousShiftForEmployees(shiftStartTimeToday),
                        delay);

                    _logger.LogInformation("Scheduled shift analysis job {JobId} for shift starting at {ShiftStartTime}", jobId, shiftStartTimeToday);
                }

                _logger.LogInformation("Completed scheduling shift analysis jobs");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling shift analysis jobs");
            }
        }

        public async Task ExecuteSalaryCalculationAsync()
        {
            try
            {
                var today = DateTime.Today;
                var calculationDate = new DateTime(today.Year, today.Month, 1);

                _logger.LogInformation("Executing salary calculation for {MonthYear}", calculationDate.ToString("MMMM yyyy"));

                await _unitOfWork.UsersServices.CalculateSalary(calculationDate);

                _logger.LogInformation("Salary calculation completed for {MonthYear}", calculationDate.ToString("MMMM yyyy"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to calculate salaries for {MonthYear}",
                    DateTime.Now.AddMonths(-1).ToString("MMMM yyyy"));
                throw;
            }
        }

        public void ScheduleRecurringJobs()
        {
            try
            {
                // Schedule salary calculation for the previous month at 23:59 on the last day of each month
                RecurringJob.AddOrUpdate<HangfireJobScheduler>(
                    "CalculateMonthlySalary",
                    (scheduler) => scheduler.ExecuteSalaryCalculationAsync(),
                    "59 23 L * *" // Run at 23:59 on the last day of the month
                );

                _logger.LogInformation("Successfully scheduled recurring salary calculation job to run at 21:59 on the last day of each month.");
                RecurringJob.AddOrUpdate<HangfireJobScheduler>(
                    "RescheduleShiftAnalysisJobs",
                    scheduler => scheduler.ScheduleShiftAnalysisJobs(),
                    "0 0 * * *"
                    );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling recurring salary calculation job.");
                throw;
            }
        }
    }
}
