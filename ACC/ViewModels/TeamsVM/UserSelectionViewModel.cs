namespace ACC.ViewModels.TeamsVM
{
    public class UserSelectionViewModel
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public bool IsSelected { get; set; }
        public string FilePermission { get; set; } = "View";
        public string IssuePermission { get; set; } = "View";
        public string ReviewPermission { get; set; } = "View";
    }
}
