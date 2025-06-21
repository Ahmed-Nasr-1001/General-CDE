using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataLayer.Models;

namespace BusinessLogic.Repository.RepositoryInterfaces
{
    public interface INotificationRepository : IGenericRepository<Notification>
    {
        Task<List<Notification>> GetUserNotificationsAsync(string userId);
        Task<List<Notification>> GetUnreadNotificationsAsync(string userId);
        Task<int> GetUnreadCountAsync(string userId);
        Task MarkAsReadAsync(int notificationId);
        Task MarkAllAsReadAsync(string userId);
        Task CreateNotificationAsync(Notification notification);
        Task CreateBulkNotificationsAsync(List<Notification> notifications);
        // NEW: Method to get notifications by review ID
        Task<List<Notification>> GetNotificationsByReviewIdAsync(int reviewId);
    }
}
