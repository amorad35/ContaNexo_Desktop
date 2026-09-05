using System.Collections.ObjectModel;
using System.Windows;
using ContaNexo.Core.Models;
using ContaNexo.Data.Repositories;

namespace ContaNexo.Desktop.ViewModels;

public sealed class PeriodoContableViewModel : ViewModelBase
{
    private readonly RepositorioPeriodoContable _repositorioPeriodoContable;
    private readonly Action<PeriodoContableListado?> _establecerPeriodoActivo;
    private readonly ComandoRelay _nuevoPeriodoCommand;
    private readonly ComandoRelay _cancelarCreacionCommand;
    private readonly ComandoRelay _seleccionarPeriodoCommand;
    private readonly ComandoRelay _editarPeriodoCommand;
    private readonly ComandoAsync _guardarPeriodoCommand;
    private readonly ComandoAsync _cerrarPeriodoCommand;

    private Empresa? _empresaActual;
    private int? _idPeriodoActivo;
    private int? _idPeriodoEdicion;
    private bool _estaCargando;
    private bool _estaGuardando;
    private bool _estaCerrando;
    private bool _empresaNoConfigurada = true;
    private bool _mostrarFormulario;
    private string _nombreEmpresa = "Sin configurar";
    private string _nombrePeriodo = string.Empty;
    private DateTime? _fechaInicioPeriodo;
    private DateTime? _fechaFinPeriodo;
    private string _mensajeError = string.Empty;
    private string _mensajeErrorFormulario = string.Empty;
    private string _mensajeExito = string.Empty;

    public PeriodoContableViewModel(
        RepositorioPeriodoContable repositorioPeriodoContable,
        Action<PeriodoContableListado?> establecerPeriodoActivo)
    {
        _repositorioPeriodoContable = repositorioPeriodoContable;
        _establecerPeriodoActivo = establecerPeriodoActivo;
        _nuevoPeriodoCommand = new ComandoRelay(AbrirFormulario, PuedeAbrirFormulario);
        _cancelarCreacionCommand = new ComandoRelay(CancelarCreacion, PuedeCancelarCreacion);
        _seleccionarPeriodoCommand = new ComandoRelay(
            SeleccionarPeriodo,
            PuedeSeleccionarPeriodo);
        _editarPeriodoCommand = new ComandoRelay(
            EditarPeriodo,
            PuedeEditarPeriodo);
        _guardarPeriodoCommand = new ComandoAsync(GuardarPeriodoAsync, PuedeGuardarPeriodo);
        _cerrarPeriodoCommand = new ComandoAsync(CerrarPeriodoAsync, PuedeCerrarPeriodo);
    }

    public ObservableCollection<PeriodoContableItemViewModel> PeriodosContables { get; } = new();

    public bool EstaCargando
    {
        get => _estaCargando;
        private set
        {
            if (EstablecerPropiedad(ref _estaCargando, value))
            {
                NotificarEstadoVista();
            }
        }
    }

    public bool EstaGuardando
    {
        get => _estaGuardando;
        private set
        {
            if (EstablecerPropiedad(ref _estaGuardando, value))
            {
                NotificarCambio(nameof(FormularioHabilitado));
                NotificarCambio(nameof(TextoGuardar));
                NotificarEstadoComandos();
            }
        }
    }

    public bool EstaCerrando
    {
        get => _estaCerrando;
        private set
        {
            if (EstablecerPropiedad(ref _estaCerrando, value))
            {
                NotificarEstadoComandos();
            }
        }
    }

    public bool EmpresaNoConfigurada
    {
        get => _empresaNoConfigurada;
        private set
        {
            if (EstablecerPropiedad(ref _empresaNoConfigurada, value))
            {
                NotificarEstadoVista();
            }
        }
    }

    public bool MostrarFormulario
    {
        get => _mostrarFormulario;
        private set
        {
            if (EstablecerPropiedad(ref _mostrarFormulario, value))
            {
                NotificarCambio(nameof(MostrarEstadoVacio));
                NotificarEstadoComandos();
            }
        }
    }

