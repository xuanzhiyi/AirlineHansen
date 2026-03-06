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

    // Timing
    private System.Diagnostics.Stopwatch _frameTimer = new();

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
        this.Text = "✈ Airline Tycoon - Empire Building Simulator";
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = Color.FromArgb(230, 240, 245);
        this.Font = new Font("Segoe UI", 10);

        // Main layout with menu bar
        var mainPanel = new Panel { Dock = DockStyle.Fill };
        this.Controls.Add(mainPanel);

        // Menu strip
        var menuStrip = new MenuStrip { BackColor = Color.FromArgb(40, 80, 140) };
        menuStrip.ForeColor = Color.White;

        var gameMenu = new ToolStripMenuItem("Game") { ForeColor = Color.White };
        gameMenu.DropDownItems.Add(new ToolStripMenuItem("New Game", null, (s, e) => NewGame()));
        gameMenu.DropDownItems.Add(new ToolStripSeparator());
        gameMenu.DropDownItems.Add(new ToolStripMenuItem("Exit", null, (s, e) => this.Close()));

        var routeMenu = new ToolStripMenuItem("Operations") { ForeColor = Color.White };
        routeMenu.DropDownItems.Add(new ToolStripMenuItem("📍 New Route", null, (s, e) => OpenRouteDialog()));
        routeMenu.DropDownItems.Add(new ToolStripMenuItem("✈ Buy Aircraft", null, (s, e) => OpenAircraftDialog()));
        routeMenu.DropDownItems.Add(new ToolStripMenuItem("👥 Hire Crew", null, (s, e) => OpenCrewDialog()));
        routeMenu.DropDownItems.Add(new ToolStripMenuItem("💰 Take Loan", null, (s, e) => OpenLoanDialog()));

        menuStrip.Items.Add(gameMenu);
        menuStrip.Items.Add(routeMenu);
        this.MainMenuStrip = menuStrip;
        this.Controls.Add(menuStrip);

        // Status bar at bottom
        var statusPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 90,
            BackColor = Color.FromArgb(50, 100, 160),
            BorderStyle = BorderStyle.FixedSingle
        };

        var dayLabelTitle = new Label { Text = "📅 Day", Location = new Point(15, 10), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.White };
        _dayLabel = new Label { Location = new Point(15, 28), Text = "1", AutoSize = true, Font = new Font("Segoe UI", 14, FontStyle.Bold), ForeColor = Color.FromArgb(100, 200, 255) };

        var balanceLabelTitle = new Label { Text = "💵 Balance", Location = new Point(150, 10), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.White };
        _balanceLabel = new Label { Location = new Point(150, 28), Text = "€100,000", AutoSize = true, Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = Color.FromArgb(100, 255, 100) };

        var statusLabelTitle = new Label { Text = "⚙ Status", Location = new Point(400, 10), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.White };
        _statusLabel = new Label { Location = new Point(400, 28), Text = "Running", AutoSize = true, Font = new Font("Segoe UI", 11, FontStyle.Bold), ForeColor = Color.Yellow };

        _pauseButton = new Button { Text = "⏸ Pause", Location = new Point(750, 12), Width = 90, Height = 35, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(100, 150, 200), ForeColor = Color.White, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
        _pauseButton.Click += (s, e) => TogglePause();

        _speedUpButton = new Button { Text = "⏩ Speed ↑", Location = new Point(850, 12), Width = 90, Height = 35, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(100, 150, 200), ForeColor = Color.White, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
        _speedUpButton.Click += (s, e) => SpeedUp();

        _speedDownButton = new Button { Text = "⏪ Speed ↓", Location = new Point(950, 12), Width = 90, Height = 35, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(100, 150, 200), ForeColor = Color.White, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
        _speedDownButton.Click += (s, e) => SpeedDown();

        statusPanel.Controls.AddRange(new Control[] { dayLabelTitle, _dayLabel, balanceLabelTitle, _balanceLabel, statusLabelTitle, _statusLabel, _pauseButton, _speedUpButton, _speedDownButton });
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
        _frameTimer.Start();
        _gameLoopTimer = new Timer { Interval = 16 }; // Target ~60 FPS (WinForms Timer precision varies)
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

        // Get actual elapsed time since last frame (WinForms Timer is not precise)
        double deltaSeconds = _frameTimer.Elapsed.TotalSeconds;
        _frameTimer.Restart();

        // Update time and get number of ticks
        int ticks = _timeManager.UpdateTime(deltaSeconds);

        // Process each game tick
        for (int i = 0; i < ticks; i++)
        {
            _simulationEngine.UpdateOneTick();
        }

        // Update UI
        UpdateStatusDisplay();
        _mapControl?.SetGameTime(_gameState.GameTime);
        _mapControl?.SetRoutes(_gameState.Routes);
        _mapControl?.SetFlights(_simulationEngine.GetActiveFlights());
        _mapControl?.Invalidate();
    }

    private void UpdateStatusDisplay()
    {
        if (_dayLabel != null)
        {
            int year = 2024 + (_gameState.GameDay / 365);
            int day = (_gameState.GameDay % 365) + 1;
            _dayLabel.Text = $"{_gameState.GameDay} | Year {year}";
        }

        if (_balanceLabel != null)
        {
            string balanceColor = _gameState.Balance >= 0 ? "✓" : "✗";
            _balanceLabel.Text = $"{balanceColor} {MathUtils.FormatCurrency(_gameState.Balance)}";
            _balanceLabel.ForeColor = _gameState.Balance >= 0 ? Color.FromArgb(100, 255, 100) : Color.FromArgb(255, 100, 100);
        }

        if (_statusLabel != null)
        {
            string status = _timeManager.IsPaused ? "⏸ PAUSED" : "▶ RUNNING";
            int aircraft = _gameState.Fleet.Count;
            int routes = _gameState.GetActiveRoutes().Count;
            _statusLabel.Text = $"{status} ({_timeManager.SpeedMultiplier:F1}x) | ✈{aircraft} routes {routes}";
            _statusLabel.ForeColor = _timeManager.IsPaused ? Color.FromArgb(255, 200, 100) : Color.Yellow;
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
