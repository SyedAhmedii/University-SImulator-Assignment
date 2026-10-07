using Microsoft.AspNetCore.Mvc;
using FuelFlowQueueLab.Models.Enums;
using FuelFlowQueueLab.Models.ViewModels;
using FuelFlowQueueLab.Services;

namespace FuelFlowQueueLab.Controllers
{
    public class SimulationController : Controller
    {
        private readonly IQueueingCalculatorService _calculator;

        public SimulationController(IQueueingCalculatorService calculator)
        {
            _calculator = calculator;
        }

        /// <summary>
        /// GET /Simulation/Index?model=MM1
        /// Shows the calculator form with the selected model pre-selected.
        /// </summary>
        [HttpGet]
        public IActionResult Index(string? model)
        {
            var vm = new CalculatorViewModel();

            // Parse model from query string
            if (!string.IsNullOrEmpty(model) && Enum.TryParse<QueueingModel>(model, true, out var parsed))
            {
                vm.Model = parsed;
            }

            return View(vm);
        }

        /// <summary>
        /// POST /Simulation/Index
        /// Validates inputs, runs the calculator service, returns results.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(CalculatorViewModel vm)
        {
            // Additional server-side validation
            if (vm.ArrivalValue <= 0)
                ModelState.AddModelError(nameof(vm.ArrivalValue), "Arrival value must be greater than zero.");

            if (vm.ServiceValue <= 0)
                ModelState.AddModelError(nameof(vm.ServiceValue), "Service value must be greater than zero.");

            // Validate uniform distribution: min < max
            bool needsArrivalUniform = (vm.Model == QueueingModel.GG1 || vm.Model == QueueingModel.GGS)
                && vm.ArrivalDistribution == DistributionType.Uniform;
            bool needsServiceUniform = (vm.Model != QueueingModel.MM1 && vm.Model != QueueingModel.MMS)
                && vm.ServiceDistribution == DistributionType.Uniform;

            if (needsArrivalUniform && vm.ArrivalMin >= vm.ArrivalMax)
                ModelState.AddModelError(nameof(vm.ArrivalMax), "Arrival Max must be greater than Arrival Min.");

            if (needsServiceUniform && vm.ServiceMin >= vm.ServiceMax)
                ModelState.AddModelError(nameof(vm.ServiceMax), "Service Max must be greater than Service Min.");

            // For multi-server models, validate servers > 0
            if ((vm.Model == QueueingModel.MMS || vm.Model == QueueingModel.MGS || vm.Model == QueueingModel.GGS)
                && vm.Servers < 1)
            {
                ModelState.AddModelError(nameof(vm.Servers), "Number of servers must be at least 1.");
            }

            // Clear irrelevant validation errors for fields not used by the selected model
            ClearIrrelevantErrors(vm);

            if (!ModelState.IsValid)
                return View(vm);

            // Run the calculator
            var result = _calculator.Calculate(vm);
            vm.Result = result;

            return View(vm);
        }

        /// <summary>
        /// Remove ModelState errors for fields that aren't relevant to the current model.
        /// E.g., don't require Uniform params when Exponential is selected.
        /// </summary>
        private void ClearIrrelevantErrors(CalculatorViewModel vm)
        {
            // Clear arrival distribution params if not needed
            if (vm.ArrivalDistribution != DistributionType.Uniform)
            {
                ModelState.Remove(nameof(vm.ArrivalMin));
                ModelState.Remove(nameof(vm.ArrivalMax));
            }
            if (vm.ArrivalDistribution != DistributionType.Normal)
            {
                ModelState.Remove(nameof(vm.ArrivalStdDev));
                ModelState.Remove(nameof(vm.ArrivalVariance));
            }

            // Clear service distribution params if not needed
            if (vm.ServiceDistribution != DistributionType.Uniform)
            {
                ModelState.Remove(nameof(vm.ServiceMin));
                ModelState.Remove(nameof(vm.ServiceMax));
            }
            if (vm.ServiceDistribution != DistributionType.Normal)
            {
                ModelState.Remove(nameof(vm.ServiceStdDev));
                ModelState.Remove(nameof(vm.ServiceVariance));
            }

            // For M/M models, clear all distribution params
            if (vm.Model == QueueingModel.MM1 || vm.Model == QueueingModel.MMS)
            {
                ModelState.Remove(nameof(vm.ServiceMin));
                ModelState.Remove(nameof(vm.ServiceMax));
                ModelState.Remove(nameof(vm.ServiceStdDev));
                ModelState.Remove(nameof(vm.ServiceVariance));
                ModelState.Remove(nameof(vm.ArrivalMin));
                ModelState.Remove(nameof(vm.ArrivalMax));
                ModelState.Remove(nameof(vm.ArrivalStdDev));
                ModelState.Remove(nameof(vm.ArrivalVariance));
            }

            // For single-server models, clear servers
            if (vm.Model == QueueingModel.MM1 || vm.Model == QueueingModel.MG1 || vm.Model == QueueingModel.GG1)
            {
                ModelState.Remove(nameof(vm.Servers));
            }
        }
    }
}
