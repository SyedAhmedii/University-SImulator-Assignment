using System.Text;
using FuelFlowQueueLab.Models.Enums;
using FuelFlowQueueLab.Models.ViewModels;

namespace FuelFlowQueueLab.Services
{
    /// <summary>
    /// Queueing Calculator Service — implements all six queueing model formulas.
    /// Each method is heavily commented so students can follow the derivation.
    /// </summary>
    public class QueueingCalculatorService : IQueueingCalculatorService
    {
        public CalculatorResultViewModel Calculate(CalculatorViewModel input)
        {
            var result = new CalculatorResultViewModel();
            var steps = new StringBuilder();

            // ── Step 1: Compute λ (arrival rate) and μ (service rate) ───
            double lambda, mu;

            if (input.ArrivalInputMode == InputMode.MeanTime)
            {
                lambda = 1.0 / input.ArrivalValue;
                steps.AppendLine($"λ = 1 / Mean Interarrival Time = 1 / {input.ArrivalValue:F4} = {lambda:F4}");
            }
            else
            {
                lambda = input.ArrivalValue;
                steps.AppendLine($"λ = {lambda:F4} (entered directly as arrival rate)");
            }

            if (input.ServiceInputMode == InputMode.MeanTime)
            {
                mu = 1.0 / input.ServiceValue;
                steps.AppendLine($"μ = 1 / Mean Service Time = 1 / {input.ServiceValue:F4} = {mu:F4}");
            }
            else
            {
                mu = input.ServiceValue;
                steps.AppendLine($"μ = {mu:F4} (entered directly as service rate)");
            }

            int s = input.Servers;
            if (input.Model == QueueingModel.MM1 || input.Model == QueueingModel.MG1 || input.Model == QueueingModel.GG1)
            {
                s = 1; // Force single server for single-server models
            }

            // ── Step 2: Compute ρ (utilization) ─────────────────────────
            double rho = lambda / (s * mu);
            steps.AppendLine($"ρ = λ / (s × μ) = {lambda:F4} / ({s} × {mu:F4}) = {rho:F4}");

            // ── Check stability ─────────────────────────────────────────
            if (rho >= 1.0)
            {
                result.IsStable = false;
                result.ErrorMessage = $"System is UNSTABLE: ρ = {rho:F4} ≥ 1. " +
                    "The arrival rate exceeds the system's capacity. " +
                    "The queue will grow infinitely. Reduce λ, increase μ, or add more servers.";
                result.Lambda = lambda;
                result.Mu = mu;
                result.Rho = rho;
                result.Servers = s;
                result.FormulaSteps = steps.ToString();
                SetTimeUnitLabel(result, input.TimeUnit);
                return result;
            }

            // ── Step 3: Compute distribution parameters (Ca², Cs²) ─────
            double caSquared = ComputeCaSquared(input, lambda, steps);
            double csSquared = ComputeCsSquared(input, mu, steps);

            // ── Step 4: Compute model-specific metrics ──────────────────
            steps.AppendLine();
            steps.AppendLine($"═══ Model: {GetModelDisplayName(input.Model)} ═══");

            switch (input.Model)
            {
                case QueueingModel.MM1:
                    CalculateMM1(lambda, mu, rho, result, steps);
                    break;
                case QueueingModel.MMS:
                    CalculateMMS(lambda, mu, rho, s, result, steps);
                    break;
                case QueueingModel.MG1:
                    CalculateMG1(lambda, mu, rho, csSquared, input, result, steps);
                    break;
                case QueueingModel.MGS:
                    CalculateMGS(lambda, mu, rho, s, csSquared, result, steps);
                    break;
                case QueueingModel.GG1:
                    CalculateGG1(lambda, mu, rho, caSquared, csSquared, result, steps);
                    break;
                case QueueingModel.GGS:
                    CalculateGGS(lambda, mu, rho, s, caSquared, csSquared, result, steps);
                    break;
            }

            // ── Populate common result fields ───────────────────────────
            result.Lambda = lambda;
            result.Mu = mu;
            result.Rho = rho;
            result.Servers = s;
            result.CaSquared = caSquared;
            result.CsSquared = csSquared;
            result.ModelName = GetModelDisplayName(input.Model);
            result.UtilizationPercent = rho * 100.0;
            result.FormulaSteps = steps.ToString();
            SetTimeUnitLabel(result, input.TimeUnit);

            return result;
        }

