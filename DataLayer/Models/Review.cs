using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataLayer.Models.Enums;

namespace DataLayer.Models
{
    public class Review : BaseEntity
    {
        public int? ProjectId { get; set; }
        public Project? Project { get; set; }
        public string Name { get; set; }
        public int WorkflowTemplateId { get; set; }
        public WorkflowTemplate WorkflowTemplate { get; set; }


        public string InitiatorUserId { get; set; }
        public ApplicationUser InitiatorUser { get; set; }

        public int ? CurrentStepId { get; set; }
        public WorkflowStepTemplate CurrentStep { get; set; }
        public FinalReviewStatus FinalReviewStatus { get; set; } = FinalReviewStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public List<ReviewDocumentComment>? Comments { get; set; }


        public List<ReviewDocument> ReviewDocuments { get; set; } = new List<ReviewDocument>();
        public List<ReviewFolder> ReviewFolders { get; set; } = new List<ReviewFolder>();

        public List<ReviewStepUser> ReviewStepUsers { get; set; } = new List<ReviewStepUser> { }; 



    }

}
