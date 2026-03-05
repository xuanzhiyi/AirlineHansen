using AirlineHansen.Engine;
using AirlineHansen.Systems;
using AirlineHansen.Utils;

namespace AirlineHansen.Forms;

public partial class AircraftDialog : Form
{
    private GameState _gameState;
    private AircraftManager _aircraftManager;
    private FinanceManager _financeManager;

    private ComboBox? _aircraftTypeCombo;
    private Label? _specLabel;
    private Label? _priceLabel;
    private Label? _affordabilityLabel;
    private DataGridView? _fleetGridView;

    private readonly (string name, int capacity, int range, decimal fuelCost, decimal price)[] _aircraftTypes = new[]
    {
        ("Airbus A220 (Small)", 50, 1500, 0.05m, 5_000_000m),
        ("Airbus A320 (Medium)", 150, 3000, 0.08m, 15_000_000m),
        ("Airbus A350 (Large)", 300, 5000, 0.12m, 30_000_000m),
    };

    public AircraftDialog(GameState gameState, AircraftManager aircraftManager, FinanceManager financeManager)
    {
        _gameState = gameState;
        _aircraftManager = aircraftManager;
        _financeManager = financeManager;
        InitializeComponent();
        LoadFleet();
    }

    private void InitializeComponent()
    {
        this.Text = "Aircraft Management";
        this.Size = new Size(700, 500);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;

        var mainPanel = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 200 };

        // Top panel - Purchase
        var purchasePanel = new GroupBox
        {
            Text = "Purchase New Aircraft",
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
        };

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 4,
            AutoSize = true,
            Padding = new Padding(5)
        };

        // Aircraft type
        table.Controls.Add(new Label { Text = "Aircraft Type:", AutoSize = true }, 0, 0);
        _aircraftTypeCombo = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        foreach (var aircraft in _aircraftTypes)
        {
            _aircraftTypeCombo.Items.Add(aircraft.name);
        }

        _aircraftTypeCombo.SelectedIndex = 0;
        _aircraftTypeCombo.SelectedIndexChanged += (s, e) => UpdateSpecs();
        table.Controls.Add(_aircraftTypeCombo, 1, 0);

        // Specifications
        _specLabel = new Label
        {
            Text = "Specs: ...",
            AutoSize = true,
            Dock = DockStyle.Top
        };
        table.Controls.Add(new Label { Text = "Specifications:", AutoSize = true }, 0, 1);
        table.Controls.Add(_specLabel, 1, 1);

        // Price
        _priceLabel = new Label { Text = "Price: ...", AutoSize = true };
        table.Controls.Add(new Label { Text = "Price:", AutoSize = true }, 0, 2);
        table.Controls.Add(_priceLabel, 1, 2);

        // Affordability
        _affordabilityLabel = new Label { Text = "You can afford this.", AutoSize = true, ForeColor = Color.Green };
        table.Controls.Add(_affordabilityLabel, 1, 3);

        purchasePanel.Controls.Add(table);

        var buyButton = new Button { Text = "Purchase Aircraft", Width = 150, Height = 30, Dock = DockStyle.Bottom };
        buyButton.Click += (s, e) => PurchaseAircraft();
        purchasePanel.Controls.Add(buyButton);

        mainPanel.Panel1.Controls.Add(purchasePanel);

        // Bottom panel - Fleet list
        var fleetPanel = new GroupBox
        {
            Text = "Current Fleet",
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
        };

        _fleetGridView = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true
        };

        _fleetGridView.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID", DataPropertyName = "Id", Width = 40 });
        _fleetGridView.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Model", DataPropertyName = "Model", Width = 150 });
        _fleetGridView.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Capacity", DataPropertyName = "Capacity", Width = 80 });
        _fleetGridView.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Range (km)", DataPropertyName = "MaxRange", Width = 100 });
        _fleetGridView.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Available", DataPropertyName = "IsAvailable", Width = 80 });

        fleetPanel.Controls.Add(_fleetGridView);

        var sellButton = new Button { Text = "Sell Selected (60% value)", Width = 200, Height = 30, Dock = DockStyle.Bottom };
        sellButton.Click += (s, e) => SellAircraft();
        fleetPanel.Controls.Add(sellButton);

        mainPanel.Panel2.Controls.Add(fleetPanel);
        this.Controls.Add(mainPanel);

        UpdateSpecs();
    }

    private void LoadFleet()
    {
        var fleet = _gameState.Fleet;
        _fleetGridView!.DataSource = fleet.ToList();
    }

    private void UpdateSpecs()
    {
        if (_aircraftTypeCombo?.SelectedIndex < 0 || _aircraftTypeCombo.SelectedIndex >= _aircraftTypes.Length)
            return;

        var aircraft = _aircraftTypes[_aircraftTypeCombo.SelectedIndex];
        _specLabel!.Text = $"{aircraft.capacity} seats | {aircraft.range} km range | €{aircraft.fuelCost}/km fuel";
        _priceLabel!.Text = MathUtils.FormatCurrency(aircraft.price);

        if (_financeManager.CanAfford(aircraft.price))
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

    private void PurchaseAircraft()
    {
        if (_aircraftTypeCombo?.SelectedIndex < 0)
        {
            MessageBox.Show("Select an aircraft type.", "Error");
            return;
        }

        var aircraft = _aircraftTypes[_aircraftTypeCombo.SelectedIndex];

        if (!_financeManager.CanAfford(aircraft.price))
        {
            MessageBox.Show("You cannot afford this aircraft.", "Error");
            return;
        }

        if (_aircraftManager.PurchaseAircraft(aircraft.name, aircraft.capacity, aircraft.range, aircraft.fuelCost, aircraft.price))
        {
            MessageBox.Show($"Successfully purchased {aircraft.name}!", "Success");
            LoadFleet();
            UpdateSpecs();
        }
        else
        {
            MessageBox.Show("Could not purchase aircraft.", "Error");
        }
    }

    private void SellAircraft()
    {
        if (_fleetGridView?.SelectedRows.Count != 1)
        {
            MessageBox.Show("Select an aircraft to sell.", "Error");
            return;
        }

        var selectedRow = _fleetGridView.SelectedRows[0];
        if (selectedRow.DataBoundItem is Models.Aircraft aircraft)
        {
            if (MessageBox.Show($"Sell {aircraft.Model}? You will receive 60% of its value.", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                if (_aircraftManager.SellAircraft(aircraft.Id))
                {
                    MessageBox.Show("Aircraft sold successfully.", "Success");
                    LoadFleet();
                }
                else
                {
                    MessageBox.Show("Cannot sell this aircraft. It may be in use on a route.", "Error");
                }
            }
        }
    }
}