        // ═══════════════════════════════════════════════════════════════
        //  M/M/1  —  Single server, Poisson arrivals, Exponential service
        // ═══════════════════════════════════════════════════════════════
        private void CalculateMM1(double lambda, double mu, double rho,
            CalculatorResultViewModel result, StringBuilder steps)
        {
            // P0 = 1 - ρ (probability system is empty)
            double p0 = 1.0 - rho;
            steps.AppendLine($"P₀ = 1 - ρ = 1 - {rho:F4} = {p0:F4}");

            // Lq = ρ² / (1 - ρ)
            double lq = (rho * rho) / (1.0 - rho);
            steps.AppendLine($"Lq = ρ² / (1 - ρ) = {rho:F4}² / (1 - {rho:F4}) = {lq:F4}");

            // L = ρ / (1 - ρ)
            double l = rho / (1.0 - rho);
            steps.AppendLine($"L = ρ / (1 - ρ) = {rho:F4} / (1 - {rho:F4}) = {l:F4}");

            // Wq = ρ / (μ - λ)
            double wq = rho / (mu - lambda);
            steps.AppendLine($"Wq = ρ / (μ - λ) = {rho:F4} / ({mu:F4} - {lambda:F4}) = {wq:F4}");

            // W = 1 / (μ - λ)
            double w = 1.0 / (mu - lambda);
            steps.AppendLine($"W = 1 / (μ - λ) = 1 / ({mu:F4} - {lambda:F4}) = {w:F4}");

            // Verification via Little's Law
            steps.AppendLine($"Verify: Lq = λ × Wq = {lambda:F4} × {wq:F4} = {lambda * wq:F4} ✓");
            steps.AppendLine($"Verify: L = λ × W = {lambda:F4} × {w:F4} = {lambda * w:F4} ✓");

            result.P0 = p0;
            result.IdleProbability = p0;
            result.Lq = lq;
            result.L = l;
            result.Wq = wq;
            result.W = w;
            result.IsStable = true;
        }

        // ═══════════════════════════════════════════════════════════════
        //  M/M/s  —  Multiple servers, Poisson arrivals, Exponential service
        // ═══════════════════════════════════════════════════════════════
        private void CalculateMMS(double lambda, double mu, double rho, int s,
            CalculatorResultViewModel result, StringBuilder steps)
        {
            double a = lambda / mu;  // offered load (traffic intensity)
            steps.AppendLine($"a = λ / μ = {lambda:F4} / {mu:F4} = {a:F4} (offered load)");
            steps.AppendLine($"ρ = a / s = {a:F4} / {s} = {rho:F4}");

            // ── P0: Probability system is empty ─────────────────────────
            // P0 = [ Σ_{n=0}^{s-1} (a^n / n!) + (a^s / (s! × (1-ρ))) ]^(-1)
            double sumPart = 0;
            for (int n = 0; n < s; n++)
            {
                sumPart += Math.Pow(a, n) / Factorial(n);
            }
            double lastTerm = Math.Pow(a, s) / (Factorial(s) * (1.0 - rho));
            double p0 = 1.0 / (sumPart + lastTerm);

            steps.AppendLine($"P₀ = [Σ(n=0 to {s - 1}) aⁿ/n! + aˢ/(s!·(1-ρ))]⁻¹");
            steps.AppendLine($"   = [{sumPart:F4} + {lastTerm:F4}]⁻¹ = {p0:F6}");

            // ── Erlang-C: C(s, a) ───────────────────────────────────────
            // C(s,a) = (a^s / (s! × (1-ρ))) × P0
            double erlangC = lastTerm * p0;
            steps.AppendLine($"C(s,a) = (aˢ/(s!·(1-ρ))) × P₀ = {lastTerm:F4} × {p0:F6} = {erlangC:F6}");

            // ── Lq = C(s,a) × ρ / (1 - ρ) ──────────────────────────────
            double lq = erlangC * rho / (1.0 - rho);
            steps.AppendLine($"Lq = C(s,a) × ρ / (1-ρ) = {erlangC:F6} × {rho:F4} / (1-{rho:F4}) = {lq:F4}");

            // ── Wq = Lq / λ (Little's Law) ──────────────────────────────
            double wq = lq / lambda;
            steps.AppendLine($"Wq = Lq / λ = {lq:F4} / {lambda:F4} = {wq:F4}");

            // ── W = Wq + 1/μ ────────────────────────────────────────────
            double w = wq + 1.0 / mu;
            steps.AppendLine($"W = Wq + 1/μ = {wq:F4} + {1.0 / mu:F4} = {w:F4}");

            // ── L = Lq + a ──────────────────────────────────────────────
            double l = lq + a;
            steps.AppendLine($"L = Lq + a = {lq:F4} + {a:F4} = {l:F4}");

            result.P0 = p0;
            result.ErlangC = erlangC;
            result.IdleProbability = p0;
            result.Lq = lq;
            result.L = l;
            result.Wq = wq;
            result.W = w;
            result.IsStable = true;
        }

