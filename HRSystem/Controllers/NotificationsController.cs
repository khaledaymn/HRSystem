using HRSystem.DTO.NotificationDTOs;
using HRSystem.Helper;
using HRSystem.UnitOfWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HRSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(IUnitOfWork unitOfWork, ILogger<NotificationsController> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }


        #region AddNotification
        [HttpPost]
        [Route("~/Notifications/add")]
        public async Task<IActionResult> AddNotification(AddNotificationDTO dto)
        {
            try
            {
                await _unitOfWork.NotificationService.AddNotification(dto);
                return Ok("Notification added successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding notification");
                return StatusCode(500, "Internal server error");
            }
        }

        #endregion


        #region GetNotification
        [HttpGet]
        [Route("~/Notifications/get")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> GetNotifications()
        {
            try
            {
                var notifications = await _unitOfWork.NotificationService.GetNotifications();
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notifications");
                return StatusCode(500, "Internal server error");
            }
        }

        #endregion


        #region DeleteNotification

        [HttpDelete]
        [Route("~/Notifications/delete/{id}")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> DeleteNotification(int id)
        {
            try
            {
                var result = await _unitOfWork.NotificationService.DeleteNotification(id);
                if (result)
                {
                    return Ok("Notification deleted successfully");
                }
                else
                {
                    return NotFound("Notification not found");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notification");
                return StatusCode(500, "Internal server error");
            }
        }

        #endregion


        #region AddNotificationsRange
        [HttpPost]
        [Route("~/Notifications/addRange")]
        public async Task<IActionResult> AddNotificationsRange(List<AddNotificationDTO> dtos)
        {
            try
            {
                await _unitOfWork.NotificationService.AddNotificationsRange(dtos);
                return Ok("Notifications added successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding notifications");
                return StatusCode(500, "Internal server error");
            }
        }
        #endregion

    }
}
