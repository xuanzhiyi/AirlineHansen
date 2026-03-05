using AirlineHansen.Controls;
using AirlineHansen.Data;
using AirlineHansen.Engine;
using AirlineHansen.Systems;
using AirlineHansen.Utils;
using Timer = System.Windows.Forms.Timer;

namespace AirlineHansen.Forms;

public partial class MainForm : Form
{
    // Game systems
    private GameState _gameState = new();
    private TimeManager _timeManager = new();
    private SimulationEngine _simulationEngine;
    private FinanceManager _financeManager;
    private AircraftManager _aircraftManager;
    private RouteManager _routeManager;
    private CrewManager _crewManager;
    private LoanManager _loanManager;

    // UI Components
    private MapControl? _mapControl;
    private Timer? _gameLoopTimer;
    private Label? _statusLabel;
    private Label? _balanceLabel;
    private Label? _dayLabel;
    private Button? _pauseButton;
    private Button? _speedUpButton;
    private Button? _speedDownButton;

    public MainForm()
    {
        InitializeComponent();

        // Initialize game systems
        _gameState.Initialize();
        _simulationEngine = new SimulationEngine(_gameState, _timeManager);
        _financeManager = new FinanceManager(_gameState);
        _aircraftManager = new AircraftManager(_gameState, _financeManager);
        _routeManager = new RouteManager(_gameState, _financeManager);
        _crewManager = new CrewManager(_gameState, _financeManager);
        _loanManager = new LoanManager(_gameState, _financeManager);

        SetupUI();
        StartGameLoop();
    }

    private void InitializeComponent()
    {
        this.ClientSize = new Size(1400, 900);
        this.Text = "Airline Tycoon - Empire Building Simulator";
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = Color.LightGray;

        // Main layout with menu bar
        var mainPanel = new Panel { Dock = DockStyle.Fill };
        this.Controls.Add(mainPanel);

        // Menu strip
        var menuStrip = new MenuStrip();
        var gameMenu = new ToolStripMenuItem("Game");
        gameMenu.DropDownItems.Add(new ToolStripMenuItem("New Game", null, (s, e) => NewGame()));
        gameMenu.DropDownItems.Add(new ToolStripSeparator());
        gameMenu.DropDownItems.Add(new ToolStripMenuItem("Exit", null, (s, e) => this.Close()));

        var routeMenu = new ToolStripMenuItem("Operations");
        routeMenu.DropDownItems.Add(new ToolStripMenuItem("New Route", null, (s, e) => OpenRouteDialog()));
        routeMenu.DropDownItems.Add(new ToolStripMenuItem("Buy Aircraft", null, (s, e) => OpenAircraftDialog()));
        routeMenu.DropDownItems.Add(new ToolStripMenuItem("Hire Crew", null, (s, e) => OpenCrewDialog()));
        routeMenu.DropDownItems.Add(new ToolStripMenuItem("Take Loan", null, (s, e) => OpenLoanDialog()));

        menuStrip.Items.Add(gameMenu);
        menuStrip.Items.Add(routeMenu);
        this.MainMenuStrip = menuStrip;
        this.Controls.Add(menuStrip);

        // Status bar at bottom
        var statusPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 80,
            BackColor = Color.White,
            BorderStyle = BorderStyle.Fixed3D
        };

        _dayLabel = new Label { AutoSize = true, Location = new Point(10, 10), Text = "Day: 1" };
        _balanceLabel = new Label { AutoSize = true, Location = new Point(10, 30), Text = "Balance: €100,000" };
        _statusLabel = new Label { AutoSize = true, Location = new Point(10, 50), Text = "Status: Running" };

        _pauseButton = new Button { Text = "Pause", Location = new Point(800, 10), Width = 80, Height = 30 };
        _pauseButton.Click += (s, e) => TogglePause();

        _speedUpButton = new Button { Text = "Speed ↑", Location = new Point(900, 10), Width = 80, Height = 30 };
        _speedUpButton.Click += (s, e) => SpeedUp();

        _speedDownButton = new Button { Text = "Speed ↓", Location = new Point(1000, 10), Width = 80, Height = 30 };
        _speedDownButton.Click += (s, e) => SpeedDown();

