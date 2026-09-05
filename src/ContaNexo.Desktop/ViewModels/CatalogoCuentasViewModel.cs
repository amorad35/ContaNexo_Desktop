using System.Collections.ObjectModel;
using ContaNexo.Core.Models;
using ContaNexo.Data.Repositories;

namespace ContaNexo.Desktop.ViewModels;

public sealed class CatalogoCuentasViewModel : ViewModelBase
{
    private readonly RepositorioCuentaContable _repositorioCuentaContable;
    private readonly Dictionary<int, CuentaContableDetalle> _detallesCargados = new();
    private string _textoBusqueda = string.Empty;
    private OpcionFiltroElemento? _elementoSeleccionado;
    private OpcionFiltroBooleano? _estadoSeleccionado;
    private OpcionFiltroBooleano? _movimientoSeleccionado;
    private CuentaContableListado? _cuentaSeleccionada;
    private CuentaContableDetalle? _detalleSeleccionado;
    private bool _estaCargando;
    private bool _estaCargandoDetalle;
    private bool _catalogoCargado;
    private string _mensajeError = string.Empty;
    private string _mensajeErrorDetalle = string.Empty;
    private int _versionSolicitudDetalle;

    public CatalogoCuentasViewModel(RepositorioCuentaContable repositorioCuentaContable)
    {
        _repositorioCuentaContable = repositorioCuentaContable;

        Elementos.Add(new OpcionFiltroElemento(null, "Todos"));
        Estados.Add(new OpcionFiltroBooleano(null, "Todos"));
        Estados.Add(new OpcionFiltroBooleano(true, "Activas"));
        Estados.Add(new OpcionFiltroBooleano(false, "Inactivas"));
        Movimientos.Add(new OpcionFiltroBooleano(null, "Todos"));
        Movimientos.Add(new OpcionFiltroBooleano(true, "Permite movimiento"));
        Movimientos.Add(new OpcionFiltroBooleano(false, "No permite movimiento"));

        _elementoSeleccionado = Elementos[0];
        _estadoSeleccionado = Estados[0];
        _movimientoSeleccionado = Movimientos[0];
    }

    public ObservableCollection<CuentaContableListado> CuentasContables { get; } = new();

    public ObservableCollection<CuentaContableListado> CuentasFiltradas { get; } = new();

    public ObservableCollection<OpcionFiltroElemento> Elementos { get; } = new();

    public ObservableCollection<OpcionFiltroBooleano> Estados { get; } = new();

    public ObservableCollection<OpcionFiltroBooleano> Movimientos { get; } = new();

    public string TextoBusqueda
    {
        get => _textoBusqueda;
        set
        {
            if (EstablecerPropiedad(ref _textoBusqueda, value))
            {
                AplicarFiltros();
            }
        }
    }

    public OpcionFiltroElemento? ElementoSeleccionado
    {
        get => _elementoSeleccionado;
        set
        {
            if (EstablecerPropiedad(ref _elementoSeleccionado, value))
            {
                AplicarFiltros();
            }
        }
    }

    public OpcionFiltroBooleano? EstadoSeleccionado
    {
        get => _estadoSeleccionado;
        set
        {
            if (EstablecerPropiedad(ref _estadoSeleccionado, value))
            {
                AplicarFiltros();
            }
        }
    }

    public OpcionFiltroBooleano? MovimientoSeleccionado
    {
        get => _movimientoSeleccionado;
        set
        {
            if (EstablecerPropiedad(ref _movimientoSeleccionado, value))
            {
                AplicarFiltros();
            }
        }
    }

    public CuentaContableListado? CuentaSeleccionada
    {
        get => _cuentaSeleccionada;
        set
        {
            if (!EstablecerPropiedad(ref _cuentaSeleccionada, value))
            {
                return;
            }

            DetalleSeleccionado = null;
            MensajeErrorDetalle = string.Empty;
            NotificarEstadoDetalle();
            _ = CargarDetalleAsync(value);
        }
    }

    public CuentaContableDetalle? DetalleSeleccionado
    {
        get => _detalleSeleccionado;
        private set
        {
            if (EstablecerPropiedad(ref _detalleSeleccionado, value))
            {
                NotificarEstadoDetalle();
            }
        }
    }

    public bool EstaCargando
    {
        get => _estaCargando;
        private set
        {
            if (EstablecerPropiedad(ref _estaCargando, value))
            {
                NotificarEstadoListado();
                NotificarEstadoDetalle();
            }
        }
    }

    public bool EstaCargandoDetalle
    {
        get => _estaCargandoDetalle;
        private set
        {
            if (EstablecerPropiedad(ref _estaCargandoDetalle, value))
            {
                NotificarEstadoDetalle();
            }
        }
    }

    public string MensajeError
    {
        get => _mensajeError;
        private set
        {
            if (EstablecerPropiedad(ref _mensajeError, value))
            {
                NotificarEstadoListado();
                NotificarEstadoDetalle();
            }
        }
    }

