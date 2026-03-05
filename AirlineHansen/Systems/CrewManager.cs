using AirlineHansen.Engine;
using AirlineHansen.Models;

namespace AirlineHansen.Systems;

/// <summary>
/// Manages crew hiring and management
/// </summary>
public class CrewManager
{
    private GameState _gameState;
    private FinanceManager _financeManager;

    public CrewManager(GameState gameState, FinanceManager financeManager)
    {
        _gameState = gameState;
        _financeManager = financeManager;
    }

    /// <summary>
    /// Hire a crew member
    /// </summary>
    public bool HireCrew(string name, string crewType, decimal monthlySalary)
    {
        if (!_financeManager.CanAfford(monthlySalary))
            return false;

        var crew = new Crew(
            _gameState.NextCrewId++,
            name,
            crewType,
            monthlySalary
        );

        _gameState.Crew.Add(crew);
        _financeManager.RecordTransaction(_gameState.GameTime, $"Hired {crewType}: {name}", -monthlySalary);

        return true;
    }

    /// <summary>
    /// Fire a crew member
    /// </summary>
    public bool FireCrew(int crewId)
    {
        var crew = _gameState.Crew.FirstOrDefault(c => c.Id == crewId);
        if (crew == null)
            return false;

        crew.IsActive = false;

        // Unassign from aircraft
        if (crew.AssignedAircraftId.HasValue)
        {
            var aircraft = _gameState.Fleet.FirstOrDefault(a => a.Id == crew.AssignedAircraftId);
            if (aircraft != null)
                aircraft.AssignedCrewIds.Remove(crewId);
        }

        return true;
    }

    /// <summary>
    /// Get all active crew
    /// </summary>
    public List<Crew> GetActiveCrew()
    {
        return _gameState.Crew.Where(c => c.IsActive).ToList();
    }

    /// <summary>
    /// Get crew by type
    /// </summary>
    public List<Crew> GetCrewByType(string crewType)
    {
        return _gameState.Crew.Where(c => c.IsActive && c.CrewType == crewType).ToList();
    }

    /// <summary>
    /// Get crew assigned to aircraft
    /// </summary>
    public List<Crew> GetCrewForAircraft(int aircraftId)
    {
        return _gameState.Crew.Where(c =>
            c.IsActive &&
            c.AssignedAircraftId == aircraftId).ToList();
    }

    /// <summary>
    /// Get unassigned crew (available for assignment)
    /// </summary>
    public List<Crew> GetUnassignedCrew()
    {
        return _gameState.Crew.Where(c => c.IsActive && !c.AssignedAircraftId.HasValue).ToList();
    }

    /// <summary>
    /// Calculate total crew salary expenses
    /// </summary>
    public decimal CalculateMonthlySalaryExpense()
    {
        return _gameState.Crew.Where(c => c.IsActive).Sum(c => c.MonthlySalary);
    }

    /// <summary>
    /// Get suggested salary for crew type
    /// </summary>
    public decimal GetSuggestedSalary(string crewType)
    {
        return crewType switch
        {
            "Pilot" => 5000m,
            "CabinCrew" => 3000m,
            _ => 3000m
        };
    }
}
