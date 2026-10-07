using Microsoft.AspNetCore.Mvc;
using FuelFlowQueueLab.Models.Enums;
using FuelFlowQueueLab.Models.ViewModels;
using FuelFlowQueueLab.Services;

namespace FuelFlowQueueLab.Controllers
{
    public class SimulatorController : Controller
    {
        private readonly ISimulationEngine _engine;
        private readonly IQueueingCalculatorService _calculator;

        public SimulatorController(ISimulationEngine engine, IQueueingCalculatorService calculator)
        {
            _engine = engine;
            _calculator = calculator;
        }

        /// <summary>
        /// GET /Simulator/Index — shows the simulator input form.
        /// </summary>
        [HttpGet]
        public IActionResult Index()
        {
            var vm = new SimulatorInputViewModel();
            return View(vm);
        }

        /// <summary>
        /// POST /Simulator/Index — runs the DES and returns results.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(SimulatorInputViewModel vm)
        {
            // Additional validation
            if (vm.ArrivalDistribution == DistributionType.Uniform && vm.ArrivalMin >= vm.ArrivalMax)
                ModelState.AddModelError(nameof(vm.ArrivalMax), "Arrival Max must be > Min.");

            if (vm.ServiceDistribution == DistributionType.Uniform && vm.ServiceMin >= vm.ServiceMax)
                ModelState.AddModelError(nameof(vm.ServiceMax), "Service Max must be > Min.");

            // Clear irrelevant validation errors
            ClearIrrelevantErrors(vm);

            if (!ModelState.IsValid)
                return View(vm);

            // Run simulation
            var result = _engine.RunSimulation(vm);

            // Compute theoretical comparison values
            ComputeTheoryComparison(vm, result);

            vm.Result = result;
            return View(vm);
        }

        /// <summary>
        /// Compute theoretical queueing metrics and attach to simulation results
        /// so the user can compare simulated vs. theoretical values.
        /// </summary>
        private void ComputeTheoryComparison(SimulatorInputViewModel simInput, SimulatorResultViewModel result)
        {
            try
            {
                // Map simulator input to calculator input
                var calcInput = new CalculatorViewModel
                {
                    ArrivalInputMode = simInput.ArrivalInputMode,
                    ArrivalValue = simInput.ArrivalValue,
                    ServiceInputMode = simInput.ServiceInputMode,
                    ServiceValue = simInput.ServiceValue,
                    Servers = simInput.Servers,
                    TimeUnit = simInput.TimeUnit,
                    ArrivalDistribution = simInput.ArrivalDistribution,
                    ServiceDistribution = simInput.ServiceDistribution == DistributionType.Gamma
                        ? DistributionType.Exponential // Gamma isn't in calculator; approximate
                        : simInput.ServiceDistribution,
                    ArrivalMin = simInput.ArrivalMin,
                    ArrivalMax = simInput.ArrivalMax,
                    ArrivalStdDev = simInput.ArrivalStdDev,
                    ArrivalVariance = simInput.ArrivalVariance,
                    UseVarianceForArrival = simInput.UseVarianceForArrival,
                    ServiceMin = simInput.ServiceMin,
                    ServiceMax = simInput.ServiceMax,
                    ServiceStdDev = simInput.ServiceStdDev,
                    ServiceVariance = simInput.ServiceVariance,
                    UseVarianceForService = simInput.UseVarianceForService,
                };

                // Determine the model
                bool arrM = simInput.ArrivalDistribution == DistributionType.Exponential;
                bool srvM = simInput.ServiceDistribution == DistributionType.Exponential;
                bool single = simInput.Servers == 1;

                if (arrM && srvM && single) calcInput.Model = QueueingModel.MM1;
                else if (arrM && srvM) calcInput.Model = QueueingModel.MMS;
                else if (arrM && single) calcInput.Model = QueueingModel.MG1;
                else if (arrM) calcInput.Model = QueueingModel.MGS;
                else if (single) calcInput.Model = QueueingModel.GG1;
                else calcInput.Model = QueueingModel.GGS;

                var calcResult = _calculator.Calculate(calcInput);

                if (calcResult.IsStable)
                {
                    result.HasTheoryComparison = true;
                    result.TheoryLq = calcResult.Lq;
                    result.TheoryL = calcResult.L;
                    result.TheoryWq = calcResult.Wq;
                    result.TheoryW = calcResult.W;
                    result.TheoryUtilization = calcResult.UtilizationPercent;
                }
            }
            catch
            {
                // If theory calculation fails, just skip the comparison
                result.HasTheoryComparison = false;
            }
        }

        private void ClearIrrelevantErrors(SimulatorInputViewModel vm)
        {
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
            if (vm.ServiceDistribution != DistributionType.Gamma)
            {
                ModelState.Remove(nameof(vm.GammaShape));
                ModelState.Remove(nameof(vm.GammaScale));
            }
        }
    }
}
