using AirlineHansen.Data;
using AirlineHansen.Engine;
using AirlineHansen.Models;
using AirlineHansen.Systems;
using AirlineHansen.Utils;

namespace AirlineHansen.Forms;

public partial class RouteDialog : Form
{
    private GameState _gameState;
    private RouteManager _routeManager;

    private ComboBox? _originCombo;
    private ComboBox? _destCombo;
    private ComboBox? _aircraftCombo;
    private TextBox? _priceTextBox;
    private NumericUpDown? _flightsPerDayUpDown;
    private Label? _distanceLabel;
    private Label? _estimatedRevenueLabel;

    public RouteDialog(GameState gameState, RouteManager routeManager)
    {
        _gameState = gameState;
        _routeManager = routeManager;
        InitializeComponent();
        LoadData();
    }

    private void InitializeComponent()
    {
        this.Text = "Create New Route";
        this.Size = new Size(500, 400);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;

        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 7,
            Padding = new Padding(10)
        };

        // Origin city
        panel.Controls.Add(new Label { Text = "Origin City:", AutoSize = true }, 0, 0);
        _originCombo = new ComboBox { Dock = DockStyle.Fill };
        panel.Controls.Add(_originCombo, 1, 0);

        // Destination city
        panel.Controls.Add(new Label { Text = "Destination City:", AutoSize = true }, 0, 1);
        _destCombo = new ComboBox { Dock = DockStyle.Fill };
        panel.Controls.Add(_destCombo, 1, 1);

        // Aircraft
        panel.Controls.Add(new Label { Text = "Aircraft:", AutoSize = true }, 0, 2);
        _aircraftCombo = new ComboBox { Dock = DockStyle.Fill };
        panel.Controls.Add(_aircraftCombo, 1, 2);

        // Ticket price
        panel.Controls.Add(new Label { Text = "Ticket Price (€):", AutoSize = true }, 0, 3);
        _priceTextBox = new TextBox { Text = "100", Dock = DockStyle.Fill };
        panel.Controls.Add(_priceTextBox, 1, 3);

        // Flights per day
        panel.Controls.Add(new Label { Text = "Flights Per Day:", AutoSize = true }, 0, 4);
        _flightsPerDayUpDown = new NumericUpDown { Value = 1, Minimum = 1, Maximum = 5, Dock = DockStyle.Fill };
        panel.Controls.Add(_flightsPerDayUpDown, 1, 4);

        // Distance display
        _distanceLabel = new Label { Text = "Distance: -", AutoSize = true };
        panel.Controls.Add(_distanceLabel, 0, 5);

        // Estimated revenue
        _estimatedRevenueLabel = new Label { Text = "Est. Daily Revenue: -", AutoSize = true };
        panel.Controls.Add(_estimatedRevenueLabel, 0, 6);

        this.Controls.Add(panel);

        // Buttons
        var buttonPanel = new Panel { Dock = DockStyle.Bottom, Height = 50 };
        var createButton = new Button { Text = "Create", Width = 80, Height = 30, Location = new Point(350, 10), DialogResult = DialogResult.OK };
        var cancelButton = new Button { Text = "Cancel", Width = 80, Height = 30, Location = new Point(440, 10), DialogResult = DialogResult.Cancel };

        buttonPanel.Controls.Add(createButton);
        buttonPanel.Controls.Add(cancelButton);
        this.Controls.Add(buttonPanel);

        // Event handlers
        _originCombo.SelectedIndexChanged += (s, e) => UpdateDistance();
        _destCombo.SelectedIndexChanged += (s, e) => UpdateDistance();
        _priceTextBox.TextChanged += (s, e) => UpdateEstimatedRevenue();
        _flightsPerDayUpDown.ValueChanged += (s, e) => UpdateEstimatedRevenue();

        this.AcceptButton = createButton;
        this.CancelButton = cancelButton;
    }

    private void LoadData()
    {
        // Load cities
        var cities = CityDatabase.GetAllCities();
        _originCombo?.DataSource = cities;
        _originCombo?.DisplayMember = "Name";
        _originCombo?.ValueMember = "Id";

        var destCities = new List<City>(cities);
        _destCombo?.DataSource = destCities;
        _destCombo?.DisplayMember = "Name";
        _destCombo?.ValueMember = "Id";

        // Load available aircraft
        var availableAircraft = _gameState.GetAvailableAircraft();
        _aircraftCombo?.DataSource = availableAircraft;
        _aircraftCombo?.DisplayMember = "Model";
        _aircraftCombo?.ValueMember = "Id";
    }

    private void UpdateDistance()
    {
        if (_originCombo?.SelectedItem is not City origin || _destCombo?.SelectedItem is not City dest)
            return;

        double distance = CityDatabase.CalculateDistance(origin, dest);
        _distanceLabel!.Text = $"Distance: {distance:F0} km";

        UpdateEstimatedRevenue();
    }

    private void UpdateEstimatedRevenue()
    {
        if (_originCombo?.SelectedItem is not City origin ||
            _destCombo?.SelectedItem is not City dest ||
            _aircraftCombo?.SelectedItem is not Models.Aircraft aircraft ||
            !decimal.TryParse(_priceTextBox?.Text, out decimal price))
            return;

        double distance = CityDatabase.CalculateDistance(origin, dest);

        // Simple demand calculation
        double baseDemand = (origin.Population + dest.Population) * 0.001;
        double adjustedDemand = baseDemand - (double)((double)price * 0.0002);
        int passengers = Math.Min((int)adjustedDemand, aircraft.Capacity);

        decimal dailyRevenue = passengers * price * (int)(_flightsPerDayUpDown?.Value ?? 1);
        _estimatedRevenueLabel!.Text = $"Est. Daily Revenue: {MathUtils.FormatCurrency(dailyRevenue)}";
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (this.DialogResult == DialogResult.OK)
        {
            // Validate and create route
            if (_originCombo?.SelectedItem is not City origin ||
                _destCombo?.SelectedItem is not City dest ||
                _aircraftCombo?.SelectedItem is not Models.Aircraft aircraft ||
                !decimal.TryParse(_priceTextBox?.Text, out decimal price))
            {
                MessageBox.Show("Please fill in all fields correctly.", "Error");
                e.Cancel = true;
                return;
            }

            int flightsPerDay = (int)(_flightsPerDayUpDown?.Value ?? 1);

            if (!_routeManager.CreateRoute(origin.Id, dest.Id, aircraft.Id, price, flightsPerDay))
            {
                MessageBox.Show("Could not create route. Check if aircraft is available and has sufficient range.", "Error");
                e.Cancel = true;
            }
        }

        base.OnFormClosing(e);
    }
}
