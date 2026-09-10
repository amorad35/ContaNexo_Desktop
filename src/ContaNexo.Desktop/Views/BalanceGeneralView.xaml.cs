using System.Windows;
using System.Windows.Controls;

namespace ContaNexo.Desktop.Views;

public partial class BalanceGeneralView : UserControl
{
    private const double AnchoMinimoDosColumnas = 980d;

    public BalanceGeneralView()
    {
        InitializeComponent();
        ActualizarDisposicion(ActualWidth);
    }

    private void BalanceGeneralView_SizeChanged(
        object sender,
        SizeChangedEventArgs eventArgs)
    {
        ActualizarDisposicion(eventArgs.NewSize.Width);
    }

    private void ActualizarDisposicion(double anchoDisponible)
    {
        bool usarDosColumnas = anchoDisponible >= AnchoMinimoDosColumnas;

        SeccionesBalanceGrid.ColumnDefinitions[0].Width = new GridLength(1d, GridUnitType.Star);
        SeccionesBalanceGrid.ColumnDefinitions[1].Width = new GridLength(
            usarDosColumnas ? 24d : 0d);
        SeccionesBalanceGrid.ColumnDefinitions[2].Width = new GridLength(1d, GridUnitType.Star);

        MetricasComprobacionGrid.Columns = usarDosColumnas ? 4 : 2;
        MetricasComprobacionGrid.Rows = usarDosColumnas ? 1 : 2;

        Grid.SetRow(ActivoPanel, 0);
        Grid.SetColumn(ActivoPanel, 0);
        Grid.SetColumnSpan(ActivoPanel, usarDosColumnas ? 1 : 3);

        Grid.SetRow(PasivoPatrimonioPanel, usarDosColumnas ? 0 : 1);
        Grid.SetColumn(PasivoPatrimonioPanel, usarDosColumnas ? 2 : 0);
        Grid.SetColumnSpan(PasivoPatrimonioPanel, usarDosColumnas ? 1 : 3);
    }
}
