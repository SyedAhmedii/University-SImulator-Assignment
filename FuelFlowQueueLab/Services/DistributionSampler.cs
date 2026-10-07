namespace FuelFlowQueueLab.Services
{
    /// <summary>
    /// Implementation of random variate generators for simulation.
    /// Each method generates a non-negative sample from the specified distribution.
    /// </summary>
    public class DistributionSampler : IDistributionSampler
    {
        /// <summary>
        /// Exponential distribution: f(x) = (1/mean) * exp(-x/mean)
        /// Uses inverse-transform: X = -mean * ln(U), where U ~ Uniform(0,1)
        /// </summary>
        public double SampleExponential(Random rng, double mean)
        {
            // U must be in (0,1] to avoid ln(0)
            double u = rng.NextDouble();
            while (u == 0.0) u = rng.NextDouble();
            double sample = -mean * Math.Log(u);
            return Math.Max(0, sample);
        }

        /// <summary>
        /// Uniform distribution on [min, max].
        /// X = min + (max - min) * U
        /// </summary>
        public double SampleUniform(Random rng, double min, double max)
        {
            double u = rng.NextDouble();
            double sample = min + (max - min) * u;
            return Math.Max(0, sample);
        }

        /// <summary>
        /// Normal distribution using Box–Muller transform.
        /// Generates two independent N(0,1) samples; we use one.
        /// Z = sqrt(-2 ln U1) * cos(2π U2)
        /// X = mean + stdDev * Z
        /// Floored at 0 to prevent negative interarrival/service times.
        /// </summary>
        public double SampleNormal(Random rng, double mean, double stdDev)
        {
            double u1 = rng.NextDouble();
            double u2 = rng.NextDouble();
            // Avoid ln(0)
            while (u1 == 0.0) u1 = rng.NextDouble();

            // Box–Muller transform
            double z = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
            double sample = mean + stdDev * z;
            return Math.Max(0, sample); // Floor at 0
        }

        /// <summary>
        /// Gamma distribution using Marsaglia and Tsang's method (2000).
        /// 
        /// For shape k ≥ 1:
        ///   d = k - 1/3
        ///   c = 1 / sqrt(9d)
        ///   Loop: generate Z ~ N(0,1), compute V = (1 + c*Z)^3
        ///     if V > 0 and ln(U) < 0.5*Z^2 + d - d*V + d*ln(V), accept X = d*V
        /// 
        /// For shape k < 1 (boost trick):
        ///   Sample Y ~ Gamma(k+1, 1), then X = Y * U^(1/k)
        ///   where U ~ Uniform(0,1)
        /// 
        /// Final sample = X * scale (θ)
        /// </summary>
        public double SampleGamma(Random rng, double shape, double scale)
        {
            double sample;

            if (shape < 1.0)
            {
                // Boost trick: Gamma(k) = Gamma(k+1) * U^(1/k)
                double boostU = rng.NextDouble();
                while (boostU == 0.0) boostU = rng.NextDouble();
                sample = SampleGammaInternal(rng, shape + 1.0) * Math.Pow(boostU, 1.0 / shape);
            }
            else
            {
                sample = SampleGammaInternal(rng, shape);
            }

            return Math.Max(0, sample * scale);
        }

        /// <summary>
        /// Marsaglia–Tsang method for Gamma(shape, 1) where shape ≥ 1.
        /// </summary>
        private double SampleGammaInternal(Random rng, double shape)
        {
            double d = shape - 1.0 / 3.0;
            double c = 1.0 / Math.Sqrt(9.0 * d);

            while (true)
            {
                double z, v;
                do
                {
                    // Generate Z ~ N(0,1) using Box–Muller
                    double u1 = rng.NextDouble();
                    double u2 = rng.NextDouble();
                    while (u1 == 0.0) u1 = rng.NextDouble();
                    z = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
                    v = 1.0 + c * z;
                } while (v <= 0.0);

                v = v * v * v; // V = (1 + c*Z)^3
                double u = rng.NextDouble();
                while (u == 0.0) u = rng.NextDouble();

                // Squeeze test + full acceptance check
                if (u < 1.0 - 0.0331 * (z * z) * (z * z))
                    return d * v;

                if (Math.Log(u) < 0.5 * z * z + d * (1.0 - v + Math.Log(v)))
                    return d * v;
            }
        }
    }
}
