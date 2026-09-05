using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;

namespace ContaNexo.Desktop.Converters;

public sealed partial class NormalizarDescripcionConverter : IValueConverter
{
    public object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        if (value is not string texto || string.IsNullOrWhiteSpace(texto))
        {
            return string.Empty;
        }

        string textoNormalizado = texto
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');

        string[] parrafos = SeparacionParrafosRegex().Split(textoNormalizado);

        IEnumerable<string> parrafosNormalizados = parrafos
            .Select(parrafo => SaltoSimpleRegex().Replace(parrafo, " "))
            .Select(parrafo => EspaciosHorizontalesRegex().Replace(parrafo, " ").Trim())
            .Where(parrafo => !string.IsNullOrEmpty(parrafo));

        return string.Join(Environment.NewLine + Environment.NewLine, parrafosNormalizados);
    }

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        return DependencyProperty.UnsetValue;
    }

    [GeneratedRegex(@"\n[ \t]*\n+")]
    private static partial Regex SeparacionParrafosRegex();

    [GeneratedRegex(@"[ \t]*\n[ \t]*")]
    private static partial Regex SaltoSimpleRegex();

    [GeneratedRegex(@"[ \t]+")]
    private static partial Regex EspaciosHorizontalesRegex();
}
