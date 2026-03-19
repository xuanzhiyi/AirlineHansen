using AirlineHansen.Data;
using AirlineHansen.Models;

namespace AirlineHansen.Engine;

/// <summary>
/// Main game simulation engine - updates game state each tick
/// </summary>
public class SimulationEngine
{
    private GameState _gameState;
    private TimeManager _timeManager;

    // Simulation parameters
    private const double BaseDemandFactor = 0.001; // Passengers per population unit
    private const decimal PriceElasticity = 0.0002m; // Demand reduction per euro of price
    private const int GroundTimeMinutes = 240; // 4 hours ground time at airport

    public SimulationEngine(GameState gameState, TimeManager timeManager)
    {
        _gameState = gameState;
        _timeManager = timeManager;
    }

    /// <summary>
    /// Update game for one tick (one game hour)
    /// </summary>
    public void UpdateOneTick()
    {
        if (!_gameState.IsGameRunning)
            return;

        // Advance game time
        _gameState.AdvanceDayOneTick();

        // Transition flights and handle arrivals (before creating new flights)
        UpdateFlightStatuses();

        // Schedule next flight for any idle aircraft
        ScheduleNextFlights();

        // Calculate demand and book passengers for scheduled flights
        CalculatePassengerDemand();

        // Update finances
        UpdateFinances();

        // Apply loan interest
        ApplyLoanInterest();

        // Check game over condition
        _gameState.CheckGameOver();
    }

    /// <summary>
    /// Schedule the next flight for each aircraft that has no pending flights.
    /// Each aircraft independently alternates between its forward and return routes.
    /// </summary>
    private void ScheduleNextFlights()
    {
        // Group active routes by aircraft
        var routesByAircraft = _gameState.GetActiveRoutes()
            .GroupBy(r => r.AircraftId);

        foreach (var aircraftRoutes in routesByAircraft)
        {
            var aircraft = _gameState.Fleet.FirstOrDefault(a => a.Id == aircraftRoutes.Key);
            if (aircraft == null)
                continue;

            var routes = aircraftRoutes.ToList();
            var routeIds = routes.Select(r => r.Id).ToHashSet();

            // Check if aircraft already has a pending flight (Scheduled or InProgress)
            bool hasPendingFlight = _gameState.AllFlights
                .Any(f => routeIds.Contains(f.RouteId) && (f.Status == "Scheduled" || f.Status == "InProgress"));

            if (hasPendingFlight)
                continue; // Aircraft is busy, don't schedule another flight

            // Find the last completed flight to determine which route is next
            var lastCompletedFlight = _gameState.AllFlights
                .Where(f => routeIds.Contains(f.RouteId) && f.Status == "Completed")
                .OrderByDescending(f => f.ActualArrival ?? f.ScheduledDeparture)
                .FirstOrDefault();

            // Alternate between forward and return routes
            Route nextRoute;
            if (lastCompletedFlight == null)
            {
                // No previous flights - start with forward route
                nextRoute = routes.FirstOrDefault(r => !r.IsReturnRoute) ?? routes.First();
            }
            else
            {
                // If last was forward, next is return; if last was return, next is forward
                var lastRoute = routes.FirstOrDefault(r => r.Id == lastCompletedFlight.RouteId);
                if (lastRoute?.IsReturnRoute == true)
                    nextRoute = routes.FirstOrDefault(r => !r.IsReturnRoute) ?? routes.First();
                else
                    nextRoute = routes.FirstOrDefault(r => r.IsReturnRoute) ?? routes.First();
            }

            // Schedule the next flight at current game time
            var flight = new Flight(
                _gameState.NextFlightId++,
                nextRoute.Id,
                aircraft.Capacity,
                _gameState.GameTime
            );

            flight.Status = "Scheduled";
            _gameState.AllFlights.Add(flight);
            nextRoute.Flights.Add(flight);
        }
    }

