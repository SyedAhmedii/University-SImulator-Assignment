namespace FuelFlowQueueLab.Models.Enums
{
    /// <summary>
    /// Supported queueing models.
    /// Kendall's notation: Arrival/Service/Servers
    /// </summary>
    public enum QueueingModel
    {
        MM1,  // M/M/1 – Single pump, Poisson arrivals, Exponential service
        MG1,  // M/G/1 – Poisson arrivals, General service
        MMS,  // M/M/S – Multiple pumps, Poisson arrivals, Exponential service
        MGS,  // M/G/S – Multiple pumps, Poisson arrivals, General service
        GG1,  // G/G/1 – General arrivals, General service, single server
        GGS   // G/G/S – General arrivals, General service, multiple servers
    }
}
