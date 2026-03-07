namespace AirlineHansen.Models;

/// <summary>
/// Represents an aircraft in the player's fleet
/// </summary>
public class Aircraft
{
    public int Id { get; set; }
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Number of passenger seats
    /// </summary>
    public int Capacity { get; set; }

    /// <summary>
    /// Maximum flight range in kilometers
    /// </summary>
    public int MaxRange { get; set; }

    /// <summary>
    /// Fuel cost per kilometer
    /// </summary>
    public decimal FuelCostPerKm { get; set; }

    /// <summary>
    /// Cruise speed in km/h
    /// </summary>
    public int CruiseSpeed { get; set; }

    /// <summary>
    /// Purchase price in euros
    /// </summary>
    public decimal PurchasePrice { get; set; }

    /// <summary>
    /// Current location (city ID), null if in flight
    /// </summary>
    public int? CurrentLocationId { get; set; }

    /// <summary>
    /// Is the aircraft available for scheduling
    /// </summary>
    public bool IsAvailable { get; set; } = true;

    /// <summary>
    /// Assigned pilots and crew
    /// </summary>
    public List<int> AssignedCrewIds { get; set; } = new();

    public Aircraft()
    {
    }

    public Aircraft(int id, string model, int capacity, int maxRange, decimal fuelCostPerKm, decimal purchasePrice, int cruiseSpeed = 800)
    {
        Id = id;
        Model = model;
        Capacity = capacity;
        MaxRange = maxRange;
        FuelCostPerKm = fuelCostPerKm;
        PurchasePrice = purchasePrice;
        CruiseSpeed = cruiseSpeed;
    }

    public override string ToString() => $"{Model} ({Capacity} seats, {MaxRange}km range)";
}
