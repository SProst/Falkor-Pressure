using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using FalkorPressure.Properties;
using StackExchange.Redis;

namespace FalkorPressure
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            if (string.IsNullOrEmpty(Settings.Default.LogPath))
            {
              Settings.Default.LogPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            }
           
            
            MainWindow = new MainWindow();
            MainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
        }
    }
}
