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
    private Label? _flightsPerDayLabel;
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
        this.Size = new Size(600, 400);
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
        _originCombo = new ComboBox { Width = 300 };
        panel.Controls.Add(_originCombo, 1, 0);

        // Destination city
        panel.Controls.Add(new Label { Text = "Destination City:", AutoSize = true }, 0, 1);
        _destCombo = new ComboBox { Width = 300 };
        panel.Controls.Add(_destCombo, 1, 1);

        // Aircraft
        panel.Controls.Add(new Label { Text = "Aircraft:", AutoSize = true }, 0, 2);
        _aircraftCombo = new ComboBox { Width = 300 };
        panel.Controls.Add(_aircraftCombo, 1, 2);

        // Ticket price
        panel.Controls.Add(new Label { Text = "Ticket Price (€):", AutoSize = true }, 0, 3);
        _priceTextBox = new TextBox { Text = "100", Width = 300 };
        panel.Controls.Add(_priceTextBox, 1, 3);

        // Distance display
        panel.Controls.Add(new Label { Text = "Distance:", AutoSize = true }, 0, 4);
        _distanceLabel = new Label { Text = "-", Width = 300 };
        panel.Controls.Add(_distanceLabel, 1, 4);

        // Flights per day (auto-calculated)
        panel.Controls.Add(new Label { Text = "Flights Per Day (Auto):", AutoSize = true }, 0, 5);
        _flightsPerDayLabel = new Label { Text = "-", Width = 300, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
        panel.Controls.Add(_flightsPerDayLabel, 1, 5);

        // Estimated revenue
        panel.Controls.Add(new Label { Text = "Est. Daily Revenue:", AutoSize = true }, 0, 6);
        _estimatedRevenueLabel = new Label { Text = "-", Width = 300, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
        panel.Controls.Add(_estimatedRevenueLabel, 1, 6);

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
        _aircraftCombo.SelectedIndexChanged += (s, e) => UpdateDistance();
        _priceTextBox.TextChanged += (s, e) => UpdateEstimatedRevenue();

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
        if (_originCombo?.SelectedItem is not City origin ||
            _destCombo?.SelectedItem is not City dest ||
            _aircraftCombo?.SelectedItem is not Models.Aircraft aircraft)
            return;

        double distance = CityDatabase.CalculateDistance(origin, dest);
        _distanceLabel!.Text = $"{distance:F0} km";

        // Simplified: 1 flight per day that takes ~22 hours to complete
        double flightTimeHours = distance / 500.0;

        _flightsPerDayLabel!.Text = $"1 flight/day | {flightTimeHours:F1}h real flight time → animates across map for ~22 hours";

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

        // Calculate flights per day
        double flightTimeHours = distance / 500.0;
        double turnAroundHours = 2.0;
        double cycleTimeHours = flightTimeHours + turnAroundHours;
        int flightsPerDay = Math.Max(1, Math.Min((int)(24.0 / cycleTimeHours), 5));

        // Simple demand calculation
        double baseDemand = (origin.Population + dest.Population) * 0.001;
        double adjustedDemand = baseDemand - (double)((double)price * 0.0002);
        int passengers = Math.Min((int)adjustedDemand, aircraft.Capacity);

        decimal dailyRevenue = passengers * price * flightsPerDay;
        _estimatedRevenueLabel!.Text = $"{MathUtils.FormatCurrency(dailyRevenue)}/day";
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

            // Use 0 to trigger auto-calculation in CreateRoute
            if (!_routeManager.CreateRoute(origin.Id, dest.Id, aircraft.Id, price, 0))
            {
                MessageBox.Show("Could not create route. Check if aircraft is available and has sufficient range.", "Error");
                e.Cancel = true;
            }
        }

        base.OnFormClosing(e);
    }
}
