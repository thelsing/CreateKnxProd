using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace CreateKnxProd
{
    /// <summary>
    /// Interaktionslogik für "App.xaml"
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var messageService = new DialogService();
            var mainWindowViewModel = new MainWindowViewModel(messageService);
            var mainWindow = new MainWindow() { DataContext = mainWindowViewModel };

            mainWindow.Show();
        }
    }
}