        // ═══════════════════════════════════════════════════════════════
        //  M/G/1  —  Pollaczek–Khinchine formula
        //  Single server, Poisson arrivals, General service distribution
        // ═══════════════════════════════════════════════════════════════
        private void CalculateMG1(double lambda, double mu, double rho, double csSquared,
            CalculatorViewModel input, CalculatorResultViewModel result, StringBuilder steps)
        {
            // Service variance (σ²_s) depends on chosen distribution
            double serviceVariance = ComputeServiceVariance(input, mu);
            double meanService = 1.0 / mu;

            steps.AppendLine($"Mean service time (1/μ) = {meanService:F4}");
            steps.AppendLine($"Service variance (σ²_s) = {serviceVariance:F6}");
            steps.AppendLine($"Cs² = σ²_s / (1/μ)² = {serviceVariance:F6} / {meanService * meanService:F6} = {csSquared:F4}");

            // P0 = 1 - ρ
            double p0 = 1.0 - rho;
            steps.AppendLine($"P₀ = 1 - ρ = {p0:F4}");

            // Pollaczek–Khinchine: Lq = (λ²σ²_s + ρ²) / (2(1-ρ))
            double lq = (lambda * lambda * serviceVariance + rho * rho) / (2.0 * (1.0 - rho));
            steps.AppendLine($"Lq = (λ²·σ²_s + ρ²) / (2·(1-ρ))");
            steps.AppendLine($"   = ({lambda:F4}² × {serviceVariance:F6} + {rho:F4}²) / (2 × (1-{rho:F4}))");
            steps.AppendLine($"   = ({lambda * lambda * serviceVariance:F6} + {rho * rho:F6}) / {2.0 * (1.0 - rho):F6}");
            steps.AppendLine($"   = {lq:F4}");

            // Wq = Lq / λ (Little's Law)
            double wq = lq / lambda;
            steps.AppendLine($"Wq = Lq / λ = {lq:F4} / {lambda:F4} = {wq:F4}");

            // W = Wq + 1/μ
            double w = wq + meanService;
            steps.AppendLine($"W = Wq + 1/μ = {wq:F4} + {meanService:F4} = {w:F4}");

            // L = λ × W (Little's Law)
            double l = lambda * w;
            steps.AppendLine($"L = λ × W = {lambda:F4} × {w:F4} = {l:F4}");

            result.P0 = p0;
            result.IdleProbability = p0;
            result.Lq = lq;
            result.L = l;
            result.Wq = wq;
            result.W = w;
            result.IsStable = true;
        }

