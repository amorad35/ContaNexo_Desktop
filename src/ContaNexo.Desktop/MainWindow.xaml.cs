using System.Text;
using System.Windows;
using ContaNexo.Data.Connections;
using ContaNexo.Data.Repositories;
using ContaNexo.Desktop.ViewModels;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ContaNexo.Desktop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const double MargenAreaTrabajo = 16;

        public MainWindow()
        {
            InitializeComponent();
            var conexionBD = new ConexionBD(ConfiguracionBD.CadenaConexion);
            var repositorioEmpresa = new RepositorioEmpresa(conexionBD);
            DataContext = new MainWindowViewModel(repositorioEmpresa);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            Rect areaTrabajo = SystemParameters.WorkArea;
            double anchoMaximo = Math.Max(1, areaTrabajo.Width - (MargenAreaTrabajo * 2));
            double altoMaximo = Math.Max(1, areaTrabajo.Height - (MargenAreaTrabajo * 2));

            MinWidth = Math.Min(MinWidth, anchoMaximo);
            MinHeight = Math.Min(MinHeight, altoMaximo);
            Width = Math.Min(Width, anchoMaximo);
            Height = Math.Min(Height, altoMaximo);
            Left = areaTrabajo.Left + ((areaTrabajo.Width - Width) / 2);
            Top = areaTrabajo.Top + ((areaTrabajo.Height - Height) / 2);
        }
    }
}
