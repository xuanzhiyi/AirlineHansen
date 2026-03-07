namespace AirlineHansen.Models;

/// <summary>
/// Represents a regularly scheduled route between two cities
/// </summary>
public class Route
{
    public int Id { get; set; }

    /// <summary>
    /// Origin city ID
    /// </summary>
    public int OriginCityId { get; set; }

    /// <summary>
    /// Destination city ID
    /// </summary>
    public int DestinationCityId { get; set; }

    /// <summary>
    /// Aircraft ID assigned to this route
    /// </summary>
    public int AircraftId { get; set; }

    /// <summary>
    /// Ticket price in euros
    /// </summary>
    public decimal TicketPrice { get; set; }

    /// <summary>
    /// Is the route active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Flights per game-day (1, 2, etc)
    /// </summary>
    public int FlightsPerDay { get; set; } = 1;

    /// <summary>
    /// Whether this is a return/reverse route (for staggering shuttle flights)
    /// </summary>
    public bool IsReturnRoute { get; set; } = false;

    /// <summary>
    /// Expected flight time in minutes (for scheduling return flights)
    /// </summary>
    public double ExpectedFlightTimeMinutes { get; set; } = 0;

    /// <summary>
    /// Date route was created
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// List of scheduled flights on this route
    /// </summary>
    public List<Flight> Flights { get; set; } = new();

    public Route()
    {
        CreatedDate = DateTime.Now;
    }

    public Route(int id, int originCityId, int destinationCityId, int aircraftId, decimal ticketPrice)
    {
        Id = id;
        OriginCityId = originCityId;
        DestinationCityId = destinationCityId;
        AircraftId = aircraftId;
        TicketPrice = ticketPrice;
        CreatedDate = DateTime.Now;
    }

    public override string ToString() => $"Route {Id}: City {OriginCityId} → City {DestinationCityId}";
}
