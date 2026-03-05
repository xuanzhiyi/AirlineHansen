using AirlineHansen.Data;
using AirlineHansen.Forms;

namespace AirlineHansen;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        // Initialize city database
        CityDatabase.Initialize();

        // Enable Windows Forms high DPI support
        ApplicationConfiguration.Initialize();

        // Run main form
        Application.Run(new MainForm());
    }
}
