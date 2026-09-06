using System.Collections.ObjectModel;
using System.Windows;
using ContaNexo.Core.Models;
using ContaNexo.Data.Repositories;

namespace ContaNexo.Desktop.ViewModels;

public sealed class CatalogoCuentasViewModel : ViewModelBase
{
    private readonly RepositorioCuentaContable _repositorioCuentaContable;
    private readonly Dictionary<int, CuentaContableDetalle> _detallesCargados = new();
    private readonly ComandoRelay _nuevaCuentaCommand;
    private readonly ComandoRelay _cancelarCreacionCommand;
    private readonly ComandoAsync _guardarCuentaCommand;
    private readonly ComandoAsync _editarCuentaCommand;
    private readonly ComandoAsync _cambiarEstadoCommand;
    private readonly ComandoAsync _configurarMovimientoCommand;
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
    private bool _mostrarFormulario;
    private bool _estaGuardando;
    private bool _estaPreparandoEdicion;
    private bool _estaActualizandoCuenta;
    private bool _estaCargandoDatosFormulario;
    private int? _idCuentaEdicion;
    private int? _idCuentaPadreOriginal;
    private int _ordenCuentaEdicion;
    private string _codigoCuentaOriginal = string.Empty;
    private bool _permiteMovimientoOriginal;
    private bool _estadoCuentaOriginal;
    private string _codigoCuenta = string.Empty;
    private string _nombreCuenta = string.Empty;
    private OpcionGrupoContable? _grupoCreacionSeleccionado;
    private OpcionTipoCuenta? _tipoCuentaSeleccionado;
    private OpcionCuentaPadre? _cuentaPadreSeleccionada;
    private string? _naturalezaSeleccionada;
    private bool _naturalezaAsignadaDesdePadre;
    private bool _permiteMovimiento = true;
    private string _mensajeErrorFormulario = string.Empty;
    private string _mensajeExito = string.Empty;
    private int _versionMensajeExito;

    public CatalogoCuentasViewModel(RepositorioCuentaContable repositorioCuentaContable)
    {
        _repositorioCuentaContable = repositorioCuentaContable;
        _nuevaCuentaCommand = new ComandoRelay(AbrirFormulario, PuedeAbrirFormulario);
        _cancelarCreacionCommand = new ComandoRelay(CancelarCreacion, PuedeCancelarCreacion);
        _guardarCuentaCommand = new ComandoAsync(GuardarCuentaAsync, PuedeGuardarCuenta);
        _editarCuentaCommand = new ComandoAsync(EditarCuentaAsync, PuedeEditarCuenta);
        _cambiarEstadoCommand = new ComandoAsync(CambiarEstadoCuentaAsync, PuedeAdministrarCuenta);
        _configurarMovimientoCommand = new ComandoAsync(ConfigurarMovimientoCuentaAsync, PuedeAdministrarCuenta);

        Elementos.Add(new OpcionFiltroElemento(null, "Todos"));
        Estados.Add(new OpcionFiltroBooleano(null, "Todos"));
        Estados.Add(new OpcionFiltroBooleano(true, "Activas"));
        Estados.Add(new OpcionFiltroBooleano(false, "Inactivas"));
        Movimientos.Add(new OpcionFiltroBooleano(null, "Todos"));
        Movimientos.Add(new OpcionFiltroBooleano(true, "Permite movimiento"));
        Movimientos.Add(new OpcionFiltroBooleano(false, "No permite movimiento"));
        TiposCuenta.Add(new OpcionTipoCuenta(false, "Cuenta principal"));
        TiposCuenta.Add(new OpcionTipoCuenta(true, "Subcuenta"));
        Naturalezas.Add("Deudora");
        Naturalezas.Add("Acreedora");
        _elementoSeleccionado = Elementos[0];
        _estadoSeleccionado = Estados[0];
        _movimientoSeleccionado = Movimientos[0];
        _tipoCuentaSeleccionado = TiposCuenta[0];
    }

    public ObservableCollection<CuentaContableListado> CuentasContables { get; } = new();
    public ObservableCollection<CuentaContableListado> CuentasFiltradas { get; } = new();
    public ObservableCollection<OpcionFiltroElemento> Elementos { get; } = new();
    public ObservableCollection<OpcionFiltroBooleano> Estados { get; } = new();
    public ObservableCollection<OpcionFiltroBooleano> Movimientos { get; } = new();
    public ObservableCollection<OpcionGrupoContable> GruposCreacion { get; } = new();
    public ObservableCollection<OpcionTipoCuenta> TiposCuenta { get; } = new();
    public ObservableCollection<OpcionCuentaPadre> CuentasPadre { get; } = new();
    public ObservableCollection<string> Naturalezas { get; } = new();

    public string TextoBusqueda
    {
        get => _textoBusqueda;
        set { if (EstablecerPropiedad(ref _textoBusqueda, value)) AplicarFiltros(); }
    }

    public OpcionFiltroElemento? ElementoSeleccionado
    {
        get => _elementoSeleccionado;
        set { if (EstablecerPropiedad(ref _elementoSeleccionado, value)) AplicarFiltros(); }
    }

    public OpcionFiltroBooleano? EstadoSeleccionado
    {
        get => _estadoSeleccionado;
        set { if (EstablecerPropiedad(ref _estadoSeleccionado, value)) AplicarFiltros(); }
    }

    public OpcionFiltroBooleano? MovimientoSeleccionado
    {
        get => _movimientoSeleccionado;
        set { if (EstablecerPropiedad(ref _movimientoSeleccionado, value)) AplicarFiltros(); }
    }

    public CuentaContableListado? CuentaSeleccionada
    {
        get => _cuentaSeleccionada;
        set
        {
            if (!EstablecerPropiedad(ref _cuentaSeleccionada, value)) return;
            DetalleSeleccionado = null;
            MensajeErrorDetalle = string.Empty;
            NotificarEstadoDetalle();
            NotificarAccionesCuentaSeleccionada();
            _ = CargarDetalleAsync(value);
        }
    }

    public CuentaContableDetalle? DetalleSeleccionado
    {
        get => _detalleSeleccionado;
        private set { if (EstablecerPropiedad(ref _detalleSeleccionado, value)) NotificarEstadoDetalle(); }
    }

