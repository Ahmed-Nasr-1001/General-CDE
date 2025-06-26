using DataLayer.Models;

namespace ACC.ViewModels.ProjectActivityVM
{
    public class ProjectActivityVM
    {
        public int Id;
        public DateTime Date { get; set; } 

        public string ActivityType { get; set; }
        public string ActivityDetail { get; set; }

        public int ProjectId { get; set; }

        public string UserEmail { get; set; }
      

    }
}
