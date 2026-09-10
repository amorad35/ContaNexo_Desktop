using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using ContaNexo.Desktop.ViewModels;

namespace ContaNexo.Desktop.Views
{
    public partial class LibroDiarioView : UserControl
    {
        private const double AnchoMinimoDosColumnas = 980d;

        private static readonly DependencyProperty IgnorarProximoCierreCuentaProperty =
            DependencyProperty.RegisterAttached(
                "IgnorarProximoCierreCuenta",
                typeof(bool),
                typeof(LibroDiarioView),
                new PropertyMetadata(false));

        public LibroDiarioView()
        {
            InitializeComponent();
            ActualizarDisposicionCaptura(ActualWidth);
        }

        private void LibroDiarioView_SizeChanged(
            object sender,
            SizeChangedEventArgs e)
        {
            ActualizarDisposicionCaptura(e.NewSize.Width);
        }

        private void ActualizarDisposicionCaptura(double anchoDisponible)
        {
            bool usarDosColumnas = anchoDisponible >= AnchoMinimoDosColumnas;

            CapturaAsientoGrid.ColumnDefinitions[0].Width = new GridLength(
                usarDosColumnas ? 3d : 1d,
                GridUnitType.Star);
            CapturaAsientoGrid.ColumnDefinitions[1].Width = new GridLength(
                usarDosColumnas ? 18d : 0d);
            CapturaAsientoGrid.ColumnDefinitions[2].Width = new GridLength(
                usarDosColumnas ? 1d : 0d,
                GridUnitType.Star);

            Grid.SetRow(FormularioAsientoPanel, 1);
            Grid.SetColumn(FormularioAsientoPanel, 0);
            Grid.SetColumnSpan(FormularioAsientoPanel, usarDosColumnas ? 1 : 3);

            Grid.SetRow(ResumenAsientoPanel, usarDosColumnas ? 1 : 2);
            Grid.SetColumn(ResumenAsientoPanel, usarDosColumnas ? 2 : 0);
            Grid.SetColumnSpan(ResumenAsientoPanel, usarDosColumnas ? 1 : 3);
            ResumenAsientoPanel.Margin = usarDosColumnas
                ? new Thickness(0)
                : new Thickness(0, 16, 0, 0);
        }

        private void CuentaSelector_KeyUp(object sender, KeyEventArgs e)
        {
            if (sender is not ComboBox selector)
            {
                return;
            }

            if (e.Key == Key.Escape)
            {
                selector.IsDropDownOpen = false;
                return;
            }

            if (e.Key is Key.Enter or Key.Tab or Key.Up or Key.Down)
            {
                return;
            }

            selector.GetBindingExpression(ComboBox.TextProperty)?.UpdateSource();
            selector.IsDropDownOpen = true;
        }

        private void CuentaSelector_DropDownClosed(
            object sender,
            EventArgs e)
        {
            if (sender is not ComboBox selector)
            {
                return;
            }

            if ((bool)selector.GetValue(IgnorarProximoCierreCuentaProperty))
            {
                selector.ClearValue(IgnorarProximoCierreCuentaProperty);
                return;
            }

            ConsolidarSeleccionCuenta(selector);
        }

        private void LibroDiarioView_PreviewKeyDown(
            object sender,
            KeyEventArgs e)
        {
            ComboBox? selector = EncontrarSelectorCuenta(e.OriginalSource);

            if (selector is null
                || !selector.IsDropDownOpen
                || !ProcesarTeclaNavegacionCuenta(selector, e.Key))
            {
                return;
            }

            e.Handled = true;
        }

        private static bool ProcesarTeclaNavegacionCuenta(
            ComboBox selector,
            Key tecla)
        {
            switch (tecla)
            {
                case Key.Down:
                    if (selector.Items.Count > 0
                        && selector.SelectedIndex < selector.Items.Count - 1)
                    {
                        selector.SelectedIndex++;
                        RestaurarTextoBusqueda(selector);
                        MostrarSeleccionActual(selector);
                    }

                    return true;

                case Key.Up:
                    if (selector.Items.Count > 0)
                    {
                        selector.SelectedIndex = selector.SelectedIndex <= 0
                            ? 0
                            : selector.SelectedIndex - 1;
                        RestaurarTextoBusqueda(selector);
                        MostrarSeleccionActual(selector);
                    }

                    return true;

                case Key.Enter:
                    ConsolidarSeleccionCuenta(selector);
                    CerrarSinReconsolidar(selector);
                    return true;

                case Key.Escape:
                    selector.GetBindingExpression(ComboBox.SelectedItemProperty)
                        ?.UpdateTarget();
                    RestaurarTextoBusqueda(selector);
                    CerrarSinReconsolidar(selector);
                    return true;

                default:
                    return false;
            }
        }

        private static void ConsolidarSeleccionCuenta(ComboBox selector)
        {
            selector.GetBindingExpression(ComboBox.SelectedItemProperty)
                ?.UpdateSource();
            selector.GetBindingExpression(ComboBox.TextProperty)?.UpdateTarget();
        }

        private static void RestaurarTextoBusqueda(ComboBox selector)
        {
            selector.GetBindingExpression(ComboBox.TextProperty)?.UpdateTarget();
        }

        private static void MostrarSeleccionActual(ComboBox selector)
        {
            if (selector.ItemContainerGenerator.ContainerFromIndex(
                    selector.SelectedIndex) is ComboBoxItem elemento)
            {
                elemento.BringIntoView();
            }
        }

        private static void CerrarSinReconsolidar(ComboBox selector)
        {
            selector.SetValue(IgnorarProximoCierreCuentaProperty, true);
            selector.IsDropDownOpen = false;
        }

        private static ComboBox? EncontrarSelectorCuenta(object? origen)
        {
            DependencyObject? actual = origen as DependencyObject;

            while (actual is not null)
            {
                if (actual is ComboBox selector
                    && selector.IsEditable
                    && selector.DataContext is LineaAsientoViewModel)
                {
                    return selector;
                }

                actual = VisualTreeHelper.GetParent(actual)
                    ?? LogicalTreeHelper.GetParent(actual);
            }

            return null;
        }
    }
}
