namespace ContaNexo.Core.Models;

public class EstadoResultadosResumen
{
    public IReadOnlyList<EstadoResultadosGrupo> GruposIngresos { get; set; } = [];

    public IReadOnlyList<EstadoResultadosGrupo> GruposGastos { get; set; } = [];

    public decimal TotalIngresos { get; set; }

    public decimal TotalGastos { get; set; }

    public decimal ResultadoPeriodo { get; set; }

    public decimal TasaParticipacionTrabajadores { get; set; }

    public decimal ParticipacionTrabajadores { get; set; }

    public decimal ResultadoAntesImpuestoRenta { get; set; }

    public decimal TasaImpuestoRenta { get; set; }

    public decimal ImpuestoRenta { get; set; }

    public decimal ResultadoNeto { get; set; }

    public int CantidadSaldosContrarios { get; set; }

    public bool TieneSaldosContrarios => CantidadSaldosContrarios > 0;
}
