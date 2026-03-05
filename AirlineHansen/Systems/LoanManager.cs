using AirlineHansen.Engine;
using AirlineHansen.Models;
using AirlineHansen.Utils;

namespace AirlineHansen.Systems;

/// <summary>
/// Manages company loans and debt
/// </summary>
public class LoanManager
{
    private GameState _gameState;
    private FinanceManager _financeManager;

    public LoanManager(GameState gameState, FinanceManager financeManager)
    {
        _gameState = gameState;
        _financeManager = financeManager;
    }

    /// <summary>
    /// Take out a new loan
    /// </summary>
    public bool TakeLoan(decimal amount)
    {
        if (amount <= 0)
            return false;

        var loan = new Loan(
            _gameState.NextLoanId++,
            amount,
            Constants.LOAN_INTEREST_RATE
        );

        _gameState.Loans.Add(loan);
        _financeManager.RecordTransaction(_gameState.GameTime, $"Loan taken: €{amount:F2}", amount);

        return true;
    }

    /// <summary>
    /// Repay part or all of a loan
    /// </summary>
    public bool RepayLoan(int loanId, decimal amount)
    {
        var loan = _gameState.Loans.FirstOrDefault(l => l.Id == loanId);
        if (loan == null || !loan.IsActive)
            return false;

        if (!_financeManager.CanAfford(amount))
            return false;

        decimal amountToPay = Math.Min(amount, loan.RemainingBalance);
        loan.RemainingBalance -= amountToPay;

        if (loan.RemainingBalance <= 0)
        {
            loan.RemainingBalance = 0;
            loan.IsActive = false;
        }

        _financeManager.RecordTransaction(_gameState.GameTime, $"Loan repayment", -amountToPay);
        return true;
    }

    /// <summary>
    /// Get all active loans
    /// </summary>
    public List<Loan> GetActiveLoans()
    {
        return _gameState.Loans.Where(l => l.IsActive).ToList();
    }

    /// <summary>
    /// Get all loans (active and paid off)
    /// </summary>
    public List<Loan> GetAllLoans()
    {
        return new List<Loan>(_gameState.Loans);
    }

    /// <summary>
    /// Get total debt (all remaining loan balances)
    /// </summary>
    public decimal GetTotalDebt()
    {
        return _gameState.Loans.Sum(l => l.RemainingBalance);
    }

    /// <summary>
    /// Get estimated monthly interest payment
    /// </summary>
    public decimal GetMonthlyInterestPayment()
    {
        return _gameState.Loans.Sum(l => l.CalculateDailyInterest() * 30);
    }

    /// <summary>
    /// Get loan details
    /// </summary>
    public (decimal remainingBalance, decimal totalInterestPaid, double interestRate) GetLoanDetails(int loanId)
    {
        var loan = _gameState.Loans.FirstOrDefault(l => l.Id == loanId);
        if (loan == null)
            return (0, 0, 0);

        return (loan.RemainingBalance, loan.TotalInterestPaid, loan.InterestRate);
    }

    /// <summary>
    /// Check maximum loan amount player can take
    /// </summary>
    public decimal GetMaxLoanAmount()
    {
        decimal netWorth = _financeManager.CalculateNetWorth();
        return Math.Max(100_000m, netWorth * 0.5m); // Can borrow up to 50% of net worth, minimum 100k
    }

    /// <summary>
    /// Get suggested monthly repayment for a loan
    /// </summary>
    public decimal GetSuggestedMonthlyRepayment(decimal loanAmount)
    {
        // Suggest paying back over 60 months at 8% interest
        decimal monthlyRate = (decimal)(Constants.LOAN_INTEREST_RATE / 12.0);
        int months = 60;

        if (monthlyRate == 0)
            return loanAmount / months;

        decimal payment = loanAmount * (monthlyRate * (decimal)Math.Pow(1 + (double)monthlyRate, months)) /
                          ((decimal)Math.Pow(1 + (double)monthlyRate, months) - 1);

        return payment;
    }
}