    /// <summary>
    /// Update flight statuses and handle departures/arrivals
    /// </summary>
    private void UpdateFlightStatuses()
    {
        foreach (var flight in _gameState.AllFlights)
        {
            if (flight.Status == "Scheduled" && flight.ScheduledDeparture <= _gameState.GameTime)
            {
                // Move to in progress (flight departs when ScheduledDeparture time reached)
                flight.Status = "InProgress";
                // Set actual departure time to current game time
                flight.ActualDeparture = _gameState.GameTime;

                // Calculate realistic flight duration based on distance and aircraft speed
                var route = _gameState.Routes.FirstOrDefault(r => r.Id == flight.RouteId);
                if (route != null)
                {
                    var aircraft = _gameState.Fleet.FirstOrDefault(a => a.Id == route.AircraftId);
                    var originCity = CityDatabase.GetCityById(route.OriginCityId);
                    var destCity = CityDatabase.GetCityById(route.DestinationCityId);

                    if (aircraft != null && originCity != null && destCity != null)
                    {
                        double distance = CityDatabase.CalculateDistance(originCity, destCity);

                        // Flight time in minutes: (distance km / cruise speed km/h) * 60 minutes
                        // Add 30 minutes for taxi, takeoff, and landing
                        // Inflated by 100% to make flight duration differences more noticeable
                        double flightTimeMinutes = ((distance / aircraft.CruiseSpeed) * 60 + 30) * 2;
                        flight.EstimatedArrival = _gameState.GameTime.AddMinutes(flightTimeMinutes);
                    }
                }

                // If EstimatedArrival wasn't set (missing route/aircraft/cities), use default
                if (flight.EstimatedArrival == null)
                {
                    flight.EstimatedArrival = _gameState.GameTime.AddMinutes(720); // 12 hour fallback
                }
            }
            else if (flight.Status == "InProgress" && flight.EstimatedArrival <= _gameState.GameTime && flight.ActualArrival == null)
            {
                // Flight arrives - set arrival time and when it's ready to depart again
                flight.ActualArrival = _gameState.GameTime;

                // Aircraft needs ground time for refueling, boarding, offboarding (default 4 hours = 240 minutes)
                flight.ReadyToDepartTime = _gameState.GameTime.AddMinutes(flight.GroundTimeMinutes);

                // Calculate costs
                var route = _gameState.Routes.FirstOrDefault(r => r.Id == flight.RouteId);
                if (route != null)
                {
                    var aircraft = _gameState.Fleet.FirstOrDefault(a => a.Id == route.AircraftId);
                    var originCity = CityDatabase.GetCityById(route.OriginCityId);
                    var destCity = CityDatabase.GetCityById(route.DestinationCityId);

                    if (aircraft != null && originCity != null && destCity != null)
                    {
                        double distance = CityDatabase.CalculateDistance(originCity, destCity);
                        flight.Cost = aircraft.FuelCostPerKm * (decimal)distance + originCity.AirportFee + destCity.AirportFee;
                        flight.Revenue = flight.PassengersBooked * route.TicketPrice;

                        // Update balance
                        _gameState.AddTransaction(flight.Profit, $"Flight {flight.Id} profit");
                    }
                }
            }
            else if (flight.Status == "InProgress" && flight.ReadyToDepartTime != null && flight.ReadyToDepartTime <= _gameState.GameTime)
            {
                // Aircraft has finished ground time, mark flight as completed
                flight.Status = "Completed";
            }
        }

        // Clean up completed flights (keep last 100 for history)
        var completedFlights = _gameState.AllFlights.Where(f => f.Status == "Completed").OrderByDescending(f => f.Id).Skip(100).ToList();
        foreach (var flight in completedFlights)
        {
            _gameState.AllFlights.Remove(flight);
        }
    }

    /// <summary>
    /// Calculate passenger demand and book seats
    /// </summary>
    private void CalculatePassengerDemand()
    {
        var scheduledFlights = _gameState.AllFlights.Where(f => f.Status == "Scheduled").ToList();

        foreach (var flight in scheduledFlights)
        {
            var route = _gameState.Routes.FirstOrDefault(r => r.Id == flight.RouteId);
            if (route == null)
                continue;

            var originCity = CityDatabase.GetCityById(route.OriginCityId);
            var destCity = CityDatabase.GetCityById(route.DestinationCityId);

            if (originCity == null || destCity == null)
                continue;

            // Calculate base demand from populations
            double baseDemand = (originCity.Population + destCity.Population) * BaseDemandFactor;

            // Apply price elasticity (higher price = less demand)
            double priceAdjustment = (double)(route.TicketPrice * PriceElasticity);
            double adjustedDemand = baseDemand - priceAdjustment;
            adjustedDemand = Math.Max(0, adjustedDemand);

            // Count competing flights on same route (more competition = less demand)
            int competingFlights = _gameState.AllFlights.Count(f =>
                f.Status == "Scheduled" && f.Id != flight.Id &&
                _gameState.Routes.Any(r => r.Id == f.RouteId &&
                    r.OriginCityId == route.OriginCityId &&
                    r.DestinationCityId == route.DestinationCityId)
            );

            adjustedDemand = adjustedDemand / (1 + competingFlights * 0.3);

            // Book passengers
            int passengersToBook = Math.Min((int)adjustedDemand, flight.Capacity);
            flight.PassengersBooked = Math.Max(0, passengersToBook);
        }
    }

    /// <summary>
    /// Update finances - pay salaries, etc
    /// </summary>
    private void UpdateFinances()
    {
        // Pay crew salaries (monthly)
        var activeCrew = _gameState.GetActiveCrew();
        if (_gameState.GameDay % 30 == 0) // Pay every 30 days
        {
            foreach (var crew in activeCrew)
            {
                _gameState.AddTransaction(-crew.MonthlySalary, $"Salary for {crew.Name}");
            }
        }

        // Daily operational costs
        _gameState.DailyProfit = 0;
    }

    /// <summary>
    /// Apply daily interest to all loans
    /// </summary>
    private void ApplyLoanInterest()
    {
        var activeLoans = _gameState.GetActiveLoans();

        foreach (var loan in activeLoans)
        {
            decimal dailyInterest = loan.CalculateDailyInterest();
            loan.TotalInterestPaid += dailyInterest;
            loan.RemainingBalance += dailyInterest;
            _gameState.AddTransaction(-dailyInterest, $"Loan interest on loan {loan.Id}");
        }
    }

    /// <summary>
    /// Get flight info for UI
    /// </summary>
    public List<Flight> GetActiveFlights()
    {
        return _gameState.AllFlights.Where(f => f.Status == "Scheduled" || f.Status == "InProgress").ToList();
    }
}
