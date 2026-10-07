using FuelFlowQueueLab.Models.Enums;
using FuelFlowQueueLab.Models.ViewModels;

namespace FuelFlowQueueLab.Services
{
    /// <summary>
    /// Discrete Event Simulation engine for a multi-server queueing system.
    /// 
    /// Architecture:
    /// - Future Event List (FEL): PriorityQueue sorted by event time
    /// - Event types: Arrival and Departure
    /// - FIFO single queue → first free server assignment
    /// - Seeded RNG for reproducibility
    /// 
    /// Students: This is a proper next-event time-advance DES.
    /// Each event is processed in chronological order. Between events,
    /// the system state doesn't change (that's the "discrete" part).
    /// </summary>
    public class SimulationEngine : ISimulationEngine
    {
        private readonly IDistributionSampler _sampler;

        public SimulationEngine(IDistributionSampler sampler)
        {
            _sampler = sampler;
        }

        // ── Event types ─────────────────────────────────────────────────
        private enum EventType { Arrival, Departure }

        /// <summary>
        /// An event in the Future Event List.
        /// </summary>
        private class SimEvent
        {
            public EventType Type { get; set; }
            public double Time { get; set; }
            public int CustomerNumber { get; set; }
            public int ServerIndex { get; set; } // For departures: which server finishes
        }

        /// <summary>
        /// Waiting customer in the FIFO queue.
        /// </summary>
        private class WaitingCustomer
        {
            public int CustomerNumber { get; set; }
            public double ArrivalTime { get; set; }
            public double ServiceTime { get; set; }
            public double InterarrivalTime { get; set; }
        }

