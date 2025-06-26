using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.Models
{
    public class ProjectActivities : BaseEntity
    {
        public string UserEmail { get; set; }
        public int? projectId { get; set; }
        public Project? project { get; set; }
        public DateTime Date { get; set; }
        public string ActivityType { get; set; }
        public string ActivityDetail { get; set; } 
        
    }
}
