namespace FuelFlowQueueLab.Services
{
    /// <summary>
    /// Provides random variate sampling for various probability distributions.
    /// Used by both the Queueing Calculator (for computing distribution parameters)
    /// and the Simulation Engine (for generating interarrival/service times).
    /// </summary>
    public interface IDistributionSampler
    {
        /// <summary>
        /// Samples from an Exponential distribution: -mean * ln(U).
        /// </summary>
        double SampleExponential(Random rng, double mean);

        /// <summary>
        /// Samples from a Uniform distribution: a + (b-a)*U.
        /// </summary>
        double SampleUniform(Random rng, double min, double max);

        /// <summary>
        /// Samples from a Normal distribution using the Box–Muller transform.
        /// Result is floored at 0 (no negative times).
        /// </summary>
        double SampleNormal(Random rng, double mean, double stdDev);

        /// <summary>
        /// Samples from a Gamma distribution using Marsaglia–Tsang's method.
        /// For shape k &lt; 1, uses the boost trick: Gamma(k) = Gamma(k+1) * U^(1/k).
        /// Result is floored at 0.
        /// </summary>
        double SampleGamma(Random rng, double shape, double scale);
    }
}
