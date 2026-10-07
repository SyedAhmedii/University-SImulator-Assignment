using FuelFlowQueueLab.Models.ViewModels;

namespace FuelFlowQueueLab.Services
{
    /// <summary>
    /// Runs a Discrete Event Simulation (DES) for a queueing system (petrol pump).
    /// </summary>
    public interface ISimulationEngine
    {
        /// <summary>
        /// Executes the simulation using a Future Event List and returns
        /// detailed per-customer records plus summary statistics.
        /// </summary>
        SimulatorResultViewModel RunSimulation(SimulatorInputViewModel input);
    }
}
