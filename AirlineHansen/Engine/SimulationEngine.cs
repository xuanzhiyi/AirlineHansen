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
    private int _lastFlightCreationDay = -1; // Track which day flights were created to avoid duplicates

    // Simulation parameters
    private const double BaseDemandFactor = 0.001; // Passengers per population unit
    private const decimal PriceElasticity = 0.0002m; // Demand reduction per euro of price

    public SimulationEngine(GameState gameState, TimeManager timeManager)
    {
        _gameState = gameState;
        _timeManager = timeManager;
    }

    /// <summary>
    /// Update game for one tick (one game day)
    /// </summary>
    public void UpdateOneTick()
    {
        if (!_gameState.IsGameRunning)
            return;

        // Advance game time
        _gameState.AdvanceDayOneTick();

        // Create new flights for today (only once per day)
        int currentDay = _gameState.GameTime.Day;
        if (currentDay != _lastFlightCreationDay)
        {
            CreateFlights();
            _lastFlightCreationDay = currentDay;
        }

        // Calculate demand and book passengers for scheduled flights
        CalculatePassengerDemand();

        // Transition flights and handle arrivals
        UpdateFlightStatuses();

        // Update finances
        UpdateFinances();

        // Apply loan interest
        ApplyLoanInterest();

        // Check game over condition
        _gameState.CheckGameOver();
    }

    /// <summary>
    /// Create new flights for today (multiple flights per day based on route FlightsPerDay)
    /// Flights are scheduled based on aircraft availability to avoid conflicts
    /// </summary>
    private void CreateFlights()
    {
        var routes = _gameState.GetActiveRoutes();

        foreach (var route in routes)
        {
            var aircraft = _gameState.Fleet.FirstOrDefault(a => a.Id == route.AircraftId);
            if (aircraft == null)
                continue;

            // Find when this aircraft will be ready (after all pending flights on ANY route complete)
            DateTime aircraftAvailableAt = _gameState.GameTime;

            // Check ALL flights for this aircraft (across all routes), not just this route
            var allRoutesForAircraft = _gameState.Routes.Where(r => r.AircraftId == route.AircraftId);
            var pendingFlights = _gameState.AllFlights
                .Where(f => allRoutesForAircraft.Any(r => r.Id == f.RouteId) && (f.Status == "Scheduled" || f.Status == "InProgress"))
                .OrderByDescending(f => f.ReadyToDepartTime ?? f.EstimatedArrival ?? f.ScheduledDeparture)
                .FirstOrDefault();

            if (pendingFlights?.ReadyToDepartTime.HasValue == true)
            {
                aircraftAvailableAt = pendingFlights.ReadyToDepartTime.Value;
            }
            else if (pendingFlights?.EstimatedArrival.HasValue == true)
            {
                // If no ReadyToDepartTime yet, use EstimatedArrival + ground time
                aircraftAvailableAt = pendingFlights.EstimatedArrival.Value.AddMinutes(240);
            }
            else if (pendingFlights?.Status == "Scheduled")
            {
                // For Scheduled flights, calculate when aircraft will be ready using expected flight time
                var flightRoute = _gameState.Routes.FirstOrDefault(r => r.Id == pendingFlights.RouteId);
                if (flightRoute != null)
                {
                    double flightDurationPlusGround = flightRoute.ExpectedFlightTimeMinutes + 240;
                    aircraftAvailableAt = pendingFlights.ScheduledDeparture.AddMinutes(flightDurationPlusGround);
                }
            }

            // Create FlightsPerDay flights per route, scheduled at aircraft's available times
            int flightsToCreate = Math.Max(1, route.FlightsPerDay);

            for (int i = 0; i < flightsToCreate; i++)
            {
                // Calculate time spacing between flights (24 hours / FlightsPerDay)
                double minutesBetweenFlights = 24.0 * 60.0 / flightsToCreate;
                DateTime scheduledDeparture = aircraftAvailableAt.AddMinutes(i * minutesBetweenFlights);

                // Only create flight if it hasn't already been created
                if (_gameState.AllFlights.Any(f => f.RouteId == route.Id &&
                    f.ScheduledDeparture == scheduledDeparture &&
                    f.Status != "Completed"))
                    continue;

                var flight = new Flight(
                    _gameState.NextFlightId++,
                    route.Id,
                    aircraft.Capacity,
                    scheduledDeparture
                );

                flight.Status = "Scheduled";
                // ActualDeparture is set when flight transitions to InProgress in UpdateFlightStatuses()
                _gameState.AllFlights.Add(flight);
                route.Flights.Add(flight);
            }
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
