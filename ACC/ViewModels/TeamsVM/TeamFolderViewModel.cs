namespace ACC.ViewModels.TeamsVM
{
    public class TeamFolderViewModel
    {
        public int Id { get; set; }
        public string TeamName { get; set; }

        public string ParentFolder { get; set; }
        public int? MemberCount { get; set; } = 1;
    }
}