        // ═══════════════════════════════════════════════════════════════
        //  M/G/s  —  Allen–Cunneen / Lee–Longton approximation
        //  Multiple servers, Poisson arrivals, General service
        // ═══════════════════════════════════════════════════════════════
        private void CalculateMGS(double lambda, double mu, double rho, int s,
            double csSquared, CalculatorResultViewModel result, StringBuilder steps)
        {
            steps.AppendLine("Using Allen–Cunneen approximation:");
            steps.AppendLine("Wq(M/G/s) ≈ Wq(M/M/s) × (1 + Cs²) / 2");
            steps.AppendLine();

            // First compute M/M/s Wq as the base
            steps.AppendLine("─── Base M/M/s calculation ───");
            var mmsResult = new CalculatorResultViewModel();
            CalculateMMS(lambda, mu, rho, s, mmsResult, steps);

            double wqMMS = mmsResult.Wq;
            steps.AppendLine();
            steps.AppendLine("─── Allen–Cunneen correction ───");

            // Wq ≈ Wq(M/M/s) × (1 + Cs²) / 2
            double correctionFactor = (1.0 + csSquared) / 2.0;
            double wq = wqMMS * correctionFactor;
            steps.AppendLine($"Correction factor = (1 + Cs²) / 2 = (1 + {csSquared:F4}) / 2 = {correctionFactor:F4}");
            steps.AppendLine($"Wq = Wq(M/M/s) × correction = {wqMMS:F4} × {correctionFactor:F4} = {wq:F4}");

            // Little's Law derivations
            double lq = lambda * wq;
            double w = wq + 1.0 / mu;
            double l = lambda * w;
            steps.AppendLine($"Lq = λ × Wq = {lambda:F4} × {wq:F4} = {lq:F4}");
            steps.AppendLine($"W = Wq + 1/μ = {wq:F4} + {1.0 / mu:F4} = {w:F4}");
            steps.AppendLine($"L = λ × W = {lambda:F4} × {w:F4} = {l:F4}");

            result.P0 = mmsResult.P0;
            result.ErlangC = mmsResult.ErlangC;
            result.IdleProbability = mmsResult.P0;
            result.Lq = lq;
            result.L = l;
            result.Wq = wq;
            result.W = w;
            result.IsStable = true;
        }

        // ═══════════════════════════════════════════════════════════════
        //  G/G/1  —  Kingman's approximation (heavy-traffic)
        //  Single server, General arrivals, General service
        // ═══════════════════════════════════════════════════════════════
        private void CalculateGG1(double lambda, double mu, double rho,
            double caSquared, double csSquared,
            CalculatorResultViewModel result, StringBuilder steps)
        {
            steps.AppendLine("Using Kingman's approximation (heavy-traffic formula):");
            steps.AppendLine("Wq ≈ (ρ / (1-ρ)) × ((Ca² + Cs²) / 2) × (1/μ)");

            double p0 = 1.0 - rho;

            // Wq ≈ (ρ/(1-ρ)) × ((Ca²+Cs²)/2) × (1/μ)
            double wq = (rho / (1.0 - rho)) * ((caSquared + csSquared) / 2.0) * (1.0 / mu);
            steps.AppendLine($"Wq = ({rho:F4} / (1-{rho:F4})) × (({caSquared:F4} + {csSquared:F4}) / 2) × (1/{mu:F4})");
            steps.AppendLine($"   = {rho / (1.0 - rho):F4} × {(caSquared + csSquared) / 2.0:F4} × {1.0 / mu:F4}");
            steps.AppendLine($"   = {wq:F4}");

            // Little's Law
            double lq = lambda * wq;
            double w = wq + 1.0 / mu;
            double l = lambda * w;
            steps.AppendLine($"Lq = λ × Wq = {lambda:F4} × {wq:F4} = {lq:F4}");
            steps.AppendLine($"W = Wq + 1/μ = {wq:F4} + {1.0 / mu:F4} = {w:F4}");
            steps.AppendLine($"L = λ × W = {lambda:F4} × {w:F4} = {l:F4}");

            result.P0 = p0;
            result.IdleProbability = p0;
            result.Lq = lq;
            result.L = l;
            result.Wq = wq;
            result.W = w;
            result.IsStable = true;
        }

