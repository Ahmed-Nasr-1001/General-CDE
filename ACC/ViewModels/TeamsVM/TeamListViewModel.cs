namespace ACC.ViewModels.TeamsVM
{
    public class TeamListViewModel
    {
        public int ProjectId { get; set; }
        public List<TeamFolderViewModel> ExistingTeams { get; set; } = new();
    }
}
