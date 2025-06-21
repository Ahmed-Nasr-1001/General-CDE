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
        General = 5
    }
}
