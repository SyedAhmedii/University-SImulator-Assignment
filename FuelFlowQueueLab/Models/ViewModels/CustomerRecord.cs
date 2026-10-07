namespace FuelFlowQueueLab.Models.ViewModels
{
    /// <summary>
    /// One row in the customer-by-customer simulation results table.
    /// </summary>
    public class CustomerRecord
    {
        public int CustomerNumber { get; set; }
        public double InterarrivalTime { get; set; }
        public double ArrivalTime { get; set; }
        public double ServiceTime { get; set; }
        public double ServiceStartTime { get; set; }
        public double ServiceEndTime { get; set; }
        public double WaitingTime { get; set; }      // Time spent waiting in queue
        public double TimeInSystem { get; set; }      // Total time from arrival to departure
        public int AssignedServer { get; set; }       // 1-indexed server number
        public double ServerIdleTime { get; set; }    // How long the server was idle before this customer
    }
}
