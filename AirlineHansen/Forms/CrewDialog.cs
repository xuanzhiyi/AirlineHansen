using AirlineHansen.Engine;
using AirlineHansen.Systems;
using AirlineHansen.Utils;

namespace AirlineHansen.Forms;

public partial class CrewDialog : Form
{
    private GameState _gameState;
    private CrewManager _crewManager;
    private FinanceManager _financeManager;

    private ComboBox? _crewTypeCombo;
    private TextBox? _nameTextBox;
    private TextBox? _salaryTextBox;
    private Label? _suggestedSalaryLabel;
    private Label? _affordabilityLabel;
    private DataGridView? _crewGridView;

    public CrewDialog(GameState gameState, CrewManager crewManager, FinanceManager financeManager)
    {
        _gameState = gameState;
        _crewManager = crewManager;
        _financeManager = financeManager;
        InitializeComponent();
        LoadCrew();
    }

    private void InitializeComponent()
    {
        this.Text = "Crew Management";
        this.Size = new Size(700, 500);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;

        var mainPanel = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 200 };

        // Top panel - Hiring
        var hiringPanel = new GroupBox
        {
            Text = "Hire New Crew",
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

        // Crew type
        table.Controls.Add(new Label { Text = "Crew Type:", AutoSize = true }, 0, 0);
        _crewTypeCombo = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _crewTypeCombo.Items.AddRange(new object[] { "Pilot", "CabinCrew" });
        _crewTypeCombo.SelectedIndex = 0;
        _crewTypeCombo.SelectedIndexChanged += (s, e) => UpdateSuggestedSalary();
        table.Controls.Add(_crewTypeCombo, 1, 0);

        // Name
        table.Controls.Add(new Label { Text = "Name:", AutoSize = true }, 0, 1);
        _nameTextBox = new TextBox { Dock = DockStyle.Fill, Text = "New Crew" };
        table.Controls.Add(_nameTextBox, 1, 1);

        // Salary
        table.Controls.Add(new Label { Text = "Monthly Salary (€):", AutoSize = true }, 0, 2);
        _salaryTextBox = new TextBox { Dock = DockStyle.Fill, Text = "5000" };
        _salaryTextBox.TextChanged += (s, e) => UpdateAffordability();
        table.Controls.Add(_salaryTextBox, 1, 2);

        // Suggested salary
        _suggestedSalaryLabel = new Label { Text = "Suggested: €5,000/month", AutoSize = true };
        table.Controls.Add(_suggestedSalaryLabel, 1, 3);

        // Affordability
        _affordabilityLabel = new Label { Text = "You can afford this.", AutoSize = true, ForeColor = Color.Green };
        table.Controls.Add(_affordabilityLabel, 1, 4);

        hiringPanel.Controls.Add(table);

        var hireButton = new Button { Text = "Hire Crew", Width = 100, Height = 30, Dock = DockStyle.Bottom };
        hireButton.Click += (s, e) => HireCrew();
        hiringPanel.Controls.Add(hireButton);

        mainPanel.Panel1.Controls.Add(hiringPanel);

        // Bottom panel - Current crew list
        var crewPanel = new GroupBox
        {
            Text = "Current Crew",
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
        };

        _crewGridView = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true
        };

        _crewGridView.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID", DataPropertyName = "Id", Width = 40 });
        _crewGridView.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Name", DataPropertyName = "Name", Width = 150 });
        _crewGridView.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Type", DataPropertyName = "CrewType", Width = 100 });
        _crewGridView.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Salary", DataPropertyName = "MonthlySalary", Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "C" } });
        _crewGridView.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Aircraft", DataPropertyName = "AssignedAircraftId", Width = 80 });

        crewPanel.Controls.Add(_crewGridView);

        var fireButton = new Button { Text = "Fire Selected", Width = 100, Height = 30, Dock = DockStyle.Bottom };
        fireButton.Click += (s, e) => FireCrew();
        crewPanel.Controls.Add(fireButton);

        mainPanel.Panel2.Controls.Add(crewPanel);
        this.Controls.Add(mainPanel);
    }

    private void LoadCrew()
    {
        var crew = _crewManager.GetActiveCrew();
        _crewGridView!.DataSource = crew;
        UpdateAffordability();
    }

    private void UpdateSuggestedSalary()
    {
        if (_crewTypeCombo?.SelectedItem is not string crewType)
            return;

        decimal suggestedSalary = _crewManager.GetSuggestedSalary(crewType);
        _suggestedSalaryLabel!.Text = $"Suggested: {MathUtils.FormatCurrency(suggestedSalary)}/month";
        _salaryTextBox!.Text = suggestedSalary.ToString();
    }

    private void UpdateAffordability()
    {
        if (!decimal.TryParse(_salaryTextBox?.Text, out decimal salary))
        {
            _affordabilityLabel!.Text = "Invalid salary amount.";
            _affordabilityLabel.ForeColor = Color.Red;
            return;
        }

        if (_financeManager.CanAfford(salary))
        {
            _affordabilityLabel!.Text = $"You can afford this. (Balance: {MathUtils.FormatCurrency(_gameState.Balance)})";
            _affordabilityLabel.ForeColor = Color.Green;
        }
        else
        {
            _affordabilityLabel!.Text = "You cannot afford this.";
            _affordabilityLabel.ForeColor = Color.Red;
        }
    }

    private void HireCrew()
    {
        if (_crewTypeCombo?.SelectedItem is not string crewType ||
            !decimal.TryParse(_salaryTextBox?.Text, out decimal salary))
        {
            MessageBox.Show("Please fill in all fields correctly.", "Error");
            return;
        }

        string name = _nameTextBox?.Text ?? "Crew Member";

        if (_crewManager.HireCrew(name, crewType, salary))
        {
            MessageBox.Show($"Successfully hired {name}!", "Success");
            _nameTextBox!.Text = "New Crew";
            LoadCrew();
        }
        else
        {
            MessageBox.Show("Could not hire crew. Check your balance.", "Error");
        }
    }

    private void FireCrew()
    {
        if (_crewGridView?.SelectedRows.Count != 1)
        {
            MessageBox.Show("Select a crew member to fire.", "Error");
            return;
        }

        var selectedRow = _crewGridView.SelectedRows[0];
        if (selectedRow.DataBoundItem is Models.Crew crew)
        {
            if (MessageBox.Show($"Fire {crew.Name}?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                _crewManager.FireCrew(crew.Id);
                LoadCrew();
            }
        }
    }
}
