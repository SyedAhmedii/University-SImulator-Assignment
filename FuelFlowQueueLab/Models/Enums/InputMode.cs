namespace FuelFlowQueueLab.Models.Enums
{
    /// <summary>
    /// Whether the user enters a mean inter-event time or a rate directly.
    /// </summary>
    public enum InputMode
    {
        MeanTime,  // User enters mean interarrival/service time; rate = 1/value
        Rate       // User enters rate (λ or μ) directly
    }
}
