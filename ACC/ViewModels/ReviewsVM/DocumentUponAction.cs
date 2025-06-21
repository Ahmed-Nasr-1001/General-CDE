using DataLayer.Models.Enums;

namespace ACC.ViewModels.ReviewsVM
{
    public class DocumentUponAction
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Comment { get; set; }

        public string State { get; set; } 

        public bool? IsApproved { get; set; }
    }
}
