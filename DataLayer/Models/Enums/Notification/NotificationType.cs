using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.Models.Enums.Notification
{
    public enum NotificationType
    {
        ReviewCreated = 1,
        ReviewApproved = 2,
        ReviewRejected = 3,
        ReviewCompleted = 4,
        General = 5,
        // NEW: Issue notification types
        IssueCreated = 6,
        IssueAssigned = 7,
        IssueUpdated = 8,
        IssueStatusChanged = 9,
        IssueCommentAdded = 10
    }
}
