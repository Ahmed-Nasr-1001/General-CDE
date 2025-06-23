using DataLayer.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.Models
{
    public class WorkflowStepTemplate : BaseEntity
    {

        public int WorkflowTemplateId { get; set; }
        public WorkflowTemplate WorkflowTemplate { get; set; }

        public int ? StepOrder { get; set; } 

        public int TimeAllowed { get; set; }

        public string selectedPositionId { get; set; }
        public ReviewersType? ReviewersType { get; set; }

        public int? MinReviewers { get; set; }
        public MultiReviewerOptions? MultiReviewerOptions { get; set; }

        public List<WorkflowStepUser> workflowStepUsers { get; set; }

        public List<ReviewStepUser> ReviewStepUsers { get; set; } = new List<ReviewStepUser> { };



    }
}
