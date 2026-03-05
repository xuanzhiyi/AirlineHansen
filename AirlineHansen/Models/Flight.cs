namespace AirlineHansen.Models;

/// <summary>
/// Represents an individual scheduled flight
/// </summary>
public class Flight
{
    public int Id { get; set; }

    /// <summary>
    /// Route ID this flight belongs to
    /// </summary>
    public int RouteId { get; set; }

    /// <summary>
    /// Flight status: Scheduled, InProgress, Completed, Cancelled
    /// </summary>
    public string Status { get; set; } = "Scheduled";

    /// <summary>
    /// Number of passengers booked
    /// </summary>
    public int PassengersBooked { get; set; }

    /// <summary>
    /// Aircraft capacity
    /// </summary>
    public int Capacity { get; set; }

    /// <summary>
    /// Scheduled departure time (game time)
    /// </summary>
    public DateTime ScheduledDeparture { get; set; }

    /// <summary>
    /// Actual departure time (null if not departed yet)
    /// </summary>
    public DateTime? ActualDeparture { get; set; }

    /// <summary>
    /// Estimated arrival time
    /// </summary>
    public DateTime? EstimatedArrival { get; set; }

    /// <summary>
    /// Revenue from ticket sales
    /// </summary>
    public decimal Revenue { get; set; }

    /// <summary>
    /// Flight cost (fuel, etc)
    /// </summary>
    public decimal Cost { get; set; }

    /// <summary>
    /// Profit from this flight
    /// </summary>
    public decimal Profit => Revenue - Cost;

    /// <summary>
    /// Load factor (percentage of seats filled)
    /// </summary>
    public double LoadFactor => Capacity > 0 ? (double)PassengersBooked / Capacity : 0;

    public Flight()
    {
    }

    public Flight(int id, int routeId, int capacity, DateTime scheduledDeparture)
    {
        Id = id;
        RouteId = routeId;
        Capacity = capacity;
        ScheduledDeparture = scheduledDeparture;
    }

    public override string ToString() => $"Flight {Id}: Route {RouteId}, {PassengersBooked}/{Capacity} passengers";
}
