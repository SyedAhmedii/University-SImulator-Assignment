using System.ComponentModel.DataAnnotations;
using FuelFlowQueueLab.Models.Enums;

namespace FuelFlowQueueLab.Models.ViewModels
{
    /// <summary>
    /// Input ViewModel for the Queueing Calculator page (/Simulation/Index).
    /// Contains all fields needed by every model; the view shows/hides fields
    /// dynamically based on the selected QueueingModel.
    /// </summary>
    public class CalculatorViewModel
    {
        // ── Model Selection ─────────────────────────────────────────────
        [Required]
        public QueueingModel Model { get; set; } = QueueingModel.MM1;

        // ── Core Inputs ─────────────────────────────────────────────────
        [Required]
        public TimeUnit TimeUnit { get; set; } = TimeUnit.Minutes;

        [Range(1, 1000000, ErrorMessage = "Number of customers must be between 1 and 1,000,000.")]
        public int NumberOfCustomers { get; set; } = 1;

        // ── Arrival Input ───────────────────────────────────────────────
        [Required]
        public InputMode ArrivalInputMode { get; set; } = InputMode.MeanTime;

        [Required(ErrorMessage = "Please enter the arrival value.")]
        [Range(0.0001, double.MaxValue, ErrorMessage = "Arrival value must be greater than zero.")]
        public double ArrivalValue { get; set; } = 3;

        // ── Service Input ───────────────────────────────────────────────
        [Required]
        public InputMode ServiceInputMode { get; set; } = InputMode.MeanTime;

        [Required(ErrorMessage = "Please enter the service value.")]
        [Range(0.0001, double.MaxValue, ErrorMessage = "Service value must be greater than zero.")]
        public double ServiceValue { get; set; } = 2;

        // ── Servers ─────────────────────────────────────────────────────
        [Range(1, 100, ErrorMessage = "Number of servers must be between 1 and 100.")]
        public int Servers { get; set; } = 1;

        // ── Distributions ───────────────────────────────────────────────
        public DistributionType ArrivalDistribution { get; set; } = DistributionType.Exponential;
        public DistributionType ServiceDistribution { get; set; } = DistributionType.Exponential;

        // ── Uniform Distribution Params ─────────────────────────────────
        [Range(0, double.MaxValue, ErrorMessage = "Arrival Min must be ≥ 0.")]
        public double ArrivalMin { get; set; }

        [Range(0.0001, double.MaxValue, ErrorMessage = "Arrival Max must be > 0.")]
        public double ArrivalMax { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Service Min must be ≥ 0.")]
        public double ServiceMin { get; set; }

        [Range(0.0001, double.MaxValue, ErrorMessage = "Service Max must be > 0.")]
        public double ServiceMax { get; set; }

        // ── Normal Distribution Params ──────────────────────────────────
        public bool UseVarianceForArrival { get; set; } = false;

        [Range(0, double.MaxValue, ErrorMessage = "Arrival Std Dev must be ≥ 0.")]
        public double ArrivalStdDev { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Arrival Variance must be ≥ 0.")]
        public double ArrivalVariance { get; set; }

        public bool UseVarianceForService { get; set; } = false;

        [Range(0, double.MaxValue, ErrorMessage = "Service Std Dev must be ≥ 0.")]
        public double ServiceStdDev { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Service Variance must be ≥ 0.")]
        public double ServiceVariance { get; set; }

        // ── Computed helpers (not bound, used by the view for re-rendering) ──
        public CalculatorResultViewModel? Result { get; set; }
    }
}
