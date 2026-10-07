namespace FuelFlowQueueLab.Models.ViewModels
{
    /// <summary>
    /// Output ViewModel for the Discrete Event Simulator.
    /// Contains summary stats, customer records, chart data, and theory comparison.
    /// </summary>
    public class SimulatorResultViewModel
    {
        // ── Summary Statistics ──────────────────────────────────────────
        public int TotalCustomersServed { get; set; }
        public double AvgWaitTime { get; set; }           // Wq
        public double AvgTimeInSystem { get; set; }       // W
        public double AvgQueueLength { get; set; }        // Lq (time-weighted)
        public double AvgSystemLength { get; set; }       // L (time-weighted)
        public double MaxQueueLength { get; set; }
        public double PercentWhoWaited { get; set; }      // % of customers with wait > 0
        public double AvgServiceTimeGenerated { get; set; }
        public double AvgInterarrivalTimeGenerated { get; set; }
        public double SimulationEndTime { get; set; }
        public string TimeUnitLabel { get; set; } = string.Empty;

        // ── Per-Server Utilization ──────────────────────────────────────
        public List<double> ServerUtilizations { get; set; } = new();
        public double OverallUtilization { get; set; }

        // ── Customer Records ────────────────────────────────────────────
        public List<CustomerRecord> Customers { get; set; } = new();

        // ── Chart Data (JSON-ready arrays) ──────────────────────────────
        public List<double> ChartQueueTimes { get; set; } = new();    // x-axis: event times
        public List<int> ChartQueueLengths { get; set; } = new();     // y-axis: queue length at each event
        public List<double> ChartWaitTimes { get; set; } = new();     // per-customer waiting times

        // ── Detected Model ──────────────────────────────────────────────
        public string DetectedModel { get; set; } = string.Empty;

        // ── Simulation vs Theory Comparison ─────────────────────────────
        public bool HasTheoryComparison { get; set; }
        public double TheoryLq { get; set; }
        public double TheoryL { get; set; }
        public double TheoryWq { get; set; }
        public double TheoryW { get; set; }
        public double TheoryUtilization { get; set; }
    }
}