    public string MensajeErrorDetalle
    {
        get => _mensajeErrorDetalle;
        private set
        {
            if (EstablecerPropiedad(ref _mensajeErrorDetalle, value))
            {
                NotificarEstadoDetalle();
            }
        }
    }

    public bool TieneError => !string.IsNullOrWhiteSpace(MensajeError);

    public bool TieneErrorDetalle => !string.IsNullOrWhiteSpace(MensajeErrorDetalle);

    public bool TieneCuentas => CuentasContables.Count > 0;

    public bool TieneCoincidencias => CuentasFiltradas.Count > 0;

    public bool MostrarEstadoVacio => _catalogoCargado && !TieneError && !TieneCuentas;

    public bool MostrarSinCoincidencias =>
        _catalogoCargado && !TieneError && TieneCuentas && !TieneCoincidencias;

    public bool MostrarListado =>
        _catalogoCargado && !TieneError && TieneCoincidencias;

    public bool FiltrosHabilitados =>
        _catalogoCargado && !EstaCargando && !TieneError && TieneCuentas;

    public string ResumenResultados =>
        CuentasFiltradas.Count == 1
            ? "1 cuenta encontrada"
            : $"{CuentasFiltradas.Count} cuentas encontradas";

    public bool MostrarEstadoSinSeleccion =>
        _catalogoCargado
        && !TieneError
        && CuentaSeleccionada is null;

    public bool MostrarDetalle =>
        DetalleSeleccionado is not null
        && !EstaCargandoDetalle
        && !TieneErrorDetalle;

    public bool TieneFichaEducativa =>
        DetalleSeleccionado is not null
        && (!string.IsNullOrWhiteSpace(DetalleSeleccionado.DescripcionDetalle)
            || !string.IsNullOrWhiteSpace(DetalleSeleccionado.DinamicaDebitoDetalle)
            || !string.IsNullOrWhiteSpace(DetalleSeleccionado.DinamicaCreditoDetalle));

    public bool TieneCuentaPadre => DetalleSeleccionado?.IdCuentaPadre is not null;

    public string CuentaPadreTexto => DetalleSeleccionado?.IdCuentaPadre is null
        ? string.Empty
        : $"{DetalleSeleccionado.CodigoCuentaPadre} — {DetalleSeleccionado.NombreCuentaPadre}";

    public string ElementoTexto => DetalleSeleccionado is null
        ? string.Empty
        : $"{DetalleSeleccionado.CodigoElemento} — {DetalleSeleccionado.NombreElemento}";

    public string GrupoTexto => DetalleSeleccionado is null
        ? string.Empty
        : $"{DetalleSeleccionado.CodigoGrupo} — {DetalleSeleccionado.NombreGrupo}";

    public string JerarquiaTexto => DetalleSeleccionado?.TieneHijas == true
        ? "Cuenta agrupadora con cuentas hijas registradas."
        : "Cuenta sin cuentas hijas registradas.";

    public async Task CargarAsync()
    {
        if (EstaCargando || _catalogoCargado)
        {
            return;
        }

        EstaCargando = true;
        MensajeError = string.Empty;

        try
        {
            List<CuentaContableListado> cuentas =
                (await _repositorioCuentaContable.ListarAsync()).ToList();

            CuentasContables.Clear();
            foreach (CuentaContableListado cuenta in cuentas)
            {
                CuentasContables.Add(cuenta);
            }

            CargarOpcionesElemento(cuentas);
            _catalogoCargado = true;
            AplicarFiltros();
        }
        catch (InvalidOperationException excepcion)
        {
            CuentasContables.Clear();
            CuentasFiltradas.Clear();
            MensajeError = excepcion.Message;
        }
        catch (Exception)
        {
            CuentasContables.Clear();
            CuentasFiltradas.Clear();
            MensajeError =
                "No se pudo cargar el catálogo de cuentas. Verifica la conexión e inténtalo nuevamente.";
        }
        finally
        {
            EstaCargando = false;
            NotificarEstadoListado();
        }
    }

    private void CargarOpcionesElemento(IEnumerable<CuentaContableListado> cuentas)
    {
        int? idSeleccionado = ElementoSeleccionado?.IdElementoContable;

        Elementos.Clear();
        Elementos.Add(new OpcionFiltroElemento(null, "Todos"));

        IEnumerable<OpcionFiltroElemento> opciones = cuentas
            .GroupBy(cuenta => cuenta.IdElementoContable)
            .Select(grupo => grupo.First())
            .OrderBy(cuenta => cuenta.OrdenCuenta)
            .Select(cuenta => new OpcionFiltroElemento(
                cuenta.IdElementoContable,
                cuenta.NombreElemento));

        foreach (OpcionFiltroElemento opcion in opciones)
        {
            Elementos.Add(opcion);
        }

        ElementoSeleccionado = Elementos.FirstOrDefault(
            opcion => opcion.IdElementoContable == idSeleccionado) ?? Elementos[0];
    }

