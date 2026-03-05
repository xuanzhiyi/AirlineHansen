using AirlineHansen.Engine;
using AirlineHansen.Systems;
using AirlineHansen.Utils;

namespace AirlineHansen.Forms;

public partial class LoanDialog : Form
{
    private GameState _gameState;
    private LoanManager _loanManager;
    private FinanceManager _financeManager;

    private TextBox? _borrowAmountTextBox;
    private Label? _maxBorrowLabel;
    private Label? _estimatedMonthlyPaymentLabel;
    private Label? _affordabilityLabel;
    private DataGridView? _loansGridView;
    private TextBox? _repayAmountTextBox;

    public LoanDialog(GameState gameState, LoanManager loanManager, FinanceManager financeManager)
    {
        _gameState = gameState;
        _loanManager = loanManager;
        _financeManager = financeManager;
        InitializeComponent();
        LoadLoans();
    }

    private void InitializeComponent()
    {
        this.Text = "Loan Management";
        this.Size = new Size(700, 550);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;

        var mainPanel = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 200 };

        // Top panel - Borrowing
        var borrowPanel = new GroupBox
        {
            Text = "Take Out New Loan",
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
        };

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 5,
            AutoSize = true,
            Padding = new Padding(5)
        };

        // Borrow amount
        table.Controls.Add(new Label { Text = "Borrow Amount (€):", AutoSize = true }, 0, 0);
        _borrowAmountTextBox = new TextBox { Dock = DockStyle.Fill, Text = "100000" };
        _borrowAmountTextBox.TextChanged += (s, e) => UpdateLoanInfo();
        table.Controls.Add(_borrowAmountTextBox, 1, 0);

        // Max borrow
        _maxBorrowLabel = new Label { Text = "Maximum: ...", AutoSize = true };
        table.Controls.Add(_maxBorrowLabel, 1, 1);

        // Estimated monthly payment
        _estimatedMonthlyPaymentLabel = new Label { Text = "Est. Monthly Payment: ...", AutoSize = true };
        table.Controls.Add(_estimatedMonthlyPaymentLabel, 1, 2);

        // Interest rate info
        table.Controls.Add(new Label { Text = "Interest Rate:", AutoSize = true }, 0, 3);
        table.Controls.Add(new Label { Text = "8.0% annual (compounded daily)", AutoSize = true }, 1, 3);

        // Affordability
        _affordabilityLabel = new Label { Text = "You can take this loan.", AutoSize = true, ForeColor = Color.Green };
        table.Controls.Add(_affordabilityLabel, 1, 4);

        borrowPanel.Controls.Add(table);

        var borrowButton = new Button { Text = "Take Loan", Width = 100, Height = 30, Dock = DockStyle.Bottom };
        borrowButton.Click += (s, e) => TakeLoan();
        borrowPanel.Controls.Add(borrowButton);

        mainPanel.Panel1.Controls.Add(borrowPanel);

        // Bottom panel - Active loans
        var loansPanel = new GroupBox
        {
            Text = "Active Loans",
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
        };

        var loansTable = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 2,
            AutoSize = true,
            Padding = new Padding(5)
        };

        _loansGridView = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            Height = 250
        };

        _loansGridView.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID", DataPropertyName = "Id", Width = 40 });
        _loansGridView.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Original Amount", DataPropertyName = "Amount", Width = 120, DefaultCellStyle = new DataGridViewCellStyle { Format = "C" } });
        _loansGridView.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Remaining", DataPropertyName = "RemainingBalance", Width = 120, DefaultCellStyle = new DataGridViewCellStyle { Format = "C" } });
        _loansGridView.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Interest Paid", DataPropertyName = "TotalInterestPaid", Width = 120, DefaultCellStyle = new DataGridViewCellStyle { Format = "C" } });
        _loansGridView.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Rate", DataPropertyName = "InterestRate", Width = 60, DefaultCellStyle = new DataGridViewCellStyle { Format = "P" } });

        loansPanel.Controls.Add(_loansGridView);

        // Repayment section
        var repayPanel = new Panel { Dock = DockStyle.Bottom, Height = 80 };
        repayPanel.Controls.Add(new Label { Text = "Repay Amount (€):", Location = new Point(10, 10), AutoSize = true });
        _repayAmountTextBox = new TextBox { Location = new Point(150, 10), Width = 100, Height = 20 };
        repayPanel.Controls.Add(_repayAmountTextBox);

        var repayButton = new Button { Text = "Repay Selected Loan", Location = new Point(260, 10), Width = 150, Height = 25 };
        repayButton.Click += (s, e) => RepayLoan();
        repayPanel.Controls.Add(repayButton);

        var totalDebtLabel = new Label { Location = new Point(10, 40), AutoSize = true };
        totalDebtLabel.Text = $"Total Debt: {MathUtils.FormatCurrency(_loanManager.GetTotalDebt())}";
        totalDebtLabel.Tag = totalDebtLabel; // Store reference to update later
        repayPanel.Controls.Add(totalDebtLabel);

        loansPanel.Controls.Add(repayPanel);
        mainPanel.Panel2.Controls.Add(loansPanel);
        this.Controls.Add(mainPanel);

        UpdateLoanInfo();
    }

    private void LoadLoans()
    {
        var loans = _loanManager.GetActiveLoans();
        _loansGridView!.DataSource = loans;
    }

    private void UpdateLoanInfo()
    {
        if (!decimal.TryParse(_borrowAmountTextBox?.Text, out decimal borrowAmount) || borrowAmount <= 0)
        {
            _maxBorrowLabel!.Text = "Maximum: Enter valid amount";
            _estimatedMonthlyPaymentLabel!.Text = "Est. Monthly Payment: -";
            _affordabilityLabel!.Text = "Invalid amount.";
            _affordabilityLabel.ForeColor = Color.Red;
            return;
        }

        decimal maxBorrow = _loanManager.GetMaxLoanAmount();
        _maxBorrowLabel!.Text = $"Maximum: {MathUtils.FormatCurrency(maxBorrow)}";

        decimal monthlyPayment = _loanManager.GetSuggestedMonthlyRepayment(borrowAmount);
        _estimatedMonthlyPaymentLabel!.Text = $"Est. Monthly Payment: {MathUtils.FormatCurrency(monthlyPayment)}";

        if (borrowAmount <= maxBorrow)
        {
            _affordabilityLabel!.Text = "You can take this loan.";
            _affordabilityLabel.ForeColor = Color.Green;
        }
        else
        {
            _affordabilityLabel!.Text = $"Maximum loan: {MathUtils.FormatCurrency(maxBorrow)}";
            _affordabilityLabel.ForeColor = Color.Red;
        }
    }

    private void TakeLoan()
    {
        if (!decimal.TryParse(_borrowAmountTextBox?.Text, out decimal amount) || amount <= 0)
        {
            MessageBox.Show("Enter a valid amount.", "Error");
            return;
        }

        decimal maxBorrow = _loanManager.GetMaxLoanAmount();
        if (amount > maxBorrow)
        {
            MessageBox.Show($"Maximum loan amount is {MathUtils.FormatCurrency(maxBorrow)}", "Error");
            return;
        }

        decimal monthlyPayment = _loanManager.GetSuggestedMonthlyRepayment(amount);
        string msg = $"Borrow {MathUtils.FormatCurrency(amount)}?\n\n" +
                     $"Interest Rate: 8% annual\n" +
                     $"Est. Monthly Payment: {MathUtils.FormatCurrency(monthlyPayment)}\n\n" +
                     $"This loan will accrue interest daily until repaid.";

        if (MessageBox.Show(msg, "Confirm Loan", MessageBoxButtons.YesNo) == DialogResult.Yes)
        {
            if (_loanManager.TakeLoan(amount))
            {
                MessageBox.Show("Loan approved!", "Success");
                _borrowAmountTextBox!.Text = "100000";
                LoadLoans();
            }
            else
            {
                MessageBox.Show("Could not create loan.", "Error");
            }
        }
    }

    private void RepayLoan()
    {
        if (_loansGridView?.SelectedRows.Count != 1)
        {
            MessageBox.Show("Select a loan to repay.", "Error");
            return;
        }

        if (!decimal.TryParse(_repayAmountTextBox?.Text, out decimal amount) || amount <= 0)
        {
            MessageBox.Show("Enter a valid repayment amount.", "Error");
            return;
        }

        var selectedRow = _loansGridView.SelectedRows[0];
        if (selectedRow.DataBoundItem is Models.Loan loan)
        {
            if (!_financeManager.CanAfford(amount))
            {
                MessageBox.Show("You cannot afford this repayment.", "Error");
                return;
            }

            if (_loanManager.RepayLoan(loan.Id, amount))
            {
                MessageBox.Show($"Repaid {MathUtils.FormatCurrency(amount)} towards loan {loan.Id}.", "Success");
                _repayAmountTextBox!.Text = "";
                LoadLoans();
            }
            else
            {
                MessageBox.Show("Could not repay loan.", "Error");
            }
        }
    }
}
