using System.ComponentModel.DataAnnotations;
using FuelFlowQueueLab.Models.Enums;

namespace FuelFlowQueueLab.Models.ViewModels
{
    /// <summary>
    /// Input ViewModel for the Discrete Event Simulator page (/Simulator/Index).
    /// </summary>
    public class SimulatorInputViewModel
    {
        // ── General Settings ────────────────────────────────────────────
        [Required]
        [Range(1, 100, ErrorMessage = "Number of servers must be between 1 and 100.")]
        public int Servers { get; set; } = 1;

        [Required]
        public TimeUnit TimeUnit { get; set; } = TimeUnit.Minutes;

        [Required]
        [Range(1, 100000, ErrorMessage = "Number of customers must be between 1 and 100,000.")]
        public int NumberOfCustomers { get; set; } = 20;

        [Required]
        [Range(0.001, double.MaxValue, ErrorMessage = "Simulation duration must be > 0.")]
        public double SimulationDuration { get; set; } = 480;

        [Range(1, int.MaxValue, ErrorMessage = "Random seed must be a positive integer.")]
        public int RandomSeed { get; set; } = 42;

        // ── Arrival Distribution ────────────────────────────────────────
        [Required]
        public DistributionType ArrivalDistribution { get; set; } = DistributionType.Exponential;

        [Required]
        public InputMode ArrivalInputMode { get; set; } = InputMode.MeanTime;

        [Required(ErrorMessage = "Please enter the arrival value.")]
        [Range(0.0001, double.MaxValue, ErrorMessage = "Arrival value must be > 0.")]
        public double ArrivalValue { get; set; } = 3;

        // Arrival Uniform params
        [Range(0, double.MaxValue)]
        public double ArrivalMin { get; set; }

        [Range(0.0001, double.MaxValue)]
        public double ArrivalMax { get; set; }

        // Arrival Normal params
        public bool UseVarianceForArrival { get; set; } = false;

        [Range(0, double.MaxValue)]
        public double ArrivalStdDev { get; set; }

        [Range(0, double.MaxValue)]
        public double ArrivalVariance { get; set; }

        // ── Service Distribution ────────────────────────────────────────
        [Required]
        public DistributionType ServiceDistribution { get; set; } = DistributionType.Exponential;

        [Required]
        public InputMode ServiceInputMode { get; set; } = InputMode.MeanTime;

        [Required(ErrorMessage = "Please enter the service value.")]
        [Range(0.0001, double.MaxValue, ErrorMessage = "Service value must be > 0.")]
        public double ServiceValue { get; set; } = 2;

        // Service Uniform params
        [Range(0, double.MaxValue)]
        public double ServiceMin { get; set; }

        [Range(0.0001, double.MaxValue)]
        public double ServiceMax { get; set; }

        // Service Normal params
        public bool UseVarianceForService { get; set; } = false;

        [Range(0, double.MaxValue)]
        public double ServiceStdDev { get; set; }

        [Range(0, double.MaxValue)]
        public double ServiceVariance { get; set; }

        // Service Gamma params
        [Range(0.0001, double.MaxValue, ErrorMessage = "Gamma shape must be > 0.")]
        public double GammaShape { get; set; } = 1;

        [Range(0.0001, double.MaxValue, ErrorMessage = "Gamma scale must be > 0.")]
        public double GammaScale { get; set; } = 2;

        // ── Result (populated after simulation runs) ────────────────────
        public SimulatorResultViewModel? Result { get; set; }
    }
}
