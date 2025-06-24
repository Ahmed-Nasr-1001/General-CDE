using BusinessLogic.Repository.RepositoryInterfaces;
using DataLayer;
using DataLayer.Models;
using DataLayer.Models.Enums.Notification;
using Microsoft.EntityFrameworkCore;

namespace BusinessLogic.Repository.RepositoryClasses
{
    public class NotificationRepository : GenericRepository<Notification>, INotificationRepository
    {
        private readonly AppDbContext _context;

        public NotificationRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        //public async Task<List<Notification>> GetUserNotificationsAsync(string userId)
        //{
        //    return await _context.Notifications
        //        .Where(n => n.RecipientId == userId)
        //        .Include(n => n.Sender)
        //        .Include(n => n.Review)
        //            .ThenInclude(r => r.InitiatorUser)
        //        .Include(n => n.Review)
        //            .ThenInclude(r => r.Project)
        //        .Include(n => n.Review)
        //            .ThenInclude(r => r.WorkflowTemplate) // NEW: Include workflow template
        //        .OrderByDescending(n => n.CreatedAt)
        //        .ToListAsync();
        //}


        //public async Task<List<Notification>> GetUnreadNotificationsAsync(string userId)
        //{
        //    return await _context.Notifications
        //        .Where(n => n.RecipientId == userId && n.Status == NotificationStatus.Unread)
        //        .Include(n => n.Sender)
        //        .Include(n => n.Review)
        //            .ThenInclude(r => r.InitiatorUser)
        //        .Include(n => n.Review)
        //            .ThenInclude(r => r.WorkflowTemplate) // NEW: Include workflow template
        //        .OrderByDescending(n => n.CreatedAt)
        //        .ToListAsync();
        //}

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.RecipientId == userId && n.Status == NotificationStatus.Unread);
        }

        public async Task MarkAsReadAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                notification.Status = NotificationStatus.Read;
                notification.ReadAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(string userId)
        {
            var unreadNotifications = await _context.Notifications
                .Where(n => n.RecipientId == userId && n.Status == NotificationStatus.Unread)
                .ToListAsync();

            foreach (var notification in unreadNotifications)
            {
                notification.Status = NotificationStatus.Read;
                notification.ReadAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
        }

        public async Task CreateNotificationAsync(Notification notification)
        {
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task CreateBulkNotificationsAsync(List<Notification> notifications)
        {
            _context.Notifications.AddRange(notifications);
            await _context.SaveChangesAsync();
        }

        // NEW: Get notifications by review ID
        public async Task<List<Notification>> GetNotificationsByReviewIdAsync(int reviewId)
        {
            return await _context.Notifications
                .Where(n => n.ReviewId == reviewId)
                .Include(n => n.Recipient)
                .Include(n => n.Sender)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }




        //for issue
        // Add these includes to the GetUserNotificationsAsync method:

        public async Task<List<Notification>> GetUserNotificationsAsync(string userId)
        {
            return await _context.Notifications
                .Where(n => n.RecipientId == userId)
                .Include(n => n.Sender)
                .Include(n => n.Review)
                    .ThenInclude(r => r.InitiatorUser)
                .Include(n => n.Review)
                    .ThenInclude(r => r.Project)
                .Include(n => n.Review)
                    .ThenInclude(r => r.WorkflowTemplate)
                // NEW: Include issue-related data
                .Include(n => n.Issue)
                    .ThenInclude(i => i.Initiator)
                .Include(n => n.Issue)
                    .ThenInclude(i => i.Project)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        // Also update GetUnreadNotificationsAsync method similarly:
        public async Task<List<Notification>> GetUnreadNotificationsAsync(string userId)
        {
            return await _context.Notifications
                .Where(n => n.RecipientId == userId && n.Status == NotificationStatus.Unread)
                .Include(n => n.Sender)
                .Include(n => n.Review)
                    .ThenInclude(r => r.InitiatorUser)
                .Include(n => n.Review)
                    .ThenInclude(r => r.WorkflowTemplate)
                // NEW: Include issue-related data
                .Include(n => n.Issue)
                    .ThenInclude(i => i.Initiator)
                .Include(n => n.Issue)
                    .ThenInclude(i => i.Project)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

    }
}
