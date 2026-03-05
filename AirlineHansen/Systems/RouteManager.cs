using AirlineHansen.Data;
using AirlineHansen.Engine;
using AirlineHansen.Models;

namespace AirlineHansen.Systems;

/// <summary>
/// Manages routes and flight schedules
/// </summary>
public class RouteManager
{
    private GameState _gameState;
    private FinanceManager _financeManager;

    public RouteManager(GameState gameState, FinanceManager financeManager)
    {
        _gameState = gameState;
        _financeManager = financeManager;
    }

    /// <summary>
    /// Create a new route
    /// </summary>
    public bool CreateRoute(int originCityId, int destinationCityId, int aircraftId, decimal ticketPrice, int flightsPerDay = 1)
    {
        // Validate cities exist
        var originCity = CityDatabase.GetCityById(originCityId);
        var destCity = CityDatabase.GetCityById(destinationCityId);

        if (originCity == null || destCity == null)
            return false;

        // Validate aircraft exists and is available
        var aircraft = _gameState.Fleet.FirstOrDefault(a => a.Id == aircraftId);
        if (aircraft == null || !aircraft.IsAvailable)
            return false;

        // Check aircraft has sufficient range
        double distance = CityDatabase.CalculateDistance(originCity, destCity);
        if (distance > aircraft.MaxRange)
            return false;

        // Route already exists?
        if (_gameState.Routes.Any(r =>
            r.OriginCityId == originCityId &&
            r.DestinationCityId == destinationCityId &&
            r.IsActive))
            return false;

        var route = new Route(
            _gameState.NextRouteId++,
            originCityId,
            destinationCityId,
            aircraftId,
            ticketPrice
        )
        {
            FlightsPerDay = flightsPerDay
        };

        _gameState.Routes.Add(route);
        aircraft.IsAvailable = false; // Aircraft is now dedicated to this route

        return true;
    }

    /// <summary>
    /// Delete a route
    /// </summary>
    public bool DeleteRoute(int routeId)
    {
        var route = _gameState.Routes.FirstOrDefault(r => r.Id == routeId);
        if (route == null)
            return false;

        route.IsActive = false;

        // Make aircraft available again
        var aircraft = _gameState.Fleet.FirstOrDefault(a => a.Id == route.AircraftId);
        if (aircraft != null)
            aircraft.IsAvailable = true;

        return true;
    }

    /// <summary>
    /// Update ticket price for a route
    /// </summary>
    public bool UpdateTicketPrice(int routeId, decimal newPrice)
    {
        var route = _gameState.Routes.FirstOrDefault(r => r.Id == routeId);
        if (route == null)
            return false;

        route.TicketPrice = newPrice;
        return true;
    }

    /// <summary>
    /// Update flights per day for a route
    /// </summary>
    public bool UpdateFlightsPerDay(int routeId, int newFlightsPerDay)
    {
        var route = _gameState.Routes.FirstOrDefault(r => r.Id == routeId);
        if (route == null)
            return false;

        route.FlightsPerDay = Math.Max(1, newFlightsPerDay);
        return true;
    }

    /// <summary>
    /// Get all routes
    /// </summary>
    public List<Route> GetAllRoutes()
    {
        return _gameState.Routes.Where(r => r.IsActive).ToList();
    }

    /// <summary>
    /// Get routes using a specific aircraft
    /// </summary>
    public List<Route> GetRoutesForAircraft(int aircraftId)
    {
        return _gameState.Routes.Where(r => r.IsActive && r.AircraftId == aircraftId).ToList();
    }

    /// <summary>
    /// Get routes between two cities
    /// </summary>
    public List<Route> GetRoutesBetween(int originCityId, int destinationCityId)
    {
        return _gameState.Routes.Where(r =>
            r.IsActive &&
            r.OriginCityId == originCityId &&
            r.DestinationCityId == destinationCityId).ToList();
    }

    /// <summary>
    /// Get flight statistics for a route
    /// </summary>
    public (int totalFlights, int completedFlights, decimal totalRevenue, double averageLoadFactor) GetRouteStats(int routeId)
    {
        var flights = _gameState.AllFlights.Where(f => f.RouteId == routeId).ToList();

        int totalFlights = flights.Count;
        int completedFlights = flights.Count(f => f.Status == "Completed");
        decimal totalRevenue = flights.Sum(f => f.Revenue);
        double averageLoadFactor = flights.Any() ? flights.Average(f => f.LoadFactor) : 0;

        return (totalFlights, completedFlights, totalRevenue, averageLoadFactor);
    }

    /// <summary>
    /// Get all scheduled and in-progress flights
    /// </summary>
    public List<Flight> GetActiveFlights()
    {
        return _gameState.AllFlights.Where(f => f.Status == "Scheduled" || f.Status == "InProgress").ToList();
    }

    /// <summary>
    /// Get flight history (completed flights)
    /// </summary>
    public List<Flight> GetFlightHistory(int limit = 50)
    {
        return _gameState.AllFlights.Where(f => f.Status == "Completed").OrderByDescending(f => f.Id).Take(limit).ToList();
    }
}