    public bool EstaCargando
    {
        get => _estaCargando;
        private set
        {
            if (!EstablecerPropiedad(ref _estaCargando, value)) return;
            NotificarEstadoListado();
            NotificarEstadoDetalle();
            NotificarEstadoFormulario();
        }
    }

    public bool EstaCargandoDetalle
    {
        get => _estaCargandoDetalle;
        private set { if (EstablecerPropiedad(ref _estaCargandoDetalle, value)) NotificarEstadoDetalle(); }
    }

    public string MensajeError
    {
        get => _mensajeError;
        private set
        {
            if (!EstablecerPropiedad(ref _mensajeError, value)) return;
            NotificarEstadoListado();
            NotificarEstadoDetalle();
            NotificarEstadoFormulario();
        }
    }

    public string MensajeErrorDetalle
    {
        get => _mensajeErrorDetalle;
        private set { if (EstablecerPropiedad(ref _mensajeErrorDetalle, value)) NotificarEstadoDetalle(); }
    }

    public bool MostrarFormulario
    {
        get => _mostrarFormulario;
        private set { if (EstablecerPropiedad(ref _mostrarFormulario, value)) NotificarEstadoFormulario(); }
    }

    public bool EstaGuardando
    {
        get => _estaGuardando;
        private set
        {
            if (!EstablecerPropiedad(ref _estaGuardando, value)) return;
            NotificarCambio(nameof(FormularioHabilitado));
            NotificarCambio(nameof(TextoGuardar));
            NotificarCambio(nameof(FiltrosHabilitados));
            NotificarEstadoFormulario();
        }
    }

    public bool EstaPreparandoEdicion
    {
        get => _estaPreparandoEdicion;
        private set
        {
            if (!EstablecerPropiedad(ref _estaPreparandoEdicion, value)) return;
            NotificarCambio(nameof(FormularioHabilitado));
            NotificarEstadoFormulario();
        }
    }

    public bool EstaActualizandoCuenta
    {
        get => _estaActualizandoCuenta;
        private set
        {
            if (!EstablecerPropiedad(ref _estaActualizandoCuenta, value)) return;
            NotificarCambio(nameof(FiltrosHabilitados));
            NotificarEstadoFormulario();
            NotificarAccionesCuentaSeleccionada();
        }
    }

    public string CodigoCuenta
    {
        get => _codigoCuenta;
        set { if (EstablecerPropiedad(ref _codigoCuenta, value)) LimpiarErrorFormulario(); }
    }

    public string NombreCuenta
    {
        get => _nombreCuenta;
        set { if (EstablecerPropiedad(ref _nombreCuenta, value)) LimpiarErrorFormulario(); }
    }

    public OpcionGrupoContable? GrupoCreacionSeleccionado
    {
        get => _grupoCreacionSeleccionado;
        set
        {
            if (!EstablecerPropiedad(ref _grupoCreacionSeleccionado, value)) return;
            NotificarCambio(nameof(ElementoCreacionTexto));
            NotificarCambio(nameof(PuedeSeleccionarCuentaPadre));
            CargarCuentasPadre();
            LimpiarErrorFormulario();
        }
    }

    public OpcionTipoCuenta? TipoCuentaSeleccionado
    {
        get => _tipoCuentaSeleccionado;
        set
        {
            bool eraSubcuenta = EsSubcuenta;
            if (!EstablecerPropiedad(ref _tipoCuentaSeleccionado, value)) return;

            if (_estaCargandoDatosFormulario)
            {
                NotificarCambio(nameof(EsSubcuenta));
                NotificarCambio(nameof(PuedeSeleccionarCuentaPadre));
                return;
            }

            string codigoAnterior = CodigoCuenta;
            CuentaPadreSeleccionada = null;

            if (value?.EsSubcuenta == true)
            {
                CodigoCuenta = string.Empty;
            }
            else if (eraSubcuenta && EstaEditando)
            {
                CodigoCuenta = _codigoCuentaOriginal;
            }
            else
            {
                CodigoCuenta = eraSubcuenta ? string.Empty : codigoAnterior;
            }

            NotificarCambio(nameof(EsSubcuenta));
            NotificarCambio(nameof(PuedeSeleccionarCuentaPadre));
            LimpiarErrorFormulario();
        }
    }

    public OpcionCuentaPadre? CuentaPadreSeleccionada
    {
        get => _cuentaPadreSeleccionada;
        set
        {
            if (!EstablecerPropiedad(ref _cuentaPadreSeleccionada, value)) return;

            if (_estaCargandoDatosFormulario) return;

            LimpiarErrorFormulario();

            if (value is null)
            {
                CodigoCuenta = string.Empty;
                if (_naturalezaAsignadaDesdePadre)
                    EstablecerNaturalezaDesdePadre(null);
            }
            else
            {
                if (EstaEditando && value.IdCuentaContable == _idCuentaPadreOriginal)
                    CodigoCuenta = _codigoCuentaOriginal;
                else
                    GenerarCodigoSubcuenta(value, _idCuentaEdicion);

                if (NaturalezaSeleccionada is null || _naturalezaAsignadaDesdePadre)
                    EstablecerNaturalezaDesdePadre(value.NaturalezaCuenta);
            }
        }
    }

    public string? NaturalezaSeleccionada
    {
        get => _naturalezaSeleccionada;
        set
        {
            _naturalezaAsignadaDesdePadre = false;
            if (EstablecerPropiedad(ref _naturalezaSeleccionada, value)) LimpiarErrorFormulario();
        }
    }

    public bool PermiteMovimiento
    {
        get => _permiteMovimiento;
        set { if (EstablecerPropiedad(ref _permiteMovimiento, value)) LimpiarErrorFormulario(); }
    }

    public string MensajeErrorFormulario
    {
        get => _mensajeErrorFormulario;
        private set => EstablecerPropiedad(ref _mensajeErrorFormulario, value);
    }

    public string MensajeExito
    {
        get => _mensajeExito;
        private set => EstablecerPropiedad(ref _mensajeExito, value);
    }

