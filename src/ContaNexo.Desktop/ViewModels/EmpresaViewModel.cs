using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ContaNexo.Core.Models;
using ContaNexo.Data.Repositories;
using Microsoft.Win32;

namespace ContaNexo.Desktop.ViewModels;

public sealed class EmpresaViewModel : ViewModelBase
{
    private const long TamanoMaximoLogoBytes = 2 * 1024 * 1024;

    private static readonly HashSet<string> ExtensionesLogoPermitidas =
        new(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg" };

    private readonly RepositorioEmpresa _repositorioEmpresa;
    private readonly Action<Empresa?> _establecerEmpresaActiva;
    private readonly ComandoAsync _guardarEmpresaCommand;
    private readonly ComandoRelay _seleccionarLogoCommand;
    private readonly ComandoRelay _quitarLogoCommand;
    private readonly ComandoRelay _deshacerCambiosCommand;

    private Empresa? _empresaConfigurada;
    private Empresa? _ultimoEstadoGuardado;
    private string _nombreEmpresa = string.Empty;
    private string _rucEmpresa = string.Empty;
    private string _direccionEmpresa = string.Empty;
    private string _telefonoEmpresa = string.Empty;
    private string _correoEmpresa = string.Empty;
    private string _mensajeError = string.Empty;
    private string _mensajeExito = string.Empty;
    private byte[]? _logoEmpresa;
    private ImageSource? _logoVistaPrevia;
    private bool _estaCargando;
    private bool _estaGuardando;
    private bool _cargaValida;
    private bool _hayInconsistencia;
    private bool _hayCambiosPendientes;
    private bool _estaAplicandoEstadoGuardado;

    public EmpresaViewModel(
        RepositorioEmpresa repositorioEmpresa,
        Action<Empresa?> establecerEmpresaActiva)
    {
        _repositorioEmpresa = repositorioEmpresa;
        _establecerEmpresaActiva = establecerEmpresaActiva;
        _guardarEmpresaCommand = new ComandoAsync(GuardarEmpresaAsync, PuedeGuardarEmpresa);
        _seleccionarLogoCommand = new ComandoRelay(SeleccionarLogo, PuedeModificarLogo);
        _quitarLogoCommand = new ComandoRelay(QuitarLogo, PuedeQuitarLogo);
        _deshacerCambiosCommand = new ComandoRelay(DeshacerCambios, PuedeDeshacerCambios);
    }

    public string NombreEmpresa
    {
        get => _nombreEmpresa;
        set
        {
            if (EstablecerPropiedad(ref _nombreEmpresa, value))
            {
                NotificarEdicionFormulario();
            }
        }
    }

    public string RucEmpresa
    {
        get => _rucEmpresa;
        set
        {
            if (EstablecerPropiedad(ref _rucEmpresa, value))
            {
                NotificarEdicionFormulario();
            }
        }
    }

    public string DireccionEmpresa
    {
        get => _direccionEmpresa;
        set
        {
            if (EstablecerPropiedad(ref _direccionEmpresa, value))
            {
                NotificarEdicionFormulario();
            }
        }
    }

    public string TelefonoEmpresa
    {
        get => _telefonoEmpresa;
        set
        {
            if (EstablecerPropiedad(ref _telefonoEmpresa, value))
            {
                NotificarEdicionFormulario();
            }
        }
    }

    public string CorreoEmpresa
    {
        get => _correoEmpresa;
        set
        {
            if (EstablecerPropiedad(ref _correoEmpresa, value))
            {
                NotificarEdicionFormulario();
            }
        }
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

    public byte[]? LogoEmpresa => _logoEmpresa;

    public ImageSource? LogoVistaPrevia
    {
        get => _logoVistaPrevia;
        private set
        {
            if (EstablecerPropiedad(ref _logoVistaPrevia, value))
            {
                NotificarCambio(nameof(TieneVistaPrevia));
                NotificarCambio(nameof(TextoEstadoLogo));
            }
        }
    }

    public bool TieneLogo => LogoEmpresa is { Length: > 0 };

    public bool TieneVistaPrevia => LogoVistaPrevia is not null;

    public string TextoEstadoLogo => TieneLogo
        ? "Vista previa no disponible"
        : "Sin logo";

    public string TextoSeleccionarLogo => TieneLogo
        ? "Cambiar logo"
        : "Seleccionar logo";

    public bool HayCambiosPendientes
    {
        get => _hayCambiosPendientes;
        private set
        {
            if (EstablecerPropiedad(ref _hayCambiosPendientes, value))
            {
                _guardarEmpresaCommand.NotificarPuedeEjecutar();
                _deshacerCambiosCommand.NotificarPuedeEjecutar();
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
                NotificarCambio(nameof(FormularioHabilitado));
                _guardarEmpresaCommand.NotificarPuedeEjecutar();
                _seleccionarLogoCommand.NotificarPuedeEjecutar();
                _quitarLogoCommand.NotificarPuedeEjecutar();
                _deshacerCambiosCommand.NotificarPuedeEjecutar();
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
                _seleccionarLogoCommand.NotificarPuedeEjecutar();
                _quitarLogoCommand.NotificarPuedeEjecutar();
                _deshacerCambiosCommand.NotificarPuedeEjecutar();
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

    public ComandoRelay SeleccionarLogoCommand => _seleccionarLogoCommand;

    public ComandoRelay QuitarLogoCommand => _quitarLogoCommand;

    public ComandoRelay DeshacerCambiosCommand => _deshacerCambiosCommand;

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
        return FormularioHabilitado
            && HayCambiosPendientes
            && !string.IsNullOrWhiteSpace(NombreEmpresa);
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

        bool estabaRegistrando = _empresaConfigurada is null;

        if (!estabaRegistrando)
        {
            MessageBoxResult confirmacion = MessageBox.Show(
                "¿Deseas guardar los cambios realizados en la información de la empresa?",
                "Guardar cambios de la empresa",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);

            if (confirmacion != MessageBoxResult.Yes)
            {
                return;
            }
        }

        EstaGuardando = true;

        try
        {
            var empresa = new Empresa
            {
                IdEmpresa = _empresaConfigurada?.IdEmpresa ?? 0,
                NombreEmpresa = NombreEmpresa.Trim(),
                RucEmpresa = NormalizarOpcional(RucEmpresa),
                DireccionEmpresa = NormalizarOpcional(DireccionEmpresa),
                TelefonoEmpresa = NormalizarOpcional(TelefonoEmpresa),
                CorreoEmpresa = NormalizarOpcional(CorreoEmpresa),
                LogoEmpresa = ClonarLogo(LogoEmpresa)
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
        _ultimoEstadoGuardado = CopiarEmpresa(empresa);
        AplicarEstadoAlFormulario(_ultimoEstadoGuardado);
        NotificarEstadoFormulario();
    }

    private void AplicarEstadoAlFormulario(Empresa? empresa)
    {
        _estaAplicandoEstadoGuardado = true;

        try
        {
            NombreEmpresa = empresa?.NombreEmpresa ?? string.Empty;
            RucEmpresa = empresa?.RucEmpresa ?? string.Empty;
            DireccionEmpresa = empresa?.DireccionEmpresa ?? string.Empty;
            TelefonoEmpresa = empresa?.TelefonoEmpresa ?? string.Empty;
            CorreoEmpresa = empresa?.CorreoEmpresa ?? string.Empty;
            CargarLogo(ClonarLogo(empresa?.LogoEmpresa));
        }
        finally
        {
            _estaAplicandoEstadoGuardado = false;
        }

        ActualizarCambiosPendientes();
    }

    private bool PuedeModificarLogo()
    {
        return FormularioHabilitado;
    }

    private bool PuedeQuitarLogo()
    {
        return FormularioHabilitado && TieneLogo;
    }

    private bool PuedeDeshacerCambios()
    {
        return FormularioHabilitado && HayCambiosPendientes;
    }

    private void DeshacerCambios()
    {
        MensajeError = string.Empty;
        MensajeExito = string.Empty;
        AplicarEstadoAlFormulario(_ultimoEstadoGuardado);
    }

    private void SeleccionarLogo()
    {
        MensajeError = string.Empty;
        MensajeExito = string.Empty;

        var selector = new OpenFileDialog
        {
            Title = "Seleccionar logo de la empresa",
            Filter = "Imágenes PNG o JPG (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg",
            CheckFileExists = true,
            Multiselect = false
        };

        if (selector.ShowDialog() != true)
        {
            return;
        }

        try
        {
            string rutaArchivo = selector.FileName;

            if (!File.Exists(rutaArchivo))
            {
                MensajeError = "El archivo seleccionado ya no existe.";
                return;
            }

            if (!ExtensionesLogoPermitidas.Contains(Path.GetExtension(rutaArchivo)))
            {
                MensajeError = "Selecciona una imagen PNG o JPG.";
                return;
            }

            var informacionArchivo = new FileInfo(rutaArchivo);

            if (informacionArchivo.Length > TamanoMaximoLogoBytes)
            {
                MensajeError = "El archivo seleccionado supera el límite de 2 MB.";
                return;
            }

            byte[] contenidoLogo = File.ReadAllBytes(rutaArchivo);

            if (contenidoLogo.Length == 0 || contenidoLogo.LongLength > TamanoMaximoLogoBytes)
            {
                MensajeError = contenidoLogo.Length == 0
                    ? "El formato de imagen no es válido."
                    : "El archivo seleccionado supera el límite de 2 MB.";
                return;
            }

            if (!EsFormatoCompatibleConExtension(contenidoLogo, Path.GetExtension(rutaArchivo)))
            {
                MensajeError = "El formato de imagen no es válido. Selecciona una imagen PNG o JPG.";
                return;
            }

            ImageSource vistaPrevia = CrearVistaPrevia(contenidoLogo);
            EstablecerLogo(contenidoLogo, vistaPrevia);
            MensajeExito = LogosIguales(LogoEmpresa, _ultimoEstadoGuardado?.LogoEmpresa)
                ? string.Empty
                : "Logo seleccionado. Guarda los cambios para conservarlo.";
        }
        catch (Exception excepcion) when (EsErrorDeArchivoOImagen(excepcion))
        {
            MensajeError = excepcion is UnauthorizedAccessException
                ? "No se pudo leer el archivo seleccionado. Verifica sus permisos."
                : "El formato de imagen no es válido.";
        }
    }

    private void QuitarLogo()
    {
        MensajeError = string.Empty;
        bool habiaLogoGuardado = _ultimoEstadoGuardado?.LogoEmpresa is { Length: > 0 };
        EstablecerLogo(null, null);
        MensajeExito = habiaLogoGuardado
            ? "El logo se quitará al guardar los cambios."
            : string.Empty;
    }

    private void CargarLogo(byte[]? contenidoLogo)
    {
        if (contenidoLogo is not { Length: > 0 })
        {
            EstablecerLogo(null, null);
            return;
        }

        try
        {
            EstablecerLogo(contenidoLogo, CrearVistaPrevia(contenidoLogo));
        }
        catch (Exception excepcion) when (EsErrorDeArchivoOImagen(excepcion))
        {
            EstablecerLogo(contenidoLogo, null);
            MensajeError = "No se pudo mostrar el logo guardado. Puedes reemplazarlo o quitarlo.";
        }
    }

    private void EstablecerLogo(byte[]? contenidoLogo, ImageSource? vistaPrevia)
    {
        _logoEmpresa = contenidoLogo;
        LogoVistaPrevia = vistaPrevia;
        NotificarCambio(nameof(LogoEmpresa));
        NotificarCambio(nameof(TieneLogo));
        NotificarCambio(nameof(TextoEstadoLogo));
        NotificarCambio(nameof(TextoSeleccionarLogo));
        _quitarLogoCommand.NotificarPuedeEjecutar();
        NotificarEdicionFormulario();
    }

    private static ImageSource CrearVistaPrevia(byte[] contenidoLogo)
    {
        using var flujo = new MemoryStream(contenidoLogo, writable: false);
        var imagen = new BitmapImage();
        imagen.BeginInit();
        imagen.CacheOption = BitmapCacheOption.OnLoad;
        imagen.DecodePixelWidth = 480;
        imagen.StreamSource = flujo;
        imagen.EndInit();
        imagen.Freeze();
        return imagen;
    }

    private static bool EsErrorDeArchivoOImagen(Exception excepcion)
    {
        return excepcion is IOException
            or UnauthorizedAccessException
            or ArgumentException
            or InvalidOperationException
            or NotSupportedException
            or System.Security.SecurityException;
    }

    private static bool EsFormatoCompatibleConExtension(byte[] contenidoLogo, string extension)
    {
        bool esPng = contenidoLogo.Length >= 8
            && contenidoLogo[0] == 0x89
            && contenidoLogo[1] == 0x50
            && contenidoLogo[2] == 0x4E
            && contenidoLogo[3] == 0x47
            && contenidoLogo[4] == 0x0D
            && contenidoLogo[5] == 0x0A
            && contenidoLogo[6] == 0x1A
            && contenidoLogo[7] == 0x0A;

        bool esJpeg = contenidoLogo.Length >= 3
            && contenidoLogo[0] == 0xFF
            && contenidoLogo[1] == 0xD8
            && contenidoLogo[2] == 0xFF;

        return extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
            ? esPng
            : esJpeg;
    }

    private void NotificarEdicionFormulario()
    {
        if (_estaAplicandoEstadoGuardado)
        {
            return;
        }

        MensajeExito = string.Empty;
        ActualizarCambiosPendientes();
    }

    private void ActualizarCambiosPendientes()
    {
        HayCambiosPendientes = !CoincideConUltimoEstadoGuardado();
        _guardarEmpresaCommand.NotificarPuedeEjecutar();
        _deshacerCambiosCommand.NotificarPuedeEjecutar();
    }

    private bool CoincideConUltimoEstadoGuardado()
    {
        string nombreGuardado = _ultimoEstadoGuardado?.NombreEmpresa ?? string.Empty;

        return string.Equals(
                NombreEmpresa.Trim(),
                nombreGuardado.Trim(),
                StringComparison.Ordinal)
            && string.Equals(
                NormalizarOpcional(RucEmpresa),
                NormalizarOpcional(_ultimoEstadoGuardado?.RucEmpresa ?? string.Empty),
                StringComparison.Ordinal)
            && string.Equals(
                NormalizarOpcional(DireccionEmpresa),
                NormalizarOpcional(_ultimoEstadoGuardado?.DireccionEmpresa ?? string.Empty),
                StringComparison.Ordinal)
            && string.Equals(
                NormalizarOpcional(TelefonoEmpresa),
                NormalizarOpcional(_ultimoEstadoGuardado?.TelefonoEmpresa ?? string.Empty),
                StringComparison.Ordinal)
            && string.Equals(
                NormalizarOpcional(CorreoEmpresa),
                NormalizarOpcional(_ultimoEstadoGuardado?.CorreoEmpresa ?? string.Empty),
                StringComparison.Ordinal)
            && LogosIguales(LogoEmpresa, _ultimoEstadoGuardado?.LogoEmpresa);
    }

    private static Empresa? CopiarEmpresa(Empresa? empresa)
    {
        return empresa is null
            ? null
            : new Empresa
            {
                IdEmpresa = empresa.IdEmpresa,
                NombreEmpresa = empresa.NombreEmpresa,
                RucEmpresa = empresa.RucEmpresa,
                DireccionEmpresa = empresa.DireccionEmpresa,
                TelefonoEmpresa = empresa.TelefonoEmpresa,
                CorreoEmpresa = empresa.CorreoEmpresa,
                LogoEmpresa = ClonarLogo(empresa.LogoEmpresa)
            };
    }

    private static byte[]? ClonarLogo(byte[]? logo)
    {
        return logo?.ToArray();
    }

    private static bool LogosIguales(byte[]? primerLogo, byte[]? segundoLogo)
    {
        bool primerLogoVacio = primerLogo is not { Length: > 0 };
        bool segundoLogoVacio = segundoLogo is not { Length: > 0 };

        if (primerLogoVacio || segundoLogoVacio)
        {
            return primerLogoVacio && segundoLogoVacio;
        }

        return primerLogo.AsSpan().SequenceEqual(segundoLogo);
    }

    private void NotificarEstadoFormulario()
    {
        NotificarCambio(nameof(FormularioHabilitado));
        NotificarCambio(nameof(TituloFormulario));
        NotificarCambio(nameof(DescripcionFormulario));
        NotificarCambio(nameof(TextoGuardar));
        _guardarEmpresaCommand.NotificarPuedeEjecutar();
        _seleccionarLogoCommand.NotificarPuedeEjecutar();
        _quitarLogoCommand.NotificarPuedeEjecutar();
        _deshacerCambiosCommand.NotificarPuedeEjecutar();
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
