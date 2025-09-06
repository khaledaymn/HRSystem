using HRSystem.DTO.NotificationDTOs;

namespace HRSystem.Services.NotificationServices
{
    public interface INotificationService
    {
        Task AddNotification(AddNotificationDTO dto);
        Task<List<NotificationDTO>> GetNotifications();
        Task<bool> DeleteNotification(int id);
        Task AddNotificationsRange(List<AddNotificationDTO> dtos);
    }
}
