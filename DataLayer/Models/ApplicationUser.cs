using DataLayer.Models.Enums;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.Models
{
    public class ApplicationUser : IdentityUser
    {
        

        [ForeignKey(nameof(Company))]
        public int? CompanyId { get; set; }
        public Company? Company { get; set; }

        [ForeignKey(nameof(Role))]
        
        public Status? Status { get; set; }
        public IList<ProjectMembers>? Projects { get; set; }
        public DateTime? AddedOn { get; set; } = DateTime.Now;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? MobilePhone { get; set; }


        public List<WorkflowStepTemplate>? WorkflowSteps { get; set; }

        public List<WorkflowStepUser> workflowStepUsers { get; set; }

        public List<ReviewStepUser> ReviewStepUsers { get; set; } = new List<ReviewStepUser> { };

        public List<IssueReviwers>? IssueReviwers { get; set; }

        public ICollection<ApplicationUserRole> UserRoles { get; set; }
        public int AccessLevelId { get; set; }
        public AccessLevel AccessLevel { get; set; }

        public List<TeamMember> TeamMembers { get; set; }

        //For Notifications
        public List<Notification>? ReceivedNotifications { get; set; } = new List<Notification>();
        public List<Notification>? SentNotifications { get; set; } = new List<Notification>();


    }
}