        // ═══════════════════════════════════════════════════════════════
        //  G/G/s  —  Allen–Cunneen approximation
        //  Multiple servers, General arrivals, General service
        // ═══════════════════════════════════════════════════════════════
        private void CalculateGGS(double lambda, double mu, double rho, int s,
            double caSquared, double csSquared,
            CalculatorResultViewModel result, StringBuilder steps)
        {
            steps.AppendLine("Using Allen–Cunneen approximation:");
            steps.AppendLine("Wq(G/G/s) ≈ Wq(M/M/s) × (Ca² + Cs²) / 2");
            steps.AppendLine();

            // Base M/M/s calculation
            steps.AppendLine("─── Base M/M/s calculation ───");
            var mmsResult = new CalculatorResultViewModel();
            CalculateMMS(lambda, mu, rho, s, mmsResult, steps);

            double wqMMS = mmsResult.Wq;
            steps.AppendLine();
            steps.AppendLine("─── Allen–Cunneen correction ───");

            // Wq ≈ Wq(M/M/s) × (Ca² + Cs²) / 2
            double correctionFactor = (caSquared + csSquared) / 2.0;
            double wq = wqMMS * correctionFactor;
            steps.AppendLine($"Correction factor = (Ca² + Cs²) / 2 = ({caSquared:F4} + {csSquared:F4}) / 2 = {correctionFactor:F4}");
            steps.AppendLine($"Wq = Wq(M/M/s) × correction = {wqMMS:F4} × {correctionFactor:F4} = {wq:F4}");

            // Little's Law
            double lq = lambda * wq;
            double w = wq + 1.0 / mu;
            double l = lambda * w;
            steps.AppendLine($"Lq = λ × Wq = {lambda:F4} × {wq:F4} = {lq:F4}");
            steps.AppendLine($"W = Wq + 1/μ = {wq:F4} + {1.0 / mu:F4} = {w:F4}");
            steps.AppendLine($"L = λ × W = {lambda:F4} × {w:F4} = {l:F4}");

            result.P0 = mmsResult.P0;
            result.ErlangC = mmsResult.ErlangC;
            result.IdleProbability = mmsResult.P0;
            result.Lq = lq;
            result.L = l;
            result.Wq = wq;
            result.W = w;
            result.IsStable = true;
        }

        // ═══════════════════════════════════════════════════════════════
        //  HELPER METHODS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Compute Ca² (squared coefficient of variation for arrivals).
        /// For Markovian (M): Ca²=1. For General (G): computed from distribution params.
        /// </summary>
        private double ComputeCaSquared(CalculatorViewModel input, double lambda, StringBuilder steps)
        {
            // M/M/x and M/G/x models have Poisson arrivals → Ca² = 1
            if (input.Model == QueueingModel.MM1 || input.Model == QueueingModel.MMS ||
                input.Model == QueueingModel.MG1 || input.Model == QueueingModel.MGS)
            {
                steps.AppendLine("Ca² = 1.0 (Poisson/Exponential arrivals — Markovian)");
                return 1.0;
            }

            double meanInterarrival = 1.0 / lambda;
            double variance;

            switch (input.ArrivalDistribution)
            {
                case DistributionType.Exponential:
                    steps.AppendLine("Ca² = 1.0 (Exponential arrivals)");
                    return 1.0;

                case DistributionType.Uniform:
                    // Uniform(a,b): mean=(a+b)/2, variance=(b-a)²/12
                    double a = input.ArrivalMin;
                    double b = input.ArrivalMax;
                    double uniformMean = (a + b) / 2.0;
                    variance = (b - a) * (b - a) / 12.0;
                    double caUniform = variance / (uniformMean * uniformMean);
                    steps.AppendLine($"Arrival Uniform({a:F4}, {b:F4}): mean={uniformMean:F4}, var={variance:F6}");
                    steps.AppendLine($"Ca² = var / mean² = {variance:F6} / {uniformMean * uniformMean:F6} = {caUniform:F4}");
                    return caUniform;

                case DistributionType.Normal:
                    double stdDev = input.UseVarianceForArrival
                        ? Math.Sqrt(input.ArrivalVariance)
                        : input.ArrivalStdDev;
                    variance = stdDev * stdDev;
                    double caNormal = variance / (meanInterarrival * meanInterarrival);
                    steps.AppendLine($"Arrival Normal(mean={meanInterarrival:F4}, σ={stdDev:F4}): var={variance:F6}");
                    steps.AppendLine($"Ca² = var / mean² = {variance:F6} / {meanInterarrival * meanInterarrival:F6} = {caNormal:F4}");
                    return caNormal;

                default:
                    return 1.0;
            }
        }

