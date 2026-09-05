using ContaNexo.Core.Models;
using ContaNexo.Data.Repositories;

namespace ContaNexo.Desktop.ViewModels;

public sealed class EmpresaViewModel : ViewModelBase
{
    private readonly RepositorioEmpresa _repositorioEmpresa;
    private readonly Action<Empresa?> _establecerEmpresaActiva;
    private readonly ComandoAsync _guardarEmpresaCommand;

    private Empresa? _empresaConfigurada;
    private string _nombreEmpresa = string.Empty;
    private string _rucEmpresa = string.Empty;
    private string _direccionEmpresa = string.Empty;
    private string _telefonoEmpresa = string.Empty;
    private string _correoEmpresa = string.Empty;
    private string _mensajeError = string.Empty;
    private string _mensajeExito = string.Empty;
    private bool _estaCargando;
    private bool _estaGuardando;
    private bool _cargaValida;
    private bool _hayInconsistencia;

    public EmpresaViewModel(
        RepositorioEmpresa repositorioEmpresa,
        Action<Empresa?> establecerEmpresaActiva)
    {
        _repositorioEmpresa = repositorioEmpresa;
        _establecerEmpresaActiva = establecerEmpresaActiva;
        _guardarEmpresaCommand = new ComandoAsync(GuardarEmpresaAsync, PuedeGuardarEmpresa);
    }

    public string NombreEmpresa
    {
        get => _nombreEmpresa;
        set
        {
            if (EstablecerPropiedad(ref _nombreEmpresa, value))
            {
                _guardarEmpresaCommand.NotificarPuedeEjecutar();
            }
        }
    }

    public string RucEmpresa
    {
        get => _rucEmpresa;
        set => EstablecerPropiedad(ref _rucEmpresa, value);
    }

    public string DireccionEmpresa
    {
        get => _direccionEmpresa;
        set => EstablecerPropiedad(ref _direccionEmpresa, value);
    }

    public string TelefonoEmpresa
    {
        get => _telefonoEmpresa;
        set => EstablecerPropiedad(ref _telefonoEmpresa, value);
    }

    public string CorreoEmpresa
    {
        get => _correoEmpresa;
        set => EstablecerPropiedad(ref _correoEmpresa, value);
    }

    public string MensajeError
    {
        get => _mensajeError;
        private set => EstablecerPropiedad(ref _mensajeError, value);
    }

    public string MensajeExito
    {
        get => _mensajeExito;
        private set => EstablecerPropiedad(ref _mensajeExito, value);
    }

    public bool EstaCargando
    {
        get => _estaCargando;
        private set
        {
            if (EstablecerPropiedad(ref _estaCargando, value))
            {
                NotificarCambio(nameof(FormularioHabilitado));
                _guardarEmpresaCommand.NotificarPuedeEjecutar();
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
                _guardarEmpresaCommand.NotificarPuedeEjecutar();
            }
        }
    }

    public bool HayInconsistencia
    {
        get => _hayInconsistencia;
        private set
        {
            if (EstablecerPropiedad(ref _hayInconsistencia, value))
            {
                NotificarEstadoFormulario();
            }
        }
    }

    public bool FormularioHabilitado =>
        _cargaValida && !EstaCargando && !EstaGuardando && !HayInconsistencia;

    public string TituloFormulario => HayInconsistencia
        ? "Configuración no disponible"
        : _empresaConfigurada is null
            ? "Registrar empresa"
            : "Datos de la empresa";

    public string DescripcionFormulario => HayInconsistencia
        ? "La configuración debe revisarse antes de continuar."
        : _empresaConfigurada is null
            ? "Configura la empresa con la que trabajará ContaNexo. Solo el nombre es obligatorio."
            : "Actualiza la información de la empresa configurada. Solo el nombre es obligatorio.";

    public string TextoGuardar
    {
        get
        {
            if (EstaGuardando)
            {
                return _empresaConfigurada is null ? "Registrando..." : "Guardando...";
            }

            return _empresaConfigurada is null ? "Registrar empresa" : "Guardar cambios";
        }
    }

    public ComandoAsync GuardarEmpresaCommand => _guardarEmpresaCommand;

