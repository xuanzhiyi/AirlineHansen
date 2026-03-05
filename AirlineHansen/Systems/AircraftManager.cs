using AirlineHansen.Engine;
using AirlineHansen.Models;

namespace AirlineHansen.Systems;

/// <summary>
/// Manages aircraft fleet operations
/// </summary>
public class AircraftManager
{
    private GameState _gameState;
    private FinanceManager _financeManager;

    public AircraftManager(GameState gameState, FinanceManager financeManager)
    {
        _gameState = gameState;
        _financeManager = financeManager;
    }

    /// <summary>
    /// Purchase a new aircraft
    /// </summary>
    public bool PurchaseAircraft(string model, int capacity, int maxRange, decimal fuelCostPerKm, decimal purchasePrice)
    {
        if (!_financeManager.CanAfford(purchasePrice))
            return false;

        var aircraft = new Aircraft(
            _gameState.NextAircraftId++,
            model,
            capacity,
            maxRange,
            fuelCostPerKm,
            purchasePrice
        );

        _gameState.Fleet.Add(aircraft);
        _financeManager.RecordTransaction(_gameState.GameTime, $"Purchased aircraft: {model}", -purchasePrice);

        return true;
    }

    /// <summary>
    /// Purchase predefined aircraft type
    /// </summary>
    public bool PurchaseAircraftType(string type)
    {
        return type switch
        {
            "Small" => PurchaseAircraft("Airbus A220", 50, 1500, 0.05m, 5_000_000m),
            "Medium" => PurchaseAircraft("Airbus A320", 150, 3000, 0.08m, 15_000_000m),
            "Large" => PurchaseAircraft("Airbus A350", 300, 5000, 0.12m, 30_000_000m),
            _ => false
        };
    }

    /// <summary>
    /// Sell an aircraft
    /// </summary>
    public bool SellAircraft(int aircraftId)
    {
        var aircraft = _gameState.Fleet.FirstOrDefault(a => a.Id == aircraftId);
        if (aircraft == null)
            return false;

        // Can't sell if aircraft is in active routes
        if (_gameState.Routes.Any(r => r.AircraftId == aircraftId && r.IsActive))
            return false;

        decimal salePrice = aircraft.PurchasePrice * 0.6m; // Sell at 60% of purchase price
        _gameState.Fleet.Remove(aircraft);
        _financeManager.RecordTransaction(_gameState.GameTime, $"Sold aircraft: {aircraft.Model}", salePrice);

        return true;
    }

    /// <summary>
    /// Get fleet info
    /// </summary>
    public List<Aircraft> GetFleet()
    {
        return _gameState.Fleet;
    }

    /// <summary>
    /// Get available aircraft for assignment
    /// </summary>
    public List<Aircraft> GetAvailableAircraft()
    {
        return _gameState.Fleet.Where(a => a.IsAvailable).ToList();
    }

    /// <summary>
    /// Assign crew to aircraft
    /// </summary>
    public bool AssignCrewToAircraft(int aircraftId, int crewId)
    {
        var aircraft = _gameState.Fleet.FirstOrDefault(a => a.Id == aircraftId);
        var crew = _gameState.Crew.FirstOrDefault(c => c.Id == crewId);

        if (aircraft == null || crew == null)
            return false;

        if (!aircraft.AssignedCrewIds.Contains(crewId))
            aircraft.AssignedCrewIds.Add(crewId);

        crew.AssignedAircraftId = aircraftId;
        return true;
    }

    /// <summary>
    /// Remove crew from aircraft
    /// </summary>
    public bool RemoveCrewFromAircraft(int aircraftId, int crewId)
    {
        var aircraft = _gameState.Fleet.FirstOrDefault(a => a.Id == aircraftId);
        if (aircraft == null)
            return false;

        aircraft.AssignedCrewIds.Remove(crewId);

        var crew = _gameState.Crew.FirstOrDefault(c => c.Id == crewId);
        if (crew != null)
            crew.AssignedAircraftId = null;

        return true;
    }
}
