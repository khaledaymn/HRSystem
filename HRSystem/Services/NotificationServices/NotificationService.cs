using HRSystem.DTO.NotificationDTOs;
using HRSystem.Models;
using HRSystem.Services.NotificationServices;
using HRSystem.UnitOfWork;
using System;
using System.Threading.Tasks;

namespace HRSystem.Services.NotificationServices
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(IUnitOfWork unitOfWork, ILogger<NotificationService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        #region Add Notification
        public async Task AddNotification(AddNotificationDTO dto)
        {
            try
            {
                var notificationRepository = _unitOfWork.Repository<Notification>();
                var notification = new Notification
                {
                    EmployeeId = dto.EmployeeId,
                    Name = dto.Name,
                    Message = dto.Message,
                    CreatedAt = DateTime.Now,
                    ShiftId = dto.ShiftId,
                    Title = "Forget Leave Notification",
                    StartTime =dto.StartTime,
                    EndTime = dto.EndTime
                };

                await notificationRepository.ADD(notification);
                await _unitOfWork.Save();
                //_logger.LogInformation("Added notification for EmployeeId: {EmployeeId}: {Message}", employeeId, message);
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Error adding notification for EmployeeId: {EmployeeId}", employeeId);
            }
        }

        #endregion


        #region Add Range
        public async Task AddNotificationsRange(List<AddNotificationDTO> dtos)
        {
            try
            {
                var notificationRepository = _unitOfWork.Repository<Notification>();
                var notifications = dtos.Select(dto => new Notification
                {
                    EmployeeId = dto.EmployeeId,
                    Name = dto.Name,
                    Message = dto.Message,
                    CreatedAt = DateTime.Now,
                    ShiftId = dto.ShiftId,
                    Title = "Forget Leave Notification",
                    StartTime = dto.StartTime,
                    EndTime = dto.EndTime
                }).ToList();

                await notificationRepository.AddRange(notifications);
                await _unitOfWork.Save();
                //_logger.LogInformation("Added {Count} notifications", notifications.Count);
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Error adding notifications batch");
            }
        }
        #endregion


        #region Get Notification

        public async Task<List<NotificationDTO>> GetNotifications()
        {
            try
            {
                var notificationRepository = _unitOfWork.Repository<Notification>();
                var notifications = await notificationRepository.GetAll();
                var notificationDTOs = notifications.Select(n => new NotificationDTO
                {
                    Id = n.Id,
                    Title = n.Title,
                    CreatedAt = n.CreatedAt,
                    Name = n.Name,
                    StartTime = n.StartTime,
                    EndTime = n.EndTime,
                    Message = n.Message,
                    EmployeeId = n.EmployeeId,
                    ShiftId = n.ShiftId
                }).ToList();
                return notificationDTOs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notifications");
                return new List<NotificationDTO>();
            }
        }

        #endregion


        #region Delete Notification

        public async Task<bool> DeleteNotification(int id)
        {
            try
            {
                var notificationRepository = _unitOfWork.Repository<Notification>();
                var notification = await notificationRepository.GetById(id);
                if (notification == null)
                {
                    return false;
                }
                notificationRepository.Delete(notification.Id);
                await _unitOfWork.Save();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notification with ID: {Id}", id);
                return false;
            }
        }
        #endregion

    }
}