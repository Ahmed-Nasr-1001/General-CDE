namespace ACC.ViewModels.TeamsVM
{
    public class TeamDetailsViewModel
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; }
        public string ProjectName { get; set; }
        public List<TeamMemberViewModel> Members { get; set; }
    }
}
