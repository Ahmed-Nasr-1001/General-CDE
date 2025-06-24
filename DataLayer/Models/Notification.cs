using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataLayer.Models.Enums.Notification;

namespace DataLayer.Models
{
    public class Notification : BaseEntity
    {

        [Required]
        public string Title { get; set; }

        [Required]
        public string Message { get; set; }

        public string? ActionUrl { get; set; }

        public NotificationType Type { get; set; } = NotificationType.ReviewCreated;

        public NotificationStatus Status { get; set; } = NotificationStatus.Unread;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? ReadAt { get; set; }

        // Foreign Keys
        [ForeignKey(nameof(Recipient))]
        public string RecipientId { get; set; }
        public ApplicationUser Recipient { get; set; }

        [ForeignKey(nameof(Sender))]
        public string? SenderId { get; set; }
        public ApplicationUser? Sender { get; set; }

        // Optional: Link to related review
        [ForeignKey(nameof(Review))]
        public int? ReviewId { get; set; }
        public Review? Review { get; set; }

        // NEW: Optional link to related issue
        [ForeignKey(nameof(Issue))]
        public int? IssueId { get; set; }
        public Issue? Issue { get; set; }
    }
}