        /// <summary>
        /// Compute Cs² (squared coefficient of variation for service).
        /// For Markovian service (M): Cs²=1. For General (G): computed from distribution params.
        /// </summary>
        private double ComputeCsSquared(CalculatorViewModel input, double mu, StringBuilder steps)
        {
            // M/M/x models have Exponential service → Cs² = 1
            if (input.Model == QueueingModel.MM1 || input.Model == QueueingModel.MMS)
            {
                steps.AppendLine("Cs² = 1.0 (Exponential service — Markovian)");
                return 1.0;
            }

            double meanService = 1.0 / mu;
            double variance;

            switch (input.ServiceDistribution)
            {
                case DistributionType.Exponential:
                    steps.AppendLine("Cs² = 1.0 (Exponential service)");
                    return 1.0;

                case DistributionType.Uniform:
                    double a = input.ServiceMin;
                    double b = input.ServiceMax;
                    double uniformMean = (a + b) / 2.0;
                    variance = (b - a) * (b - a) / 12.0;
                    double csUniform = variance / (uniformMean * uniformMean);
                    steps.AppendLine($"Service Uniform({a:F4}, {b:F4}): mean={uniformMean:F4}, var={variance:F6}");
                    steps.AppendLine($"Cs² = var / mean² = {variance:F6} / {uniformMean * uniformMean:F6} = {csUniform:F4}");
                    return csUniform;

                case DistributionType.Normal:
                    double stdDev = input.UseVarianceForService
                        ? Math.Sqrt(input.ServiceVariance)
                        : input.ServiceStdDev;
                    variance = stdDev * stdDev;
                    double csNormal = variance / (meanService * meanService);
                    steps.AppendLine($"Service Normal(mean={meanService:F4}, σ={stdDev:F4}): var={variance:F6}");
                    steps.AppendLine($"Cs² = var / mean² = {variance:F6} / {meanService * meanService:F6} = {csNormal:F4}");
                    return csNormal;

                default:
                    return 1.0;
            }
        }

        /// <summary>
        /// Compute the actual service time variance for M/G/1 formula.
        /// </summary>
        private double ComputeServiceVariance(CalculatorViewModel input, double mu)
        {
            double meanService = 1.0 / mu;

            switch (input.ServiceDistribution)
            {
                case DistributionType.Exponential:
                    // Exponential: variance = mean²
                    return meanService * meanService;

                case DistributionType.Uniform:
                    double a = input.ServiceMin;
                    double b = input.ServiceMax;
                    return (b - a) * (b - a) / 12.0;

                case DistributionType.Normal:
                    if (input.UseVarianceForService)
                        return input.ServiceVariance;
                    return input.ServiceStdDev * input.ServiceStdDev;

                default:
                    return meanService * meanService;
            }
        }

        /// <summary>
        /// Factorial for small n (used in P0 and Erlang-C calculations).
        /// </summary>
        private double Factorial(int n)
        {
            if (n <= 1) return 1.0;
            double result = 1.0;
            for (int i = 2; i <= n; i++)
                result *= i;
            return result;
        }

        /// <summary>
        /// Returns a human-readable model name in Kendall's notation.
        /// </summary>
        public static string GetModelDisplayName(QueueingModel model)
        {
            return model switch
            {
                QueueingModel.MM1 => "M/M/1",
                QueueingModel.MG1 => "M/G/1",
                QueueingModel.MMS => "M/M/s",
                QueueingModel.MGS => "M/G/s",
                QueueingModel.GG1 => "G/G/1",
                QueueingModel.GGS => "G/G/s",
                _ => model.ToString()
            };
        }

        private void SetTimeUnitLabel(CalculatorResultViewModel result, TimeUnit unit)
        {
            result.TimeUnitLabel = unit switch
            {
                TimeUnit.Seconds => "sec",
                TimeUnit.Minutes => "min",
                TimeUnit.Hours => "hr",
                _ => "min"
            };
        }
    }
}
