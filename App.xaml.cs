using CreateKnxProd.Properties;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using WPFLocalizeExtension.Engine;

namespace CreateKnxProd
{
    /// <summary>
    /// Interaktionslogik für "App.xaml"
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            InitialiseCultures();
        }

        private static void InitialiseCultures()
        {
            if (!string.IsNullOrEmpty(Settings.Default.Culture))
            {
                LocalizeDictionary.Instance.Culture
                    = Thread.CurrentThread.CurrentCulture
                    = new CultureInfo(Settings.Default.Culture);
            }

            if (!string.IsNullOrEmpty(Settings.Default.UICulture))
            {
                LocalizeDictionary.Instance.Culture
                    = Thread.CurrentThread.CurrentUICulture
                    = new CultureInfo(Settings.Default.UICulture);
            }

            FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement), new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.Name)));
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var messageService = new DialogService();
            var mainWindowViewModel = new MainWindowViewModel(messageService);
            var mainWindow = new MainWindow() { DataContext = mainWindowViewModel };

            mainWindow.Show();
        }
    }
}
