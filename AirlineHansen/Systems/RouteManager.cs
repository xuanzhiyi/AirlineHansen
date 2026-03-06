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
    /// Calculate optimal flights per day based on distance
    /// </summary>
    public int CalculateFlightsPerDay(double distanceKm)
    {
        // Flight time = distance / 500 km/h
        // Turn-around time = 2 hours (boarding, refueling, cleaning)
        // Total cycle time per flight

        double flightTimeHours = distanceKm / 500.0;
        double turnAroundTimeHours = 2.0;
        double cycleTimes = flightTimeHours + turnAroundTimeHours;

        // Available hours per day
        const double hoursPerDay = 24.0;
        int maxFlights = (int)(hoursPerDay / cycleTimes);

        // Cap maximum flights
        return Math.Max(1, Math.Min(maxFlights, 5));
    }

    /// <summary>
    /// Create a new route with automatic flights per day calculation
    /// </summary>
    public bool CreateRoute(int originCityId, int destinationCityId, int aircraftId, decimal ticketPrice, int flightsPerDay = 0)
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

        // Auto-calculate flights per day if not specified
        int calculatedFlightsPerDay = flightsPerDay > 0 ? flightsPerDay : CalculateFlightsPerDay(distance);

        var route = new Route(
            _gameState.NextRouteId++,
            originCityId,
            destinationCityId,
            aircraftId,
            ticketPrice
        )
        {
            FlightsPerDay = 1  // Use 1 flight per day for continuous shuttle (plane goes A→B→A)
        };

        _gameState.Routes.Add(route);
        aircraft.IsAvailable = false; // Aircraft is now dedicated to this route

        // Automatically create return route for continuous shuttle service
        // Check if return route already exists to avoid duplicates
        if (!_gameState.Routes.Any(r =>
            r.OriginCityId == destinationCityId &&
            r.DestinationCityId == originCityId &&
            r.IsActive))
        {
            var returnRoute = new Route(
                _gameState.NextRouteId++,
                destinationCityId,
                originCityId,
                aircraftId,  // Same aircraft handles return flights
                ticketPrice
            )
            {
                FlightsPerDay = 1,  // Return route uses 1 flight per day (same plane continues shuttle)
                IsReturnRoute = true  // Mark as return route for staggered scheduling
            };

            _gameState.Routes.Add(returnRoute);
            // Aircraft already marked as unavailable from forward route
        }

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
