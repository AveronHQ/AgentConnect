using System;
using System.Windows;
using Velopack;

namespace AgentConnect.Core
{
    /// <summary>
    /// Application entry point with Velopack bootstrap for auto-updates.
    /// This must run before any WPF code to handle install/uninstall/update hooks.
    /// </summary>
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            // Initialize Velopack as early as possible
            // This handles install/uninstall/update hooks before the app starts
            VelopackApp.Build().Run();

            // Start WPF application
            var app = new App();
            app.InitializeComponent();
            app.Run();
        }
    }
}
