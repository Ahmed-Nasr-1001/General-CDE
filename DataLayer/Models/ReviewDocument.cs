using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.Models
{
    public class ReviewDocument
    {
        public int ReviewId { get; set; }
        public Review Review { get; set; }

        public int DocumentId { get; set; }
        public Document Document { get; set; }

        public List<ReviewDocumentComment>? Comments { get; set; }
        public string? Status { get; set; } 

        public bool? IsApproved { get; set; }
    }
}
