namespace FuelFlowQueueLab.Models.ViewModels
{
    /// <summary>
    /// ViewModel for the Home/Index page. Contains the list of project team members.
    /// </summary>
    public class HomeViewModel
    {
        public List<TeamMember> TeamMembers { get; set; } = new();
    }
}