    public bool TieneError => !string.IsNullOrWhiteSpace(MensajeError);
    public bool TieneErrorDetalle => !string.IsNullOrWhiteSpace(MensajeErrorDetalle);
    public bool TieneCuentas => CuentasContables.Count > 0;
    public bool TieneCoincidencias => CuentasFiltradas.Count > 0;
    public bool MostrarEstadoVacio => _catalogoCargado && !TieneError && !TieneCuentas;
    public bool MostrarSinCoincidencias => _catalogoCargado && !TieneError && TieneCuentas && !TieneCoincidencias;
    public bool MostrarListado => _catalogoCargado && !TieneError && TieneCoincidencias;
    public bool FiltrosHabilitados => _catalogoCargado && !EstaCargando && !TieneError
        && TieneCuentas && !EstaGuardando && !EstaActualizandoCuenta;
    public string ResumenResultados => CuentasFiltradas.Count == 1 ? "1 cuenta encontrada" : $"{CuentasFiltradas.Count} cuentas encontradas";
    public bool MostrarEstadoSinSeleccion => _catalogoCargado && !TieneError && CuentaSeleccionada is null;
    public bool MostrarDetalle => DetalleSeleccionado is not null && !EstaCargandoDetalle && !TieneErrorDetalle;
    public bool TieneFichaEducativa => DetalleSeleccionado is not null
        && (!string.IsNullOrWhiteSpace(DetalleSeleccionado.DescripcionDetalle)
            || !string.IsNullOrWhiteSpace(DetalleSeleccionado.DinamicaDebitoDetalle)
            || !string.IsNullOrWhiteSpace(DetalleSeleccionado.DinamicaCreditoDetalle));
    public bool TieneCuentaPadre => DetalleSeleccionado?.IdCuentaPadre is not null;
    public string CuentaPadreTexto => DetalleSeleccionado?.IdCuentaPadre is null ? string.Empty : $"{DetalleSeleccionado.CodigoCuentaPadre} — {DetalleSeleccionado.NombreCuentaPadre}";
    public string ElementoTexto => DetalleSeleccionado is null ? string.Empty : $"{DetalleSeleccionado.CodigoElemento} — {DetalleSeleccionado.NombreElemento}";
    public string GrupoTexto => DetalleSeleccionado is null ? string.Empty : $"{DetalleSeleccionado.CodigoGrupo} — {DetalleSeleccionado.NombreGrupo}";
    public string JerarquiaTexto => DetalleSeleccionado?.TieneHijas == true ? "Cuenta agrupadora con cuentas hijas registradas." : "Cuenta sin cuentas hijas registradas.";
    public bool FormularioHabilitado => !EstaGuardando && !EstaPreparandoEdicion;
    public bool EstaEditando => _idCuentaEdicion.HasValue;
    public bool EsSubcuenta => TipoCuentaSeleccionado?.EsSubcuenta == true;
    public bool PuedeSeleccionarCuentaPadre => EsSubcuenta && GrupoCreacionSeleccionado is not null;
    public bool PuedeEditarMovimientoEnFormulario => !EstaEditando;
    public string ElementoCreacionTexto => GrupoCreacionSeleccionado is null ? "Se deriva del grupo" : $"{GrupoCreacionSeleccionado.CodigoElemento} — {GrupoCreacionSeleccionado.NombreElemento}";
    public string TituloFormulario => EstaEditando ? "Editar cuenta contable" : "Nueva cuenta contable";
    public string DescripcionFormulario => EstaEditando
        ? "Actualiza la identificación y clasificación. Estado y movimiento no se modifican en este bloque."
        : "La cuenta se guardará activa. El elemento se deriva del grupo seleccionado.";
    public string TextoGuardar => EstaGuardando ? "Guardando..." : EstaEditando ? "Guardar cambios" : "Guardar";
    public string TextoAccionEstado => CuentaSeleccionada?.EstadoCuenta == true
        ? "Desactivar"
        : "Activar";
    public string TextoAccionMovimiento => CuentaSeleccionada?.PermiteMovimientoCuenta == true
        ? "No permitir movimiento"
        : "Permitir movimiento";
    public ComandoRelay NuevaCuentaCommand => _nuevaCuentaCommand;
    public ComandoRelay CancelarCreacionCommand => _cancelarCreacionCommand;
    public ComandoAsync GuardarCuentaCommand => _guardarCuentaCommand;
    public ComandoAsync EditarCuentaCommand => _editarCuentaCommand;
    public ComandoAsync CambiarEstadoCommand => _cambiarEstadoCommand;
    public ComandoAsync ConfigurarMovimientoCommand => _configurarMovimientoCommand;

    public async Task CargarAsync()
    {
        if (EstaCargando || _catalogoCargado) return;
        await CargarCatalogoAsync(null);
    }

