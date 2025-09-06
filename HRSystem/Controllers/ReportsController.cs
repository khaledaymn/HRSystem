using HRSystem.DTO.AttendanceDTOs;
using HRSystem.Helper;
using HRSystem.UnitOfWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HRSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ReportsController> logger;

        public ReportsController(IUnitOfWork unitOfWork, ILogger<ReportsController> logger)
        {
            _unitOfWork = unitOfWork;
            this.logger = logger;
        }

        #region Get Over Time

        [HttpGet]
        [Route("~/Reports/MonthlyOvertimeReport")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> GetMonthlyOvertimeReport()
        {
            try
            {
                var report = await _unitOfWork.ReportServices.GetMonthlyOvertimeReport();
                return Ok(new
                {
                    Success = true,
                    Data = report
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving monthly overtime report");
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "An unexpected error occurred while retrieving the overtime report."
                });
            }
        }

        #endregion


        #region Get Late Report

        [HttpGet]
        [Route("~/Reports/MonthlyLateReport")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> GetMonthlyLateReport()
        {
            try
            {
                var report = await _unitOfWork.ReportServices.GetMonthlyLateReport();
                return Ok(new
                {
                    Success = true,
                    Data = report
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving monthly late report");
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "An unexpected error occurred while retrieving the late report."
                });
            }
        }

        #endregion


        #region Employee Attendance And Leave Report

        [HttpGet]
        [Route("~/Reports/GetEmployeeAttendanceAndLeaveReport")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.User}")]
        public async Task<IActionResult> GetEmployeeAttendanceAndLeaveReport([FromQuery]AttendanceAndLeaveReportDTO dto)
        {
            try
            {
                var report = await _unitOfWork.ReportServices.GetAttendanceAndLeaveReport(dto);
                return Ok(new
                {
                    Success = true,
                    Data = report
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving monthly attendance and leave report");
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "An unexpected error occurred while retrieving the attendance and leave report."
                });
            }
        }

        #endregion


        #region Employee Attendance Report

        [HttpGet]
        [Route("~/Reports/AttendanceReport")]
        [Authorize(Roles = $"{Roles.Admin}")]
        public async Task<IActionResult> EmployeeAttendanceReport([FromQuery] ParamDTO dto)
        {
            try
            {
                var report = await _unitOfWork.ReportServices.AttendanceReport(dto);
                return Ok(new
                {
                    Success = true,
                    Data = report
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving employee attendance report");
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "An unexpected error occurred while retrieving the attendance report."
                });
            }
        }

        #endregion


        #region Employee Absence Report
        [HttpGet]
        [Route("~/Reports/AbsenceReport")]
        //[Authorize(Roles = $"{Roles.Admin}")]
        public async Task<IActionResult> EmployeeAbsenceReport([FromQuery] AttendanceAndLeaveReportDTO dto)
        {
            try
            {
                var report = await _unitOfWork.ReportServices.GetEmployeeAbsent(dto);
                return Ok(new
                {
                    Success = true,
                    Data = report
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving employee absence report");
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "An unexpected error occurred while retrieving the absence report."
                });
            }
        }

        #endregion


        #region Employee Absence Summary Report

        [HttpGet]
        [Route("~/Reports/AbsenceSummaryReport")]
        //[Authorize(Roles = $"{Roles.Admin}")]
        public async Task<IActionResult> EmployeeAbsenceSummaryReport([FromQuery] ParamDTO dto)
        {
            try
            {
                var report = await _unitOfWork.ReportServices.AbsenceReport(dto);
                var result = report.Select(x => new
                {
                    x.BasicInformation.EmployeeId,
                    x.BasicInformation.EmployeeName,
                    x.BasicInformation.TotalHours,
                    x.OtherData
                }).ToList();

                return Ok(new
                {
                    Success = true,
                    Data = result
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving employee absence summary report");
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "An unexpected error occurred while retrieving the absence summary report."
                });
            }
        }

        #endregion
    }
}