        public SimulatorResultViewModel RunSimulation(SimulatorInputViewModel input)
        {
            var result = new SimulatorResultViewModel();
            var rng = new Random(input.RandomSeed);
            int s = input.Servers;
            int maxCustomers = Math.Min(input.NumberOfCustomers, 100000); // Safety cap
            double maxTime = input.SimulationDuration;

            // ── Server state: when each server becomes free ─────────────
            double[] serverFreeAt = new double[s];
            double[] serverBusyTime = new double[s]; // Total busy time per server
            for (int i = 0; i < s; i++) serverFreeAt[i] = 0;

            // ── FIFO queue for waiting customers ────────────────────────
            var queue = new Queue<WaitingCustomer>();

            // ── Future Event List (min-heap by time) ────────────────────
            var fel = new PriorityQueue<SimEvent, double>();

            // ── Customer records ────────────────────────────────────────
            var customers = new List<CustomerRecord>();
            var chartQueueTimes = new List<double>();
            var chartQueueLengths = new List<int>();

            int currentQueueLength = 0;
            int maxQueueLength = 0;
            int customersArrived = 0;
            int customersWhoWaited = 0;
            double totalServiceTime = 0;
            double totalInterarrivalTime = 0;

            // ── Time-weighted statistics tracking ───────────────────────
            double lastEventTime = 0;
            double areaUnderQueueLength = 0;  // ∫ queue_length dt
            double areaUnderSystemLength = 0; // ∫ system_length dt
            int currentSystemLength = 0;

            // ── Schedule first arrival ──────────────────────────────────
            double firstInterarrival = SampleArrival(rng, input);
            totalInterarrivalTime += firstInterarrival;
            fel.Enqueue(new SimEvent
            {
                Type = EventType.Arrival,
                Time = firstInterarrival,
                CustomerNumber = 1
            }, firstInterarrival);

            // ═══════════════════════════════════════════════════════════
            // MAIN SIMULATION LOOP — process events until all done
            // ═══════════════════════════════════════════════════════════
            while (fel.Count > 0)
            {
                var evt = fel.Dequeue();
                double now = evt.Time;

                // Stop if we've exceeded the simulation duration
                if (now > maxTime && customersArrived >= maxCustomers)
                    break;

                // ── Update time-weighted statistics ─────────────────────
                double dt = now - lastEventTime;
                areaUnderQueueLength += currentQueueLength * dt;
                areaUnderSystemLength += currentSystemLength * dt;
                lastEventTime = now;

                // Record queue length for chart
                chartQueueTimes.Add(now);
                chartQueueLengths.Add(currentQueueLength);

                // ── ARRIVAL EVENT ───────────────────────────────────────
                if (evt.Type == EventType.Arrival)
                {
                    customersArrived++;
                    double serviceTime = SampleService(rng, input);
                    totalServiceTime += serviceTime;

                    double interarrivalTime = (customersArrived == 1) ? firstInterarrival :
                        (customers.Count > 0 ? now - customers[customers.Count - 1].ArrivalTime : firstInterarrival);

                    // Find a free server (the one with earliest free time that is ≤ now)
                    int freeServer = -1;
                    for (int i = 0; i < s; i++)
                    {
                        if (serverFreeAt[i] <= now)
                        {
                            freeServer = i;
                            break;
                        }
                    }

                    if (freeServer >= 0)
                    {
                        // Server available → start service immediately
                        double idleTime = now - serverFreeAt[freeServer];
                        double serviceStart = now;
                        double serviceEnd = now + serviceTime;
                        serverFreeAt[freeServer] = serviceEnd;
                        serverBusyTime[freeServer] += serviceTime;

                        var record = new CustomerRecord
                        {
                            CustomerNumber = evt.CustomerNumber,
                            InterarrivalTime = interarrivalTime,
                            ArrivalTime = now,
                            ServiceTime = serviceTime,
                            ServiceStartTime = serviceStart,
                            ServiceEndTime = serviceEnd,
                            WaitingTime = 0,
                            TimeInSystem = serviceTime,
                            AssignedServer = freeServer + 1,
                            ServerIdleTime = Math.Max(0, idleTime)
                        };
                        customers.Add(record);

                        currentSystemLength++;

                        // Schedule departure
                        fel.Enqueue(new SimEvent
                        {
                            Type = EventType.Departure,
                            Time = serviceEnd,
                            CustomerNumber = evt.CustomerNumber,
                            ServerIndex = freeServer
                        }, serviceEnd);
                    }
                    else
                    {
                        // All servers busy → enqueue customer
                        queue.Enqueue(new WaitingCustomer
                        {
                            CustomerNumber = evt.CustomerNumber,
                            ArrivalTime = now,
                            ServiceTime = serviceTime,
                            InterarrivalTime = interarrivalTime
                        });
                        currentQueueLength++;
                        currentSystemLength++;
                        if (currentQueueLength > maxQueueLength)
                            maxQueueLength = currentQueueLength;
                    }

                    // Schedule next arrival (if we haven't reached the customer/time limit)
                    if (customersArrived < maxCustomers)
                    {
                        double nextInterarrival = SampleArrival(rng, input);
                        totalInterarrivalTime += nextInterarrival;
                        double nextArrivalTime = now + nextInterarrival;

                        if (nextArrivalTime <= maxTime)
                        {
                            fel.Enqueue(new SimEvent
                            {
                                Type = EventType.Arrival,
                                Time = nextArrivalTime,
                                CustomerNumber = customersArrived + 1
                            }, nextArrivalTime);
                        }
                    }
                }
                // ── DEPARTURE EVENT ─────────────────────────────────────
                else if (evt.Type == EventType.Departure)
                {
                    currentSystemLength--;

                    // If queue is not empty, serve the next waiting customer
                    if (queue.Count > 0)
                    {
                        var next = queue.Dequeue();
                        currentQueueLength--;
                        customersWhoWaited++;

                        double waitTime = now - next.ArrivalTime;
                        double serviceStart = now;
                        double serviceEnd = now + next.ServiceTime;
                        serverFreeAt[evt.ServerIndex] = serviceEnd;
                        serverBusyTime[evt.ServerIndex] += next.ServiceTime;

                        var record = new CustomerRecord
                        {
                            CustomerNumber = next.CustomerNumber,
                            InterarrivalTime = next.InterarrivalTime,
                            ArrivalTime = next.ArrivalTime,
                            ServiceTime = next.ServiceTime,
                            ServiceStartTime = serviceStart,
                            ServiceEndTime = serviceEnd,
                            WaitingTime = waitTime,
                            TimeInSystem = serviceEnd - next.ArrivalTime,
                            AssignedServer = evt.ServerIndex + 1,
                            ServerIdleTime = 0
                        };
                        customers.Add(record);

                        // Schedule departure for this customer
                        fel.Enqueue(new SimEvent
                        {
                            Type = EventType.Departure,
                            Time = serviceEnd,
                            CustomerNumber = next.CustomerNumber,
                            ServerIndex = evt.ServerIndex
                        }, serviceEnd);
                    }
                }
            }

            // ═══════════════════════════════════════════════════════════
            // COMPUTE SUMMARY STATISTICS
            // ═══════════════════════════════════════════════════════════

            // Sort customers by number for display
            customers.Sort((a, b) => a.CustomerNumber.CompareTo(b.CustomerNumber));

            double simEndTime = lastEventTime;
            int totalServed = customers.Count;

            if (totalServed > 0)
            {
                result.TotalCustomersServed = totalServed;
                result.AvgWaitTime = customers.Average(c => c.WaitingTime);
                result.AvgTimeInSystem = customers.Average(c => c.TimeInSystem);
                result.AvgServiceTimeGenerated = customers.Average(c => c.ServiceTime);
                result.AvgInterarrivalTimeGenerated = totalInterarrivalTime / customersArrived;
                result.MaxQueueLength = maxQueueLength;
                result.PercentWhoWaited = totalServed > 0 ?
                    (customers.Count(c => c.WaitingTime > 0.0001) / (double)totalServed) * 100.0 : 0;

                // Time-weighted averages
                result.AvgQueueLength = simEndTime > 0 ? areaUnderQueueLength / simEndTime : 0;
                result.AvgSystemLength = simEndTime > 0 ? areaUnderSystemLength / simEndTime : 0;

                // Per-server utilization
                for (int i = 0; i < s; i++)
                {
                    double util = simEndTime > 0 ? (serverBusyTime[i] / simEndTime) * 100.0 : 0;
                    result.ServerUtilizations.Add(Math.Min(100, util));
                }
                result.OverallUtilization = result.ServerUtilizations.Average();
            }

            result.SimulationEndTime = simEndTime;
            result.Customers = customers;
            result.TimeUnitLabel = GetTimeUnitLabel(input.TimeUnit);

            // Chart data
            result.ChartQueueTimes = chartQueueTimes;
            result.ChartQueueLengths = chartQueueLengths;
            result.ChartWaitTimes = customers.Select(c => c.WaitingTime).ToList();

            // Detect model
            result.DetectedModel = DetectModel(input);

            return result;
        }

