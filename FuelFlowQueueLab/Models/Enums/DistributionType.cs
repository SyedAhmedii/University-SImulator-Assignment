namespace FuelFlowQueueLab.Models.Enums
{
    /// <summary>
    /// Distribution types for arrival and service random variates.
    /// </summary>
    public enum DistributionType
    {
        Exponential,  // Memoryless, C=1
        Uniform,      // Uniform(a,b)
        Normal,       // Normal(μ, σ)
        Gamma         // Gamma(k, θ) — only for service in simulator
    }
}
