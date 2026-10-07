using FuelFlowQueueLab.Models.ViewModels;

namespace FuelFlowQueueLab.Services
{
    /// <summary>
    /// Computes queueing theory metrics for all six models:
    /// M/M/1, M/G/1, M/M/s, M/G/s, G/G/1, G/G/s.
    /// </summary>
    public interface IQueueingCalculatorService
    {
        /// <summary>
        /// Given user inputs (model, rates, distributions, servers),
        /// computes all performance metrics (Lq, L, Wq, W, P0, utilization, etc.)
        /// along with step-by-step formula derivation.
        /// </summary>
        CalculatorResultViewModel Calculate(CalculatorViewModel input);
    }
}
