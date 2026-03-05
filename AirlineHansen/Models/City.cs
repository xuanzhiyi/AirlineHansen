namespace AirlineHansen.Models;

/// <summary>
/// Represents a European city in the game world
/// </summary>
public class City
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;

    /// <summary>
    /// Latitude coordinate
    /// </summary>
    public double Latitude { get; set; }

    /// <summary>
    /// Longitude coordinate
    /// </summary>
    public double Longitude { get; set; }

    /// <summary>
    /// City population (affects demand)
    /// </summary>
    public int Population { get; set; }

    /// <summary>
    /// Airport fee per flight (in euros)
    /// </summary>
    public decimal AirportFee { get; set; }

    public City()
    {
    }

    public City(int id, string name, string country, double latitude, double longitude, int population, decimal airportFee)
    {
        Id = id;
        Name = name;
        Country = country;
        Latitude = latitude;
        Longitude = longitude;
        Population = population;
        AirportFee = airportFee;
    }

    public override string ToString() => $"{Name}, {Country}";
}
