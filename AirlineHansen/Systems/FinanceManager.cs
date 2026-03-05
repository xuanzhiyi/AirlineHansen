using AirlineHansen.Engine;

namespace AirlineHansen.Systems;

/// <summary>
/// Manages all financial transactions and reporting
/// </summary>
public class FinanceManager
{
    private GameState _gameState;
    private List<(DateTime date, string description, decimal amount)> _transactionHistory = new();

    public FinanceManager(GameState gameState)
    {
        _gameState = gameState;
    }

    /// <summary>
    /// Record a transaction
    /// </summary>
    public void RecordTransaction(DateTime date, string description, decimal amount)
    {
        _transactionHistory.Add((date, description, amount));
        _gameState.Balance += amount;

        if (amount > 0)
            _gameState.TotalIncome += amount;
        else
            _gameState.TotalExpenses += Math.Abs(amount);
    }

    /// <summary>
    /// Get transaction history
    /// </summary>
    public List<(DateTime date, string description, decimal amount)> GetTransactionHistory()
    {
        return new List<(DateTime, string, decimal)>(_transactionHistory);
    }

    /// <summary>
    /// Get transactions for a specific date
    /// </summary>
    public List<(DateTime date, string description, decimal amount)> GetTransactionsForDate(DateTime date)
    {
        return _transactionHistory.Where(t => t.date.Date == date.Date).ToList();
    }

    /// <summary>
    /// Get financial summary
    /// </summary>
    public (decimal balance, decimal totalIncome, decimal totalExpenses, decimal netProfit) GetFinancialSummary()
    {
        decimal netProfit = _gameState.TotalIncome - _gameState.TotalExpenses;
        return (_gameState.Balance, _gameState.TotalIncome, _gameState.TotalExpenses, netProfit);
    }

    /// <summary>
    /// Calculate daily profit/loss
    /// </summary>
    public decimal CalculateDailyProfit()
    {
        var todayTransactions = _transactionHistory.Where(t => t.date.Date == _gameState.GameTime.Date);
        return todayTransactions.Sum(t => t.amount);
    }

    /// <summary>
    /// Calculate net worth (assets minus liabilities)
    /// </summary>
    public decimal CalculateNetWorth()
    {
        decimal assetValue = _gameState.Fleet.Sum(a => a.PurchasePrice) * 0.6m; // Aircraft depreciate to 60% value
        decimal liabilities = _gameState.Loans.Sum(l => l.RemainingBalance);
        return _gameState.Balance + assetValue - liabilities;
    }

    /// <summary>
    /// Check if player can afford an expense
    /// </summary>
    public bool CanAfford(decimal amount)
    {
        return _gameState.Balance >= amount;
    }

    /// <summary>
    /// Clear transaction history (for testing)
    /// </summary>
    public void ClearHistory()
    {
        _transactionHistory.Clear();
    }
}
