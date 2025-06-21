using DataLayer.Models.Enums;

namespace ACC.ViewModels.MemberVM.MemberVM
{
    public class MemberVM
    {
        public string Id { get; set; }
        public string? Name { get; set; }
        public string Email { get; set; }
        public string? Company { get; set; }
        public string GlobalAccessLevel { get; set; }
        public DateTime? AddedOn { get; set; }
    }
}
