using Microsoft.AspNetCore.Mvc;
using FuelFlowQueueLab.Models;
using FuelFlowQueueLab.Models.ViewModels;

namespace FuelFlowQueueLab.Controllers
{
    public class HomeController : Controller
    {
        /// <summary>
        /// Landing page — shows two main cards (Calculator + Simulator) and the project team.
        /// </summary>
        public IActionResult Index()
        {
            var model = new HomeViewModel
            {
                TeamMembers = new List<TeamMember>
                {
                    new() { Name = "Syed Haziq Zia Naqvi", RollNumber = "EB24210006102" },
                    new() { Name = "Muhammad Noman", RollNumber = "EB24210006067" },
                    new() { Name = "Syed Abdul Ali Naqvi", RollNumber = "EB24210006092" },
                    new() { Name = "Syed Abeer bin Haider", RollNumber = "EB24210006094" },
                    new() { Name = "Hasan Kamran", RollNumber = "EB24210006027" },
                    new() { Name = "Syed Muhammad Ahmed", RollNumber = "EB24210006112" },
                    new() { Name = "Usama Khan", RollNumber = "EB24210006124" }
                }
            };
            return View(model);
        }
    }
}
