using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.Models
{
    public class ReviewDocumentComment : BaseEntity
    {

        public int DocumentId { get; set; }    
        public Document Document { get; set; }

        public int ReviewId { get; set; }
        public Review Review { get; set; }

        public string ReviewerId { get; set; }                   
        public ApplicationUser Reviewer { get; set; }

        public int StepOrder{ get; set; }     


        public string Comment { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

}