    private void AplicarFiltros()
    {
        IEnumerable<CuentaContableListado> consulta = CuentasContables;
        string busqueda = TextoBusqueda.Trim();

        if (!string.IsNullOrEmpty(busqueda))
        {
            consulta = consulta.Where(cuenta =>
                cuenta.CodigoCuenta.Contains(busqueda, StringComparison.CurrentCultureIgnoreCase)
                || cuenta.NombreCuenta.Contains(busqueda, StringComparison.CurrentCultureIgnoreCase));
        }

        if (ElementoSeleccionado?.IdElementoContable is int idElementoContable)
        {
            consulta = consulta.Where(cuenta =>
                cuenta.IdElementoContable == idElementoContable);
        }

        if (EstadoSeleccionado?.Valor is bool estado)
        {
            consulta = consulta.Where(cuenta => cuenta.EstadoCuenta == estado);
        }

        if (MovimientoSeleccionado?.Valor is bool permiteMovimiento)
        {
            consulta = consulta.Where(cuenta =>
                cuenta.PermiteMovimientoCuenta == permiteMovimiento);
        }

        List<CuentaContableListado> resultados = consulta.ToList();

        CuentasFiltradas.Clear();
        foreach (CuentaContableListado cuenta in resultados)
        {
            CuentasFiltradas.Add(cuenta);
        }

        if (CuentaSeleccionada is not null
            && !resultados.Contains(CuentaSeleccionada))
        {
            CuentaSeleccionada = null;
        }

        NotificarEstadoListado();
    }

    private async Task CargarDetalleAsync(CuentaContableListado? cuenta)
    {
        int versionSolicitud = ++_versionSolicitudDetalle;

        if (cuenta is null)
        {
            EstaCargandoDetalle = false;
            return;
        }

        if (_detallesCargados.TryGetValue(cuenta.IdCuentaContable, out CuentaContableDetalle? detalle))
        {
            DetalleSeleccionado = detalle;
            EstaCargandoDetalle = false;
            return;
        }

        EstaCargandoDetalle = true;

        try
        {
            detalle = await _repositorioCuentaContable.ObtenerPorIdAsync(cuenta.IdCuentaContable);

            if (versionSolicitud != _versionSolicitudDetalle)
            {
                return;
            }

            if (detalle is null)
            {
                MensajeErrorDetalle =
                    "La cuenta seleccionada ya no está disponible en el catálogo.";
                return;
            }

            _detallesCargados[cuenta.IdCuentaContable] = detalle;
            DetalleSeleccionado = detalle;
        }
        catch (InvalidOperationException excepcion)
        {
            if (versionSolicitud == _versionSolicitudDetalle)
            {
                MensajeErrorDetalle = excepcion.Message;
            }
        }
        catch (Exception)
        {
            if (versionSolicitud == _versionSolicitudDetalle)
            {
                MensajeErrorDetalle =
                    "No se pudo consultar el detalle de la cuenta. Verifica la conexión e inténtalo nuevamente.";
            }
        }
        finally
        {
            if (versionSolicitud == _versionSolicitudDetalle)
            {
                EstaCargandoDetalle = false;
            }
        }
    }

    private void NotificarEstadoListado()
    {
        NotificarCambio(nameof(TieneError));
        NotificarCambio(nameof(TieneCuentas));
        NotificarCambio(nameof(TieneCoincidencias));
        NotificarCambio(nameof(MostrarEstadoVacio));
        NotificarCambio(nameof(MostrarSinCoincidencias));
        NotificarCambio(nameof(MostrarListado));
        NotificarCambio(nameof(FiltrosHabilitados));
        NotificarCambio(nameof(ResumenResultados));
    }

    private void NotificarEstadoDetalle()
    {
        NotificarCambio(nameof(TieneErrorDetalle));
        NotificarCambio(nameof(MostrarEstadoSinSeleccion));
        NotificarCambio(nameof(MostrarDetalle));
        NotificarCambio(nameof(TieneFichaEducativa));
        NotificarCambio(nameof(TieneCuentaPadre));
        NotificarCambio(nameof(CuentaPadreTexto));
        NotificarCambio(nameof(ElementoTexto));
        NotificarCambio(nameof(GrupoTexto));
        NotificarCambio(nameof(JerarquiaTexto));
    }
}

public sealed class OpcionFiltroElemento
{
    public OpcionFiltroElemento(int? idElementoContable, string nombre)
    {
        IdElementoContable = idElementoContable;
        Nombre = nombre;
    }

    public int? IdElementoContable { get; }

    public string Nombre { get; }
}

public sealed class OpcionFiltroBooleano
{
    public OpcionFiltroBooleano(bool? valor, string nombre)
    {
        Valor = valor;
        Nombre = nombre;
    }

    public bool? Valor { get; }

    public string Nombre { get; }
}