        // ═══════════════════════════════════════════════════════════════
        //  SAMPLING HELPERS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Generate a random interarrival time based on the selected distribution.
        /// </summary>
        private double SampleArrival(Random rng, SimulatorInputViewModel input)
        {
            double mean = input.ArrivalInputMode == InputMode.MeanTime
                ? input.ArrivalValue
                : 1.0 / input.ArrivalValue;

            return input.ArrivalDistribution switch
            {
                DistributionType.Exponential => _sampler.SampleExponential(rng, mean),
                DistributionType.Uniform => _sampler.SampleUniform(rng, input.ArrivalMin, input.ArrivalMax),
                DistributionType.Normal => _sampler.SampleNormal(rng, mean,
                    input.UseVarianceForArrival ? Math.Sqrt(input.ArrivalVariance) : input.ArrivalStdDev),
                _ => _sampler.SampleExponential(rng, mean)
            };
        }

        /// <summary>
        /// Generate a random service time based on the selected distribution.
        /// </summary>
        private double SampleService(Random rng, SimulatorInputViewModel input)
        {
            double mean = input.ServiceInputMode == InputMode.MeanTime
                ? input.ServiceValue
                : 1.0 / input.ServiceValue;

            return input.ServiceDistribution switch
            {
                DistributionType.Exponential => _sampler.SampleExponential(rng, mean),
                DistributionType.Uniform => _sampler.SampleUniform(rng, input.ServiceMin, input.ServiceMax),
                DistributionType.Normal => _sampler.SampleNormal(rng, mean,
                    input.UseVarianceForService ? Math.Sqrt(input.ServiceVariance) : input.ServiceStdDev),
                DistributionType.Gamma => _sampler.SampleGamma(rng, input.GammaShape, input.GammaScale),
                _ => _sampler.SampleExponential(rng, mean)
            };
        }

        /// <summary>
        /// Automatically detect the queueing model based on distribution choices and server count.
        /// </summary>
        private string DetectModel(SimulatorInputViewModel input)
        {
            bool arrivalIsM = input.ArrivalDistribution == DistributionType.Exponential;
            bool serviceIsM = input.ServiceDistribution == DistributionType.Exponential;
            bool singleServer = input.Servers == 1;

            if (arrivalIsM && serviceIsM && singleServer) return "M/M/1";
            if (arrivalIsM && serviceIsM && !singleServer) return "M/M/s";
            if (arrivalIsM && !serviceIsM && singleServer) return "M/G/1";
            if (arrivalIsM && !serviceIsM && !singleServer) return "M/G/s";
            if (!arrivalIsM && singleServer) return "G/G/1";
            return "G/G/s";
        }

        private string GetTimeUnitLabel(TimeUnit unit)
        {
            return unit switch
            {
                TimeUnit.Seconds => "sec",
                TimeUnit.Minutes => "min",
                TimeUnit.Hours => "hr",
                _ => "min"
            };
        }
    }
}