    public async Task CargarAsync()
    {
        EstaCargando = true;
        _cargaValida = false;
        HayInconsistencia = false;
        MensajeError = string.Empty;
        MensajeExito = string.Empty;
        NotificarEstadoFormulario();

        try
        {
            List<Empresa> empresas = (await _repositorioEmpresa.ListarAsync()).ToList();
            _cargaValida = true;

            switch (empresas.Count)
            {
                case 0:
                    EstablecerEmpresaConfigurada(null);
                    _establecerEmpresaActiva(null);
                    break;

                case 1:
                    EstablecerEmpresaConfigurada(empresas[0]);
                    _establecerEmpresaActiva(empresas[0]);
                    break;

                default:
                    EstablecerEmpresaConfigurada(null);
                    _establecerEmpresaActiva(null);
                    HayInconsistencia = true;
                    MensajeError =
                        "Se encontraron varias empresas registradas. La configuración requiere revisión manual antes de continuar.";
                    break;
            }
        }
        catch (Exception excepcion)
        {
            EstablecerEmpresaConfigurada(null);
            MensajeError = ObtenerMensajeUsuario(
                excepcion,
                "No se pudo cargar la configuración de la empresa. Inténtalo nuevamente.");
        }
        finally
        {
            EstaCargando = false;
            NotificarEstadoFormulario();
        }
    }

    private bool PuedeGuardarEmpresa()
    {
        return FormularioHabilitado && !string.IsNullOrWhiteSpace(NombreEmpresa);
    }

    private async Task GuardarEmpresaAsync()
    {
        MensajeError = string.Empty;
        MensajeExito = string.Empty;

        if (!PuedeGuardarEmpresa())
        {
            if (string.IsNullOrWhiteSpace(NombreEmpresa))
            {
                MensajeError = "El nombre de la empresa es obligatorio.";
            }

            return;
        }

        EstaGuardando = true;
        bool estabaRegistrando = _empresaConfigurada is null;

        try
        {
            var empresa = new Empresa
            {
                IdEmpresa = _empresaConfigurada?.IdEmpresa ?? 0,
                NombreEmpresa = NombreEmpresa.Trim(),
                RucEmpresa = NormalizarOpcional(RucEmpresa),
                DireccionEmpresa = NormalizarOpcional(DireccionEmpresa),
                TelefonoEmpresa = NormalizarOpcional(TelefonoEmpresa),
                CorreoEmpresa = NormalizarOpcional(CorreoEmpresa)
            };

            Empresa empresaGuardada = estabaRegistrando
                ? await _repositorioEmpresa.CrearAsync(empresa)
                : await _repositorioEmpresa.ActualizarAsync(empresa);

            EstablecerEmpresaConfigurada(empresaGuardada);
            _establecerEmpresaActiva(empresaGuardada);
            MensajeExito = estabaRegistrando
                ? "La empresa se registró correctamente."
                : "Los cambios se guardaron correctamente.";
        }
        catch (Exception excepcion)
        {
            MensajeError = ObtenerMensajeUsuario(
                excepcion,
                estabaRegistrando
                    ? "No se pudo registrar la empresa. Revisa los datos e inténtalo nuevamente."
                    : "No se pudieron guardar los cambios. Inténtalo nuevamente.");
        }
        finally
        {
            EstaGuardando = false;
        }
    }

    private void EstablecerEmpresaConfigurada(Empresa? empresa)
    {
        _empresaConfigurada = empresa;
        NombreEmpresa = empresa?.NombreEmpresa ?? string.Empty;
        RucEmpresa = empresa?.RucEmpresa ?? string.Empty;
        DireccionEmpresa = empresa?.DireccionEmpresa ?? string.Empty;
        TelefonoEmpresa = empresa?.TelefonoEmpresa ?? string.Empty;
        CorreoEmpresa = empresa?.CorreoEmpresa ?? string.Empty;
        NotificarEstadoFormulario();
    }

    private void NotificarEstadoFormulario()
    {
        NotificarCambio(nameof(FormularioHabilitado));
        NotificarCambio(nameof(TituloFormulario));
        NotificarCambio(nameof(DescripcionFormulario));
        NotificarCambio(nameof(TextoGuardar));
        _guardarEmpresaCommand.NotificarPuedeEjecutar();
    }

    private static string? NormalizarOpcional(string valor)
    {
        return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
    }

    private static string ObtenerMensajeUsuario(Exception excepcion, string mensajeAlternativo)
    {
        return excepcion is InvalidOperationException
            ? excepcion.Message
            : mensajeAlternativo;
    }
}