        statusPanel.Controls.AddRange(new Control[] { _dayLabel, _balanceLabel, _statusLabel, _pauseButton, _speedUpButton, _speedDownButton });
        mainPanel.Controls.Add(statusPanel);

        // Map control
        _mapControl = new MapControl { Dock = DockStyle.Fill };
        mainPanel.Controls.Add(_mapControl);
    }

    private void SetupUI()
    {
        // Load initial data into map
        var allCities = CityDatabase.GetAllCities();
        _mapControl?.SetCities(allCities);
        _mapControl?.SetRoutes(_gameState.Routes);
        _mapControl?.SetFlights(_gameState.AllFlights);

        UpdateStatusDisplay();
    }

    private void StartGameLoop()
    {
        _timeManager.Reset();
        _gameLoopTimer = new Timer { Interval = 50 }; // 50ms per tick
        _gameLoopTimer.Tick += GameLoopTick;
        _gameLoopTimer.Start();
    }

    private void GameLoopTick(object? sender, EventArgs e)
    {
        if (!_gameState.IsGameRunning)
        {
            MessageBox.Show(_gameState.GameOverReason, "Game Over");
            _gameLoopTimer?.Stop();
            return;
        }

        // Update time and get number of ticks
        int ticks = _timeManager.UpdateTime(0.05); // 50ms passed

        // Process each game tick
        for (int i = 0; i < ticks; i++)
        {
            _simulationEngine.UpdateOneTick();
        }

        // Update UI
        UpdateStatusDisplay();
        _mapControl?.SetRoutes(_gameState.Routes);
        _mapControl?.SetFlights(_simulationEngine.GetActiveFlights());
    }

    private void UpdateStatusDisplay()
    {
        if (_dayLabel != null)
            _dayLabel.Text = $"Day: {_gameState.GameDay}";

        if (_balanceLabel != null)
            _balanceLabel.Text = $"Balance: {MathUtils.FormatCurrency(_gameState.Balance)} | Aircraft: {_gameState.Fleet.Count} | Routes: {_gameState.GetActiveRoutes().Count}";

        if (_statusLabel != null)
        {
            string status = _timeManager.IsPaused ? "PAUSED" : "RUNNING";
            _statusLabel.Text = $"Status: {status} (Speed: {_timeManager.SpeedMultiplier:F1}x)";
        }
    }

    private void TogglePause()
    {
        _timeManager.TogglePause();
        if (_pauseButton != null)
            _pauseButton.Text = _timeManager.IsPaused ? "Resume" : "Pause";
    }

    private void SpeedUp()
    {
        _timeManager.SetSpeed(_timeManager.SpeedMultiplier + 0.5f);
    }

    private void SpeedDown()
    {
        _timeManager.SetSpeed(_timeManager.SpeedMultiplier - 0.5f);
    }

    private void OpenRouteDialog()
    {
        var dialog = new RouteDialog(_gameState, _routeManager);
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            _mapControl?.SetRoutes(_gameState.Routes);
        }
    }

    private void OpenAircraftDialog()
    {
        var dialog = new AircraftDialog(_gameState, _aircraftManager, _financeManager);
        dialog.ShowDialog();
    }

    private void OpenCrewDialog()
    {
        var dialog = new CrewDialog(_gameState, _crewManager, _financeManager);
        dialog.ShowDialog();
    }

    private void OpenLoanDialog()
    {
        var dialog = new LoanDialog(_gameState, _loanManager, _financeManager);
        dialog.ShowDialog();
    }

    private void NewGame()
    {
        if (MessageBox.Show("Start a new game? Current progress will be lost.", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
        {
            _gameState = new GameState();
            _gameState.Initialize();
            _simulationEngine = new SimulationEngine(_gameState, _timeManager);
            _financeManager = new FinanceManager(_gameState);
            _routeManager = new RouteManager(_gameState, _financeManager);
            _timeManager.Reset();
            _timeManager.Resume();
            SetupUI();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _gameLoopTimer?.Stop();
            _gameLoopTimer?.Dispose();
        }
        base.Dispose(disposing);
    }
}
