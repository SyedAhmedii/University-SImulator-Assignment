namespace FuelFlowQueueLab.Models.ViewModels
{
    /// <summary>
    /// Output ViewModel containing all computed queueing metrics.
    /// Populated by QueueingCalculatorService and displayed in the results section.
    /// </summary>
    public class CalculatorResultViewModel
    {
        // ── Input Echo ──────────────────────────────────────────────────
        public string ModelName { get; set; } = string.Empty;       // e.g. "M/M/1"
        public string TimeUnitLabel { get; set; } = string.Empty;   // e.g. "min"

        // ── Core Rates ──────────────────────────────────────────────────
        public double Lambda { get; set; }   // λ — arrival rate
        public double Mu { get; set; }       // μ — service rate
        public double Rho { get; set; }      // ρ — utilization = λ / (s·μ)
        public int Servers { get; set; }     // s

        // ── Coefficients of Variation Squared ───────────────────────────
        public double CaSquared { get; set; } // Ca² (arrival)
        public double CsSquared { get; set; } // Cs² (service)

        // ── Probabilities ───────────────────────────────────────────────
        public double P0 { get; set; }        // Probability system is empty
        public double ErlangC { get; set; }   // C(s,a) — Erlang-C value (M/M/s)

        // ── Performance Metrics ─────────────────────────────────────────
        public double Lq { get; set; }        // Average number in queue
        public double L { get; set; }         // Average number in system
        public double Wq { get; set; }        // Average waiting time in queue
        public double W { get; set; }         // Average time in system

        // ── Derived ─────────────────────────────────────────────────────
        public double UtilizationPercent { get; set; }  // ρ × 100
        public double IdleProbability { get; set; }     // 1 − ρ (or P0 for multi)

        // ── Stability ───────────────────────────────────────────────────
        public bool IsStable { get; set; } = true;
        public string ErrorMessage { get; set; } = string.Empty;

        // ── Formula Steps ───────────────────────────────────────────────
        /// <summary>
        /// Step-by-step formula derivation with substituted numeric values,
        /// formatted as HTML for display. Students can follow along to verify.
        /// </summary>
        public string FormulaSteps { get; set; } = string.Empty;
    }
}
