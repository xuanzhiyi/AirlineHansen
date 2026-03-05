using AirlineHansen.Models;

namespace AirlineHansen.Data;

/// <summary>
/// Database of European cities with real-world data
/// Coordinates are latitude/longitude, populations are approximate
/// </summary>
public static class CityDatabase
{
    private static readonly List<City> Cities = new();

    public static void Initialize()
    {
        Cities.Clear();

        // Major European Hub Cities
        Cities.Add(new City(1, "London", "United Kingdom", 51.5074, -0.1278, 9002488, 5000m));
        Cities.Add(new City(2, "Paris", "France", 48.8566, 2.3522, 2165423, 5000m));
        Cities.Add(new City(3, "Frankfurt", "Germany", 50.1109, 8.6821, 746878, 4500m));
        Cities.Add(new City(4, "Amsterdam", "Netherlands", 52.3676, 4.9041, 873555, 4000m));
        Cities.Add(new City(5, "Madrid", "Spain", 40.4168, -3.7038, 3223606, 4000m));
        Cities.Add(new City(6, "Barcelona", "Spain", 41.3851, 2.1734, 1620343, 3500m));
        Cities.Add(new City(7, "Rome", "Italy", 41.9028, 12.4964, 2873494, 3500m));
        Cities.Add(new City(8, "Milan", "Italy", 45.4642, 9.1900, 1352638, 3500m));
        Cities.Add(new City(9, "Berlin", "Germany", 52.5200, 13.4050, 3769886, 3000m));
        Cities.Add(new City(10, "Munich", "Germany", 48.1351, 11.5820, 1484410, 3500m));

        // Other major cities
        Cities.Add(new City(11, "Vienna", "Austria", 48.2082, 16.3738, 1920000, 3000m));
        Cities.Add(new City(12, "Brussels", "Belgium", 50.8503, 4.3517, 1211000, 3500m));
        Cities.Add(new City(13, "Zurich", "Switzerland", 47.3769, 8.5472, 428373, 3000m));
        Cities.Add(new City(14, "Geneva", "Switzerland", 46.1955, 6.1329, 200891, 3000m));
        Cities.Add(new City(15, "Athens", "Greece", 37.9838, 23.7275, 3154001, 2500m));
        Cities.Add(new City(16, "Istanbul", "Turkey", 41.0082, 28.9784, 15462000, 2500m));
        Cities.Add(new City(17, "Prague", "Czech Republic", 50.0755, 14.4378, 1360000, 2500m));
        Cities.Add(new City(18, "Budapest", "Hungary", 47.4979, 19.0402, 1752286, 2500m));
        Cities.Add(new City(19, "Warsaw", "Poland", 52.2297, 21.0122, 1863000, 2500m));
        Cities.Add(new City(20, "Krakow", "Poland", 50.0647, 19.9450, 779115, 2000m));

        // Northern European cities
        Cities.Add(new City(21, "Copenhagen", "Denmark", 55.6761, 12.5883, 1345216, 2500m));
        Cities.Add(new City(22, "Stockholm", "Sweden", 59.3293, 18.0686, 975551, 2500m));
        Cities.Add(new City(23, "Oslo", "Norway", 59.9139, 10.7522, 697010, 2500m));
        Cities.Add(new City(24, "Helsinki", "Finland", 60.1695, 24.9354, 656438, 2500m));

        // Southern European cities
        Cities.Add(new City(25, "Lisbon", "Portugal", 38.7223, -9.1393, 1505008, 2000m));
        Cities.Add(new City(26, "Valencia", "Spain", 39.4699, -0.3763, 1604367, 2000m));
        Cities.Add(new City(27, "Seville", "Spain", 37.3886, -5.9823, 1528384, 2000m));
        Cities.Add(new City(28, "Naples", "Italy", 40.8518, 14.2681, 914798, 2500m));
        Cities.Add(new City(29, "Venice", "Italy", 45.4408, 12.3155, 265884, 2000m));
        Cities.Add(new City(30, "Palermo", "Italy", 38.1156, 13.3615, 657560, 2000m));

        // Eastern European cities
        Cities.Add(new City(31, "Bucharest", "Romania", 44.4268, 26.1025, 1830000, 2000m));
        Cities.Add(new City(32, "Sofia", "Bulgaria", 42.6977, 23.3219, 1242568, 2000m));
        Cities.Add(new City(33, "Belgrade", "Serbia", 44.8176, 20.4633, 1659440, 2000m));
        Cities.Add(new City(34, "Ljubljana", "Slovenia", 46.0569, 14.5058, 295000, 1500m));
        Cities.Add(new City(35, "Bratislava", "Slovakia", 48.1486, 17.1077, 475948, 2000m));

        // Additional key cities
        Cities.Add(new City(36, "Cologne", "Germany", 50.9365, 6.9589, 1087863, 3000m));
        Cities.Add(new City(37, "Hamburg", "Germany", 53.5511, 9.9937, 1852901, 3000m));
        Cities.Add(new City(38, "Lyon", "France", 45.7640, 4.8357, 513275, 2500m));
        Cities.Add(new City(39, "Marseille", "France", 43.2965, 5.3698, 869815, 2500m));
        Cities.Add(new City(40, "Toulouse", "France", 43.6047, 1.4422, 479553, 2000m));
    }

    public static List<City> GetAllCities()
    {
        if (Cities.Count == 0) Initialize();
        return new List<City>(Cities);
    }

    public static City? GetCityById(int id)
    {
        if (Cities.Count == 0) Initialize();
        return Cities.FirstOrDefault(c => c.Id == id);
    }

    public static City? GetCityByName(string name)
    {
        if (Cities.Count == 0) Initialize();
        return Cities.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Calculate distance between two cities using haversine formula
    /// </summary>
    public static double CalculateDistance(City city1, City city2)
    {
        const double earthRadiusKm = 6371.0;

        double lat1Rad = DegToRad(city1.Latitude);
        double lat2Rad = DegToRad(city2.Latitude);
        double deltaLatRad = DegToRad(city2.Latitude - city1.Latitude);
        double deltaLonRad = DegToRad(city2.Longitude - city1.Longitude);

        double a = Math.Sin(deltaLatRad / 2) * Math.Sin(deltaLatRad / 2) +
                   Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                   Math.Sin(deltaLonRad / 2) * Math.Sin(deltaLonRad / 2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadiusKm * c;
    }

    private static double DegToRad(double degrees)
    {
        return degrees * (Math.PI / 180.0);
    }
}
