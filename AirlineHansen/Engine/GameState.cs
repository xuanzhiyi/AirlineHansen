using AirlineHansen.Models;

namespace AirlineHansen.Engine;

/// <summary>
/// Central game state management - holds all game data
/// </summary>
public class GameState
{
    // Game timing
    public DateTime GameTime { get; set; }
    public int GameDay { get; set; } = 1;

    // Company finances
    public decimal Balance { get; set; } = 100_000m; // Starting capital in euros
    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal DailyProfit { get; set; }

    // Assets
    public List<Aircraft> Fleet { get; set; } = new();
    public List<Route> Routes { get; set; } = new();
    public List<Flight> AllFlights { get; set; } = new();
    public List<Crew> Crew { get; set; } = new();
    public List<Loan> Loans { get; set; } = new();

    // Counters for IDs
    public int NextAircraftId { get; set; } = 1;
    public int NextCrewId { get; set; } = 1;
    public int NextRouteId { get; set; } = 1;
    public int NextFlightId { get; set; } = 1;
    public int NextLoanId { get; set; } = 1;

    // Game status
    public bool IsGameRunning { get; set; } = true;
    public bool IsGameOver { get; set; } = false;
    public string GameOverReason { get; set; } = string.Empty;

    public GameState()
    {
        GameTime = new DateTime(2024, 1, 1);
    }

    /// <summary>
    /// Initialize game with starting aircraft
    /// </summary>
    public void Initialize()
    {
        // Create starting aircraft (Small aircraft)
        var startingAircraft = new Aircraft(
            NextAircraftId++,
            "Airbus A220",
            50,  // seats
            1500, // km range
            0.05m, // €0.05 per km
            5_000_000m
        );

        Fleet.Add(startingAircraft);
    }

    /// <summary>
    /// Advance game time by one day
    /// </summary>
    public void AdvanceDayOneTick()
    {
        GameDay++;
        GameTime = GameTime.AddDays(1);
    }

    /// <summary>
    /// Add transaction (income or expense)
    /// </summary>
    public void AddTransaction(decimal amount, string description)
    {
        if (amount > 0)
        {
            TotalIncome += amount;
        }
        else
        {
            TotalExpenses += Math.Abs(amount);
        }

        Balance += amount;
    }

    /// <summary>
    /// Get all active routes
    /// </summary>
    public List<Route> GetActiveRoutes()
    {
        return Routes.Where(r => r.IsActive).ToList();
    }

    /// <summary>
    /// Get all active aircraft
    /// </summary>
    public List<Aircraft> GetAvailableAircraft()
    {
        return Fleet.Where(a => a.IsAvailable).ToList();
    }

    /// <summary>
    /// Get all active crew
    /// </summary>
    public List<Crew> GetActiveCrew()
    {
        return Crew.Where(c => c.IsActive).ToList();
    }

    /// <summary>
    /// Get all active loans
    /// </summary>
    public List<Loan> GetActiveLoans()
    {
        return Loans.Where(l => l.IsActive).ToList();
    }

    /// <summary>
    /// Check if game should be over (bankruptcy)
    /// </summary>
    public void CheckGameOver()
    {
        if (Balance < 0)
        {
            IsGameRunning = false;
            IsGameOver = true;
            GameOverReason = "Your airline company has gone bankrupt!";
        }
    }
}
