using System.Configuration;
using System.Data;
using System.Windows;
using QuestPDF.Infrastructure;

namespace ContaNexo.Desktop
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }
    }

}
