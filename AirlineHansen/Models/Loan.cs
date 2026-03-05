namespace AirlineHansen.Models;

/// <summary>
/// Represents a loan taken by the airline company
/// </summary>
public class Loan
{
    public int Id { get; set; }

    /// <summary>
    /// Original loan amount in euros
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Current remaining balance
    /// </summary>
    public decimal RemainingBalance { get; set; }

    /// <summary>
    /// Annual interest rate (e.g., 0.08 for 8%)
    /// </summary>
    public double InterestRate { get; set; }

    /// <summary>
    /// Loan taken date
    /// </summary>
    public DateTime LoanDate { get; set; }

    /// <summary>
    /// Is the loan still active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Total interest paid so far
    /// </summary>
    public decimal TotalInterestPaid { get; set; }

    public Loan()
    {
        LoanDate = DateTime.Now;
    }

    public Loan(int id, decimal amount, double interestRate)
    {
        Id = id;
        Amount = amount;
        RemainingBalance = amount;
        InterestRate = interestRate;
        LoanDate = DateTime.Now;
    }

    /// <summary>
    /// Calculate daily interest
    /// </summary>
    public decimal CalculateDailyInterest()
    {
        return RemainingBalance * (decimal)(InterestRate / 365.0);
    }

    public override string ToString() => $"Loan {Id}: €{RemainingBalance:F2} remaining (${InterestRate * 100:F1}%)";
}