    public string NombreEmpresa
    {
        get => _nombreEmpresa;
        private set => EstablecerPropiedad(ref _nombreEmpresa, value);
    }

    public string NombrePeriodo
    {
        get => _nombrePeriodo;
        set
        {
            if (EstablecerPropiedad(ref _nombrePeriodo, value))
            {
                LimpiarErrorFormulario();
            }
        }
    }

    public DateTime? FechaInicioPeriodo
    {
        get => _fechaInicioPeriodo;
        set
        {
            if (EstablecerPropiedad(ref _fechaInicioPeriodo, value))
            {
                LimpiarErrorFormulario();
            }
        }
    }

    public DateTime? FechaFinPeriodo
    {
        get => _fechaFinPeriodo;
        set
        {
            if (EstablecerPropiedad(ref _fechaFinPeriodo, value))
            {
                LimpiarErrorFormulario();
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
                NotificarEstadoVista();
            }
        }
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

    public bool TienePeriodos => PeriodosContables.Count > 0;

    public bool MostrarEstadoVacio =>
        !EstaCargando
        && !EmpresaNoConfigurada
        && !TieneError
        && !TienePeriodos
        && !MostrarFormulario;

    public bool FormularioHabilitado => !EstaGuardando;

    public bool EstaEditando => _idPeriodoEdicion.HasValue;

    public string TituloFormulario =>
        EstaEditando ? "Editar período contable" : "Nuevo período contable";

    public string DescripcionFormulario => EstaEditando
        ? "Modifica el nombre o el rango de fechas del período abierto."
        : "Ingresa el nombre y el rango de fechas. El período se creará con estado Abierto.";

    public string TextoGuardar => EstaGuardando
        ? "Guardando..."
        : EstaEditando
            ? "Guardar cambios"
            : "Guardar";

    public ComandoRelay NuevoPeriodoCommand => _nuevoPeriodoCommand;

    public ComandoRelay CancelarCreacionCommand => _cancelarCreacionCommand;

    public ComandoRelay SeleccionarPeriodoCommand => _seleccionarPeriodoCommand;

    public ComandoRelay EditarPeriodoCommand => _editarPeriodoCommand;

    public ComandoAsync GuardarPeriodoCommand => _guardarPeriodoCommand;

    public ComandoAsync CerrarPeriodoCommand => _cerrarPeriodoCommand;

    public async Task CargarAsync(
        Empresa? empresa,
        string? mensajeErrorContextoEmpresa = null,
        int? idPeriodoActivo = null)
    {
        if (EstaCargando)
        {
            return;
        }

        _empresaActual = empresa;
        _idPeriodoActivo = idPeriodoActivo;
        PeriodosContables.Clear();
        MensajeError = string.Empty;
        MensajeExito = string.Empty;
        EmpresaNoConfigurada = empresa is null;
        NombreEmpresa = empresa?.NombreEmpresa ?? "Sin configurar";
        NotificarEstadoVista();

        if (!string.IsNullOrWhiteSpace(mensajeErrorContextoEmpresa))
        {
            EmpresaNoConfigurada = false;
            OcultarYLimpiarFormulario();
            MensajeError = mensajeErrorContextoEmpresa;
            return;
        }

        if (empresa is null)
        {
            OcultarYLimpiarFormulario();
            _idPeriodoActivo = null;
            _establecerPeriodoActivo(null);
            return;
        }

        EstaCargando = true;

        try
        {
            IEnumerable<PeriodoContableListado> periodos =
                await _repositorioPeriodoContable.ListarAsync(empresa.IdEmpresa);

            foreach (PeriodoContableListado periodo in periodos)
            {
                PeriodosContables.Add(new PeriodoContableItemViewModel(periodo));
            }

            SincronizarPeriodoActivoDespuesDeCarga();
        }
        catch (Exception)
        {
            PeriodosContables.Clear();
            MensajeError =
                "No se pudieron cargar los períodos contables. Verifica la conexión e inténtalo nuevamente.";
        }
        finally
        {
            EstaCargando = false;
            NotificarEstadoVista();
        }
    }

    private bool PuedeAbrirFormulario()
    {
        return _empresaActual is not null
            && !EmpresaNoConfigurada
            && !TieneError
            && !EstaCargando
            && !EstaGuardando
            && !EstaCerrando
            && !MostrarFormulario;
    }

    private void AbrirFormulario()
    {
        EstablecerPeriodoEdicion(null);
        LimpiarFormulario();
        MensajeExito = string.Empty;
        MostrarFormulario = true;
    }

    private bool PuedeCancelarCreacion()
    {
        return MostrarFormulario && !EstaGuardando;
    }

    private void CancelarCreacion()
    {
        OcultarYLimpiarFormulario();
    }

    private bool PuedeSeleccionarPeriodo(object? parametro)
    {
        return parametro is PeriodoContableItemViewModel periodo
            && periodo.PuedeSeleccionar
            && !EstaCargando
            && !EstaGuardando
            && !EstaCerrando
            && !MostrarFormulario;
    }

    private void SeleccionarPeriodo(object? parametro)
    {
        if (parametro is not PeriodoContableItemViewModel periodo
            || !periodo.PuedeSeleccionar)
        {
            return;
        }

        _idPeriodoActivo = periodo.IdPeriodoContable;
        ActualizarIndicadoresPeriodoActivo();
        _establecerPeriodoActivo(periodo.Periodo);
    }

    private bool PuedeEditarPeriodo(object? parametro)
    {
        return parametro is PeriodoContableItemViewModel periodo
            && periodo.PuedeEditar
            && !EstaCargando
            && !EstaGuardando
            && !EstaCerrando
            && !MostrarFormulario;
    }

    private void EditarPeriodo(object? parametro)
    {
        if (parametro is not PeriodoContableItemViewModel periodo
            || !PuedeEditarPeriodo(periodo))
        {
            return;
        }

        EstablecerPeriodoEdicion(periodo.IdPeriodoContable);
        NombrePeriodo = periodo.NombrePeriodo;
        FechaInicioPeriodo = periodo.FechaInicioPeriodo.Date;
        FechaFinPeriodo = periodo.FechaFinPeriodo.Date;
        MensajeErrorFormulario = string.Empty;
        MensajeExito = string.Empty;
        MostrarFormulario = true;
    }

    private bool PuedeCerrarPeriodo(object? parametro)
    {
        return parametro is PeriodoContableItemViewModel periodo
            && periodo.PuedeCerrar
            && _empresaActual is not null
            && !EstaCargando
            && !EstaGuardando
            && !EstaCerrando
            && !MostrarFormulario;
    }

    private async Task CerrarPeriodoAsync(object? parametro)
    {
        if (parametro is not PeriodoContableItemViewModel periodo
            || !PuedeCerrarPeriodo(periodo))
        {
            return;
        }

        MessageBoxResult confirmacion = MessageBox.Show(
            $"¿Deseas cerrar el período \"{periodo.NombrePeriodo}\"?\n\n" +
            "Una vez cerrado, no podrá editarse ni utilizarse como período activo. " +
            "La reapertura no está disponible actualmente.",
            "Cerrar período contable",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (confirmacion != MessageBoxResult.Yes)
        {
            return;
        }

        Empresa? empresa = _empresaActual;

        if (empresa is null)
        {
            MensajeError =
                "No se puede cerrar el período porque no hay una empresa configurada.";
            return;
        }

        EstaCerrando = true;
        MensajeError = string.Empty;
        MensajeExito = string.Empty;

        try
        {
            PeriodoContableCierre cierre =
                await _repositorioPeriodoContable.CerrarAsync(periodo.IdPeriodoContable);

            if (string.Equals(cierre.EstadoPeriodo, "Cerrado", StringComparison.Ordinal)
                && _idPeriodoActivo == cierre.IdPeriodoContable)
            {
                _idPeriodoActivo = null;
                _establecerPeriodoActivo(null);
            }

            await CargarAsync(
                empresa,
                idPeriodoActivo: _idPeriodoActivo);

            MensajeExito = TieneError
                ? "El período se cerró correctamente, pero el listado no pudo actualizarse."
                : "El período contable se cerró correctamente.";
        }
        catch (InvalidOperationException excepcion)
        {
            MensajeError = excepcion.Message;
        }
        catch (Exception)
        {
            MensajeError =
                "No se pudo cerrar el período contable. Verifica la conexión e inténtalo nuevamente.";
        }
        finally
        {
            EstaCerrando = false;
        }
    }

    private bool PuedeGuardarPeriodo()
    {
        return MostrarFormulario
            && _empresaActual is not null
            && !EstaGuardando;
    }

    private async Task GuardarPeriodoAsync()
    {
        MensajeErrorFormulario = string.Empty;
        MensajeExito = string.Empty;

        if (!ValidarFormulario())
        {
            return;
        }

        Empresa? empresa = _empresaActual;

        if (empresa is null)
        {
            MensajeErrorFormulario =
                "Primero configura la empresa antes de crear un período contable.";
            return;
        }

        bool estabaEditando = EstaEditando;

        var periodo = new PeriodoContable
        {
            IdPeriodoContable = _idPeriodoEdicion ?? 0,
            IdEmpresa = empresa.IdEmpresa,
            NombrePeriodo = NombrePeriodo.Trim(),
            FechaInicioPeriodo = FechaInicioPeriodo!.Value.Date,
            FechaFinPeriodo = FechaFinPeriodo!.Value.Date
        };

        EstaGuardando = true;

        try
        {
            if (estabaEditando)
            {
                await _repositorioPeriodoContable.ActualizarAsync(periodo);
            }
            else
            {
                await _repositorioPeriodoContable.CrearAsync(periodo);
            }

            await CargarAsync(
                empresa,
                idPeriodoActivo: _idPeriodoActivo);

            OcultarYLimpiarFormulario();
            MensajeExito = TieneError
                ? estabaEditando
                    ? "El período se actualizó correctamente, pero el listado no pudo actualizarse."
                    : "El período se creó correctamente, pero el listado no pudo actualizarse."
                : estabaEditando
                    ? "El período contable se actualizó correctamente."
                    : "El período contable se creó correctamente.";
        }
        catch (InvalidOperationException excepcion)
        {
            MensajeErrorFormulario = excepcion.Message;
        }
        catch (Exception)
        {
            MensajeErrorFormulario =
                "No se pudo crear el período contable. Verifica la conexión e inténtalo nuevamente.";
        }
        finally
        {
            EstaGuardando = false;
        }
    }

    private bool ValidarFormulario()
    {
        if (string.IsNullOrWhiteSpace(NombrePeriodo))
        {
            MensajeErrorFormulario = "El nombre del período es obligatorio.";
            return false;
        }

        if (FechaInicioPeriodo is null)
        {
            MensajeErrorFormulario = "La fecha de inicio es obligatoria.";
            return false;
        }

        if (FechaFinPeriodo is null)
        {
            MensajeErrorFormulario = "La fecha de fin es obligatoria.";
            return false;
        }

        if (FechaInicioPeriodo.Value.Date > FechaFinPeriodo.Value.Date)
        {
            MensajeErrorFormulario =
                "La fecha de inicio no puede ser posterior a la fecha de fin.";
            return false;
        }

        return true;
    }

    private void OcultarYLimpiarFormulario()
    {
        MostrarFormulario = false;
        EstablecerPeriodoEdicion(null);
        LimpiarFormulario();
    }

    private void LimpiarFormulario()
    {
        NombrePeriodo = string.Empty;
        FechaInicioPeriodo = null;
        FechaFinPeriodo = null;
        MensajeErrorFormulario = string.Empty;
    }

    private void LimpiarErrorFormulario()
    {
        if (!string.IsNullOrEmpty(MensajeErrorFormulario))
        {
            MensajeErrorFormulario = string.Empty;
        }
    }

    private void EstablecerPeriodoEdicion(int? idPeriodoContable)
    {
        if (_idPeriodoEdicion == idPeriodoContable)
        {
            return;
        }

        _idPeriodoEdicion = idPeriodoContable;
        NotificarCambio(nameof(EstaEditando));
        NotificarCambio(nameof(TituloFormulario));
        NotificarCambio(nameof(DescripcionFormulario));
        NotificarCambio(nameof(TextoGuardar));
    }

    private void SincronizarPeriodoActivoDespuesDeCarga()
    {
        if (_idPeriodoActivo is null)
        {
            ActualizarIndicadoresPeriodoActivo();
            return;
        }

        PeriodoContableItemViewModel? periodoActivo = PeriodosContables.FirstOrDefault(
            periodo => periodo.IdPeriodoContable == _idPeriodoActivo
                && periodo.EsAbierto);

        if (periodoActivo is null)
        {
            _idPeriodoActivo = null;
            ActualizarIndicadoresPeriodoActivo();
            _establecerPeriodoActivo(null);
            return;
        }

        ActualizarIndicadoresPeriodoActivo();
        _establecerPeriodoActivo(periodoActivo.Periodo);
    }

    private void ActualizarIndicadoresPeriodoActivo()
    {
        foreach (PeriodoContableItemViewModel periodo in PeriodosContables)
        {
            periodo.EstablecerActivo(periodo.IdPeriodoContable == _idPeriodoActivo);
        }

        _seleccionarPeriodoCommand.NotificarPuedeEjecutar();
    }

    private void NotificarEstadoVista()
    {
        NotificarCambio(nameof(TieneError));
        NotificarCambio(nameof(TienePeriodos));
        NotificarCambio(nameof(MostrarEstadoVacio));
        NotificarEstadoComandos();
    }

    private void NotificarEstadoComandos()
    {
        _nuevoPeriodoCommand.NotificarPuedeEjecutar();
        _cancelarCreacionCommand.NotificarPuedeEjecutar();
        _seleccionarPeriodoCommand.NotificarPuedeEjecutar();
        _editarPeriodoCommand.NotificarPuedeEjecutar();
        _guardarPeriodoCommand.NotificarPuedeEjecutar();
        _cerrarPeriodoCommand.NotificarPuedeEjecutar();
    }
}

public sealed class PeriodoContableItemViewModel : ViewModelBase
{
    private bool _esActivo;

    public PeriodoContableItemViewModel(PeriodoContableListado periodo)
    {
        Periodo = periodo;
    }

    public PeriodoContableListado Periodo { get; }

    public int IdPeriodoContable => Periodo.IdPeriodoContable;

    public string NombrePeriodo => Periodo.NombrePeriodo;

    public DateTime FechaInicioPeriodo => Periodo.FechaInicioPeriodo;

    public DateTime FechaFinPeriodo => Periodo.FechaFinPeriodo;

    public string EstadoPeriodo => Periodo.EstadoPeriodo;

    public bool EsAbierto =>
        string.Equals(EstadoPeriodo, "Abierto", StringComparison.Ordinal);

    public bool EsActivo
    {
        get => _esActivo;
        private set
        {
            if (EstablecerPropiedad(ref _esActivo, value))
            {
                NotificarCambio(nameof(PuedeSeleccionar));
            }
        }
    }

    public bool PuedeSeleccionar => EsAbierto && !EsActivo;

    public bool PuedeEditar => EsAbierto;

    public bool PuedeCerrar => EsAbierto;

    public void EstablecerActivo(bool esActivo)
    {
        EsActivo = esActivo && EsAbierto;
    }
}