    private async Task<bool> CargarCatalogoAsync(int? idCuentaAConservar)
    {
        if (EstaCargando) return false;
        EstaCargando = true;
        MensajeError = string.Empty;

        try
        {
            List<CuentaContableListado> cuentas = (await _repositorioCuentaContable.ListarAsync()).ToList();
            List<CuentaContableListado> cuentasOrdenadas = OrdenarJerarquicamente(cuentas);
            CuentaSeleccionada = null;
            _detallesCargados.Clear();
            CuentasContables.Clear();
            foreach (CuentaContableListado cuenta in cuentasOrdenadas) CuentasContables.Add(cuenta);
            CargarOpcionesElemento(cuentas);
            CargarOpcionesGrupo(cuentas);
            _catalogoCargado = true;
            AplicarFiltros();

            if (idCuentaAConservar is int idCuenta)
                CuentaSeleccionada = CuentasFiltradas.FirstOrDefault(cuenta => cuenta.IdCuentaContable == idCuenta);
            return true;
        }
        catch (InvalidOperationException excepcion)
        {
            CuentasContables.Clear();
            CuentasFiltradas.Clear();
            MensajeError = excepcion.Message;
            return false;
        }
        catch (Exception)
        {
            CuentasContables.Clear();
            CuentasFiltradas.Clear();
            MensajeError = "No se pudo cargar el catálogo de cuentas. Verifica la conexión e inténtalo nuevamente.";
            return false;
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
        foreach (OpcionFiltroElemento opcion in cuentas.GroupBy(c => c.IdElementoContable).Select(g => g.First())
                     .OrderBy(c => c.CodigoElemento).Select(c => new OpcionFiltroElemento(c.IdElementoContable, c.NombreElemento)))
            Elementos.Add(opcion);
        ElementoSeleccionado = Elementos.FirstOrDefault(o => o.IdElementoContable == idSeleccionado) ?? Elementos[0];
    }

    private void CargarOpcionesGrupo(IEnumerable<CuentaContableListado> cuentas)
    {
        int? idSeleccionado = GrupoCreacionSeleccionado?.IdGrupoContable;
        GruposCreacion.Clear();
        foreach (OpcionGrupoContable opcion in cuentas.GroupBy(c => c.IdGrupoContable).Select(g => g.First())
                     .OrderBy(c => c.CodigoGrupo).Select(c => new OpcionGrupoContable(c.IdGrupoContable, c.CodigoGrupo,
                         c.NombreGrupo, c.IdElementoContable, c.CodigoElemento, c.NombreElemento)))
            GruposCreacion.Add(opcion);
        GrupoCreacionSeleccionado = GruposCreacion.FirstOrDefault(o => o.IdGrupoContable == idSeleccionado);
        NotificarEstadoFormulario();
    }

    private void CargarCuentasPadre()
    {
        int? idGrupo = GrupoCreacionSeleccionado?.IdGrupoContable;
        CuentaPadreSeleccionada = null;
        CuentasPadre.Clear();
        HashSet<int> idsNoPermitidos = _idCuentaEdicion is int idCuenta
            ? ObtenerIdsNoPermitidosComoPadre(idCuenta)
            : new HashSet<int>();

        if (idGrupo is int valor)
            foreach (OpcionCuentaPadre opcion in CuentasContables
                         .Where(c => c.IdGrupoContable == valor && !idsNoPermitidos.Contains(c.IdCuentaContable))
                         .OrderBy(c => c.OrdenCuenta).ThenBy(c => c.CodigoCuenta).Select(c => new OpcionCuentaPadre(c)))
                CuentasPadre.Add(opcion);
    }

    private HashSet<int> ObtenerIdsNoPermitidosComoPadre(int idCuenta)
    {
        var idsNoPermitidos = new HashSet<int> { idCuenta };
        var pendientes = new Stack<int>();
        pendientes.Push(idCuenta);

        while (pendientes.Count > 0)
        {
            int idPadre = pendientes.Pop();
            foreach (CuentaContableListado hija in CuentasContables.Where(c => c.IdCuentaPadre == idPadre))
            {
                if (idsNoPermitidos.Add(hija.IdCuentaContable))
                    pendientes.Push(hija.IdCuentaContable);
            }
        }

        return idsNoPermitidos;
    }

    private static List<CuentaContableListado> OrdenarJerarquicamente(
        IReadOnlyCollection<CuentaContableListado> cuentas)
    {
        var hijasPorPadre = cuentas
            .Where(cuenta => cuenta.IdCuentaPadre.HasValue)
            .GroupBy(cuenta => cuenta.IdCuentaPadre!.Value)
            .ToDictionary(
                grupo => grupo.Key,
                grupo => grupo
                    .OrderBy(cuenta => cuenta.OrdenCuenta)
                    .ThenBy(cuenta => cuenta.CodigoCuenta)
                    .ToList());

        var resultado = new List<CuentaContableListado>(cuentas.Count);
        var visitadas = new HashSet<int>();

        void AgregarFamilia(CuentaContableListado cuenta)
        {
            if (!visitadas.Add(cuenta.IdCuentaContable)) return;

            resultado.Add(cuenta);
            if (!hijasPorPadre.TryGetValue(cuenta.IdCuentaContable, out List<CuentaContableListado>? hijas))
                return;

            foreach (CuentaContableListado hija in hijas)
                AgregarFamilia(hija);
        }

        IEnumerable<CuentaContableListado> cuentasPrincipales = cuentas
            .Where(cuenta => !cuenta.IdCuentaPadre.HasValue)
            .OrderBy(cuenta => cuenta.OrdenCuenta)
            .ThenBy(cuenta => cuenta.CodigoCuenta);

        foreach (CuentaContableListado cuentaPrincipal in cuentasPrincipales)
            AgregarFamilia(cuentaPrincipal);

        IEnumerable<CuentaContableListado> cuentasAnomalas = cuentas
            .Where(cuenta => !visitadas.Contains(cuenta.IdCuentaContable))
            .OrderBy(cuenta => cuenta.OrdenCuenta)
            .ThenBy(cuenta => cuenta.CodigoCuenta);

        foreach (CuentaContableListado cuentaAnomala in cuentasAnomalas)
            AgregarFamilia(cuentaAnomala);

        return resultado;
    }

    private void AplicarFiltros()
    {
        IEnumerable<CuentaContableListado> consulta = CuentasContables;
        string busqueda = TextoBusqueda.Trim();
        if (!string.IsNullOrEmpty(busqueda))
            consulta = consulta.Where(c => c.CodigoCuenta.Contains(busqueda, StringComparison.CurrentCultureIgnoreCase)
                || c.NombreCuenta.Contains(busqueda, StringComparison.CurrentCultureIgnoreCase));
        if (ElementoSeleccionado?.IdElementoContable is int idElemento) consulta = consulta.Where(c => c.IdElementoContable == idElemento);
        if (EstadoSeleccionado?.Valor is bool estado) consulta = consulta.Where(c => c.EstadoCuenta == estado);
        if (MovimientoSeleccionado?.Valor is bool movimiento) consulta = consulta.Where(c => c.PermiteMovimientoCuenta == movimiento);

        List<CuentaContableListado> resultados = consulta.ToList();
        CuentasFiltradas.Clear();
        foreach (CuentaContableListado cuenta in resultados) CuentasFiltradas.Add(cuenta);
        if (CuentaSeleccionada is not null && !resultados.Contains(CuentaSeleccionada)) CuentaSeleccionada = null;
        NotificarEstadoListado();
    }

    private bool PuedeAbrirFormulario() => _catalogoCargado && !EstaCargando && !EstaGuardando
        && !EstaPreparandoEdicion && !EstaActualizandoCuenta && !TieneError
        && GruposCreacion.Count > 0 && !MostrarFormulario;

    private void AbrirFormulario()
    {
        EstablecerCuentaEdicion(null);
        LimpiarFormulario();
        LimpiarMensajeExito();
        MostrarFormulario = true;
    }

    private bool PuedeEditarCuenta()
    {
        return CuentaSeleccionada is not null
            && !EstaCargando
            && !EstaGuardando
            && !EstaPreparandoEdicion
            && !EstaActualizandoCuenta
            && !MostrarFormulario;
    }

    private bool PuedeAdministrarCuenta()
    {
        return CuentaSeleccionada is not null
            && !EstaCargando
            && !EstaGuardando
            && !EstaPreparandoEdicion
            && !EstaActualizandoCuenta
            && !MostrarFormulario;
    }

    private async Task CambiarEstadoCuentaAsync()
    {
        CuentaContableListado? cuenta = CuentaSeleccionada;
        if (cuenta is null) return;

        bool nuevoEstado = !cuenta.EstadoCuenta;
        string accion = nuevoEstado ? "activar" : "desactivar";
        string consecuencia = nuevoEstado
            ? string.Empty
            : "\n\nUna cuenta inactiva no estará disponible para nuevas operaciones contables que requieran cuentas activas.";
        MessageBoxResult confirmacion = MessageBox.Show(
            $"¿Está seguro de que desea {accion} la cuenta\n{cuenta.CodigoCuenta} - {cuenta.NombreCuenta}?{consecuencia}",
            nuevoEstado ? "Activar cuenta" : "Desactivar cuenta",
            MessageBoxButton.YesNo,
            nuevoEstado ? MessageBoxImage.Question : MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (confirmacion != MessageBoxResult.Yes) return;

        EstaActualizandoCuenta = true;
        MensajeErrorDetalle = string.Empty;
        LimpiarMensajeExito();

        try
        {
            await _repositorioCuentaContable.CambiarEstadoAsync(
                cuenta.IdCuentaContable,
                nuevoEstado);
            bool actualizado = await CargarCatalogoAsync(cuenta.IdCuentaContable);

            MostrarMensajeExitoTemporal(actualizado
                ? nuevoEstado
                    ? "La cuenta se activó correctamente."
                    : "La cuenta se desactivó correctamente."
                : nuevoEstado
                    ? "La cuenta se activó, pero el catálogo no pudo actualizarse."
                    : "La cuenta se desactivó, pero el catálogo no pudo actualizarse.");
        }
        catch (InvalidOperationException excepcion)
        {
            MensajeErrorDetalle = excepcion.Message;
        }
        catch (Exception)
        {
            MensajeErrorDetalle =
                "No se pudo cambiar el estado de la cuenta. Verifica la conexión e inténtalo nuevamente.";
        }
        finally
        {
            EstaActualizandoCuenta = false;
        }
    }

    private async Task ConfigurarMovimientoCuentaAsync()
    {
        CuentaContableListado? cuenta = CuentaSeleccionada;
        if (cuenta is null) return;

        bool permitirMovimiento = !cuenta.PermiteMovimientoCuenta;
        string mensaje = permitirMovimiento && cuenta.TieneHijas
            ? $"La cuenta {cuenta.CodigoCuenta} - {cuenta.NombreCuenta} tiene subcuentas registradas.\n\n" +
              "¿Desea permitir movimientos directamente en esta cuenta de todas formas?"
            : permitirMovimiento
                ? $"¿Está seguro de que desea permitir movimientos en la cuenta\n{cuenta.CodigoCuenta} - {cuenta.NombreCuenta}?"
                : $"¿Está seguro de que desea impedir movimientos en la cuenta\n{cuenta.CodigoCuenta} - {cuenta.NombreCuenta}?";
        MessageBoxResult confirmacion = MessageBox.Show(
            mensaje,
            permitirMovimiento ? "Permitir movimientos" : "No permitir movimientos",
            MessageBoxButton.YesNo,
            permitirMovimiento && cuenta.TieneHijas
                ? MessageBoxImage.Warning
                : MessageBoxImage.Question,
            MessageBoxResult.No);

        if (confirmacion != MessageBoxResult.Yes) return;

        EstaActualizandoCuenta = true;
        MensajeErrorDetalle = string.Empty;
        LimpiarMensajeExito();

        try
        {
            await _repositorioCuentaContable.ConfigurarMovimientoAsync(
                cuenta.IdCuentaContable,
                permitirMovimiento);
            bool actualizado = await CargarCatalogoAsync(cuenta.IdCuentaContable);

            MostrarMensajeExitoTemporal(actualizado
                ? permitirMovimiento
                    ? "Se habilitaron los movimientos de la cuenta."
                    : "Se deshabilitaron los movimientos de la cuenta."
                : permitirMovimiento
                    ? "Se habilitaron los movimientos, pero el catálogo no pudo actualizarse."
                    : "Se deshabilitaron los movimientos, pero el catálogo no pudo actualizarse.");
        }
        catch (InvalidOperationException excepcion)
        {
            MensajeErrorDetalle = excepcion.Message;
        }
        catch (Exception)
        {
            MensajeErrorDetalle =
                "No se pudo cambiar la configuración de movimiento. Verifica la conexión e inténtalo nuevamente.";
        }
        finally
        {
            EstaActualizandoCuenta = false;
        }
    }

    private async Task EditarCuentaAsync()
    {
        int? idCuenta = CuentaSeleccionada?.IdCuentaContable;
        if (idCuenta is null) return;

        EstaPreparandoEdicion = true;
        MensajeErrorDetalle = string.Empty;
        LimpiarMensajeExito();

        try
        {
            CuentaContableDetalle? detalle =
                await _repositorioCuentaContable.ObtenerPorIdAsync(idCuenta.Value);

            if (detalle is null)
            {
                MensajeErrorDetalle =
                    "La cuenta seleccionada ya no está disponible para editarla.";
                return;
            }

            CargarDatosEdicion(detalle);
            MostrarFormulario = true;
        }
        catch (InvalidOperationException excepcion)
        {
            MensajeErrorDetalle = excepcion.Message;
        }
        catch (Exception)
        {
            MensajeErrorDetalle =
                "No se pudo cargar la cuenta para editarla. Verifica la conexión e inténtalo nuevamente.";
        }
        finally
        {
            EstaPreparandoEdicion = false;
        }
    }

    private void CargarDatosEdicion(CuentaContableDetalle detalle)
    {
        EstablecerCuentaEdicion(detalle);
        _estaCargandoDatosFormulario = true;

        try
        {
            GrupoCreacionSeleccionado = GruposCreacion.FirstOrDefault(
                grupo => grupo.IdGrupoContable == detalle.IdGrupoContable);
            TipoCuentaSeleccionado = TiposCuenta.First(
                tipo => tipo.EsSubcuenta == detalle.IdCuentaPadre.HasValue);
            CuentaPadreSeleccionada = detalle.IdCuentaPadre is int idPadre
                ? CuentasPadre.FirstOrDefault(padre => padre.IdCuentaContable == idPadre)
                : null;
            CodigoCuenta = detalle.CodigoCuenta;
            NombreCuenta = detalle.NombreCuenta;
            NaturalezaSeleccionada = detalle.NaturalezaCuenta;
            _naturalezaAsignadaDesdePadre = detalle.IdCuentaPadre.HasValue;
            PermiteMovimiento = detalle.PermiteMovimientoCuenta;
            MensajeErrorFormulario = string.Empty;
        }
        finally
        {
            _estaCargandoDatosFormulario = false;
        }

        NotificarEstadoFormulario();
    }

    private bool PuedeCancelarCreacion() => MostrarFormulario && !EstaGuardando;
    private void CancelarCreacion()
    {
        string mensaje = EstaEditando
            ? "¿Está seguro de que desea cancelar la edición?\n\nSe perderán los cambios realizados."
            : "¿Está seguro de que desea cancelar?\n\nSe perderán los datos ingresados.";
        string titulo = EstaEditando ? "Cancelar edición de cuenta" : "Cancelar nueva cuenta";
        MessageBoxResult confirmacion = MessageBox.Show(
            mensaje,
            titulo,
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (confirmacion == MessageBoxResult.Yes)
            OcultarYLimpiarFormulario();
    }

    private bool PuedeGuardarCuenta()
    {
        return MostrarFormulario
            && !EstaGuardando
            && !EstaCargando
            && !EstaActualizandoCuenta
            && !string.IsNullOrWhiteSpace(CodigoCuenta)
            && !string.IsNullOrWhiteSpace(NombreCuenta)
            && GrupoCreacionSeleccionado is not null
            && TipoCuentaSeleccionado is not null
            && NaturalezaSeleccionada is not null
            && (!EsSubcuenta || CuentaPadreSeleccionada is not null);
    }

    private async Task GuardarCuentaAsync()
    {
        MensajeErrorFormulario = string.Empty;
        MensajeExito = string.Empty;
        if (!ValidarFormulario()) return;

        bool estabaEditando = EstaEditando;
        int idCuenta = _idCuentaEdicion ?? 0;
        int ordenCuenta;
        if (estabaEditando)
        {
            ordenCuenta = _ordenCuentaEdicion;
            if (ordenCuenta <= 0)
            {
                MensajeErrorFormulario =
                    "La cuenta no tiene un orden válido y no puede actualizarse.";
                return;
            }
        }
        else if (!TryObtenerSiguienteOrden(out ordenCuenta))
        {
            MensajeErrorFormulario =
                "No se puede calcular un orden válido para la nueva cuenta.";
            return;
        }

        string pregunta = estabaEditando
            ? $"¿Está seguro de que desea guardar los cambios de la cuenta\n{CodigoCuenta.Trim()} - {NombreCuenta.Trim()}?"
            : $"¿Está seguro de que desea crear {(EsSubcuenta ? "la subcuenta" : "la cuenta")}\n{CodigoCuenta.Trim()} - {NombreCuenta.Trim()}?";
        MessageBoxResult confirmacion = MessageBox.Show(
            pregunta,
            estabaEditando ? "Guardar cambios de la cuenta" : "Guardar cuenta contable",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);

        if (confirmacion != MessageBoxResult.Yes) return;

        var cuenta = new CuentaContable
        {
            IdCuentaContable = idCuenta,
            IdGrupoContable = GrupoCreacionSeleccionado!.IdGrupoContable,
            IdCuentaPadre = EsSubcuenta ? CuentaPadreSeleccionada!.IdCuentaContable : null,
            CodigoCuenta = CodigoCuenta.Trim(),
            NombreCuenta = NombreCuenta.Trim(),
            NaturalezaCuenta = NaturalezaSeleccionada!,
            PermiteMovimientoCuenta = estabaEditando
                ? _permiteMovimientoOriginal
                : PermiteMovimiento,
            EstadoCuenta = estabaEditando ? _estadoCuentaOriginal : true,
            OrdenCuenta = ordenCuenta
        };
        int? idSeleccionada = estabaEditando
            ? idCuenta
            : CuentaSeleccionada?.IdCuentaContable;
        EstaGuardando = true;
        try
        {
            if (estabaEditando)
                await _repositorioCuentaContable.ActualizarAsync(cuenta);
            else
                await _repositorioCuentaContable.CrearAsync(cuenta);

            bool actualizado = await CargarCatalogoAsync(idSeleccionada);
            OcultarYLimpiarFormulario();
            MostrarMensajeExitoTemporal(actualizado
                ? estabaEditando
                    ? "La cuenta contable se actualizó correctamente."
                    : "La cuenta contable se creó correctamente."
                : estabaEditando
                    ? "La cuenta se actualizó correctamente, pero el catálogo no pudo actualizarse."
                    : "La cuenta se creó correctamente, pero el catálogo no pudo actualizarse.");
        }
        catch (InvalidOperationException excepcion) { MensajeErrorFormulario = excepcion.Message; }
        catch (Exception)
        {
            MensajeErrorFormulario = estabaEditando
                ? "No se pudo actualizar la cuenta contable. Verifica la conexión e inténtalo nuevamente."
                : "No se pudo crear la cuenta contable. Verifica la conexión e inténtalo nuevamente.";
        }
        finally { EstaGuardando = false; }
    }

    private bool ValidarFormulario()
    {
        if (string.IsNullOrWhiteSpace(CodigoCuenta)) return Error("El código de la cuenta es obligatorio.");
        if (string.IsNullOrWhiteSpace(NombreCuenta)) return Error("El nombre de la cuenta es obligatorio.");
        if (GrupoCreacionSeleccionado is null) return Error("Selecciona el grupo contable de la cuenta.");
        if (TipoCuentaSeleccionado is null) return Error("Selecciona si crearás una cuenta principal o una subcuenta.");
        if (NaturalezaSeleccionada is null) return Error("Selecciona la naturaleza de la cuenta.");
        if (!EsSubcuenta) return true;
        if (CuentaPadreSeleccionada is null) return Error("Selecciona la cuenta padre de la subcuenta.");
        if (CuentaPadreSeleccionada.IdGrupoContable != GrupoCreacionSeleccionado.IdGrupoContable)
            return Error("La cuenta padre debe pertenecer al mismo grupo contable.");
        if (_idCuentaEdicion is int idCuenta)
        {
            if (CuentaPadreSeleccionada.IdCuentaContable == idCuenta)
                return Error("Una cuenta no puede ser su propia cuenta padre.");

            if (ObtenerIdsNoPermitidosComoPadre(idCuenta)
                .Contains(CuentaPadreSeleccionada.IdCuentaContable))
            {
                return Error("La cuenta padre seleccionada produciría un ciclo jerárquico.");
            }
        }
        return true;
    }

    private bool Error(string mensaje)
    {
        MensajeErrorFormulario = mensaje;
        return false;
    }

    private bool TryObtenerSiguienteOrden(out int ordenCuenta)
    {
        ordenCuenta = 1;
        if (CuentasContables.Count == 0) return true;
        int maximo = CuentasContables.Max(c => c.OrdenCuenta);
        if (maximo == int.MaxValue) return false;
        ordenCuenta = maximo + 1;
        return true;
    }

    private void OcultarYLimpiarFormulario()
    {
        MostrarFormulario = false;
        EstablecerCuentaEdicion(null);
        LimpiarFormulario();
    }

    private void EstablecerCuentaEdicion(CuentaContableDetalle? detalle)
    {
        _idCuentaEdicion = detalle?.IdCuentaContable;
        _idCuentaPadreOriginal = detalle?.IdCuentaPadre;
        _ordenCuentaEdicion = detalle?.OrdenCuenta ?? 0;
        _codigoCuentaOriginal = detalle?.CodigoCuenta ?? string.Empty;
        _permiteMovimientoOriginal = detalle?.PermiteMovimientoCuenta ?? false;
        _estadoCuentaOriginal = detalle?.EstadoCuenta ?? false;
        NotificarCambio(nameof(EstaEditando));
        NotificarCambio(nameof(PuedeEditarMovimientoEnFormulario));
        NotificarCambio(nameof(TituloFormulario));
        NotificarCambio(nameof(DescripcionFormulario));
        NotificarCambio(nameof(TextoGuardar));
        NotificarEstadoFormulario();
    }

    private void LimpiarFormulario()
    {
        CodigoCuenta = string.Empty;
        NombreCuenta = string.Empty;
        GrupoCreacionSeleccionado = null;
        TipoCuentaSeleccionado = TiposCuenta[0];
        CuentaPadreSeleccionada = null;
        NaturalezaSeleccionada = null;
        PermiteMovimiento = true;
        MensajeErrorFormulario = string.Empty;
    }

    private void LimpiarErrorFormulario()
    {
        if (!string.IsNullOrEmpty(MensajeErrorFormulario)) MensajeErrorFormulario = string.Empty;
        _guardarCuentaCommand.NotificarPuedeEjecutar();
    }

    private void EstablecerNaturalezaDesdePadre(string? naturaleza)
    {
        EstablecerPropiedad(
            ref _naturalezaSeleccionada,
            naturaleza,
            nameof(NaturalezaSeleccionada));
        _naturalezaAsignadaDesdePadre = naturaleza is not null;
        _guardarCuentaCommand.NotificarPuedeEjecutar();
    }

    private void GenerarCodigoSubcuenta(
        OpcionCuentaPadre padre,
        int? idCuentaExcluida = null)
    {
        List<CuentaContableListado> hijasDirectas = CuentasContables
            .Where(cuenta => cuenta.IdCuentaPadre == padre.IdCuentaContable
                && cuenta.IdCuentaContable != idCuentaExcluida)
            .ToList();

        string separador = string.Empty;
        int anchoSufijo = 2;
        int ultimoNumero = 0;

        if (hijasDirectas.Count > 0)
        {
            string? separadorDetectado = null;
            int? anchoDetectado = null;

            foreach (CuentaContableListado hija in hijasDirectas)
            {
                string separadorHija;
                string sufijo;
                string prefijoConPunto = padre.CodigoCuenta + ".";

                if (hija.CodigoCuenta.StartsWith(prefijoConPunto, StringComparison.Ordinal))
                {
                    separadorHija = ".";
                    sufijo = hija.CodigoCuenta[prefijoConPunto.Length..];
                }
                else if (hija.CodigoCuenta.StartsWith(padre.CodigoCuenta, StringComparison.Ordinal))
                {
                    separadorHija = string.Empty;
                    sufijo = hija.CodigoCuenta[padre.CodigoCuenta.Length..];
                }
                else
                {
                    MostrarErrorGeneracionCodigo(padre);
                    return;
                }

                if (string.IsNullOrEmpty(sufijo)
                    || !sufijo.All(char.IsDigit)
                    || !int.TryParse(sufijo, out int numero)
                    || numero < 1
                    || (separadorDetectado is not null && separadorDetectado != separadorHija)
                    || (anchoDetectado.HasValue && anchoDetectado.Value != sufijo.Length))
                {
                    MostrarErrorGeneracionCodigo(padre);
                    return;
                }

                separadorDetectado ??= separadorHija;
                anchoDetectado ??= sufijo.Length;
                ultimoNumero = Math.Max(ultimoNumero, numero);
            }

            separador = separadorDetectado!;
            anchoSufijo = anchoDetectado!.Value;
        }

        if (ultimoNumero == int.MaxValue)
        {
            CodigoCuenta = string.Empty;
            MensajeErrorFormulario = "No se pudo generar un código disponible para la subcuenta.";
            return;
        }

        int siguienteNumero = ultimoNumero + 1;
        while (siguienteNumero > 0)
        {
            string codigo = padre.CodigoCuenta
                + separador
                + siguienteNumero.ToString($"D{anchoSufijo}");

            if (codigo.Length > 20)
            {
                CodigoCuenta = string.Empty;
                MensajeErrorFormulario =
                    "No se puede generar el código porque superaría los 20 caracteres permitidos.";
                return;
            }

            bool yaExiste = CuentasContables.Any(cuenta =>
                cuenta.IdCuentaContable != idCuentaExcluida
                &&
                string.Equals(cuenta.CodigoCuenta, codigo, StringComparison.OrdinalIgnoreCase));

            if (!yaExiste)
            {
                CodigoCuenta = codigo;
                return;
            }

            if (siguienteNumero == int.MaxValue) break;
            siguienteNumero++;
        }

        CodigoCuenta = string.Empty;
        MensajeErrorFormulario = "No se pudo generar un código disponible para la subcuenta.";
    }

    private void MostrarErrorGeneracionCodigo(OpcionCuentaPadre padre)
    {
        CodigoCuenta = string.Empty;
        MensajeErrorFormulario =
            $"Las hijas directas de {padre.CodigoCuenta} no utilizan una convención uniforme de códigos.";
    }

    private void MostrarMensajeExitoTemporal(string mensaje)
    {
        int version = ++_versionMensajeExito;
        MensajeExito = mensaje;
        _ = LimpiarMensajeExitoDespuesAsync(version);
    }

    private async Task LimpiarMensajeExitoDespuesAsync(int version)
    {
        await Task.Delay(TimeSpan.FromSeconds(3));

        if (version == _versionMensajeExito)
            MensajeExito = string.Empty;
    }

    private void LimpiarMensajeExito()
    {
        _versionMensajeExito++;
        MensajeExito = string.Empty;
    }

    private async Task CargarDetalleAsync(CuentaContableListado? cuenta)
    {
        int version = ++_versionSolicitudDetalle;
        if (cuenta is null) { EstaCargandoDetalle = false; return; }
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
            if (version != _versionSolicitudDetalle) return;
            if (detalle is null) { MensajeErrorDetalle = "La cuenta seleccionada ya no está disponible en el catálogo."; return; }
            _detallesCargados[cuenta.IdCuentaContable] = detalle;
            DetalleSeleccionado = detalle;
        }
        catch (InvalidOperationException excepcion)
        {
            if (version == _versionSolicitudDetalle) MensajeErrorDetalle = excepcion.Message;
        }
        catch (Exception)
        {
            if (version == _versionSolicitudDetalle)
                MensajeErrorDetalle = "No se pudo consultar el detalle de la cuenta. Verifica la conexión e inténtalo nuevamente.";
        }
        finally { if (version == _versionSolicitudDetalle) EstaCargandoDetalle = false; }
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

    private void NotificarEstadoFormulario()
    {
        NotificarCambio(nameof(FormularioHabilitado));
        _nuevaCuentaCommand.NotificarPuedeEjecutar();
        _cancelarCreacionCommand.NotificarPuedeEjecutar();
        _guardarCuentaCommand.NotificarPuedeEjecutar();
        _editarCuentaCommand.NotificarPuedeEjecutar();
        _cambiarEstadoCommand.NotificarPuedeEjecutar();
        _configurarMovimientoCommand.NotificarPuedeEjecutar();
    }

    private void NotificarAccionesCuentaSeleccionada()
    {
        NotificarCambio(nameof(TextoAccionEstado));
        NotificarCambio(nameof(TextoAccionMovimiento));
        _editarCuentaCommand.NotificarPuedeEjecutar();
        _cambiarEstadoCommand.NotificarPuedeEjecutar();
        _configurarMovimientoCommand.NotificarPuedeEjecutar();
    }
}

public sealed class OpcionFiltroElemento
{
    public OpcionFiltroElemento(int? id, string nombre) { IdElementoContable = id; Nombre = nombre; }
    public int? IdElementoContable { get; }
    public string Nombre { get; }
}

public sealed class OpcionFiltroBooleano
{
    public OpcionFiltroBooleano(bool? valor, string nombre) { Valor = valor; Nombre = nombre; }
    public bool? Valor { get; }
    public string Nombre { get; }
}

public sealed class OpcionGrupoContable
{
    public OpcionGrupoContable(int idGrupo, string codigoGrupo, string nombreGrupo, int idElemento,
        string codigoElemento, string nombreElemento)
    {
        IdGrupoContable = idGrupo; CodigoGrupo = codigoGrupo; NombreGrupo = nombreGrupo;
        IdElementoContable = idElemento; CodigoElemento = codigoElemento; NombreElemento = nombreElemento;
    }
    public int IdGrupoContable { get; }
    public string CodigoGrupo { get; }
    public string NombreGrupo { get; }
    public int IdElementoContable { get; }
    public string CodigoElemento { get; }
    public string NombreElemento { get; }
    public string Texto => $"{CodigoGrupo} — {NombreGrupo}";
}

public sealed class OpcionTipoCuenta
{
    public OpcionTipoCuenta(bool esSubcuenta, string nombre) { EsSubcuenta = esSubcuenta; Nombre = nombre; }
    public bool EsSubcuenta { get; }
    public string Nombre { get; }
}

public sealed class OpcionCuentaPadre
{
    public OpcionCuentaPadre(CuentaContableListado cuenta)
    {
        IdCuentaContable = cuenta.IdCuentaContable; IdGrupoContable = cuenta.IdGrupoContable;
        CodigoCuenta = cuenta.CodigoCuenta; NombreCuenta = cuenta.NombreCuenta;
        NaturalezaCuenta = cuenta.NaturalezaCuenta; EstadoCuenta = cuenta.EstadoCuenta;
    }
    public int IdCuentaContable { get; }
    public int IdGrupoContable { get; }
    public string CodigoCuenta { get; }
    public string NombreCuenta { get; }
    public string NaturalezaCuenta { get; }
    public bool EstadoCuenta { get; }
    public string Texto => EstadoCuenta ? $"{CodigoCuenta} — {NombreCuenta}" : $"{CodigoCuenta} — {NombreCuenta} (inactiva)";
}
