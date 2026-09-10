namespace ContaNexo.Core.Models;

public sealed class BalanceGeneralResumen
{
    public BalanceGeneralGrupo ActivoCorriente { get; set; } = new();

    public BalanceGeneralGrupo ActivoNoCorriente { get; set; } = new();

    public BalanceGeneralGrupo PasivoCorriente { get; set; } = new();

    public BalanceGeneralGrupo PasivoNoCorriente { get; set; } = new();

    public BalanceGeneralGrupo CapitalContable { get; set; } = new();

    public BalanceGeneralGrupo ResultadosReservas { get; set; } = new();

    public decimal TasaParticipacionTrabajadores { get; set; }

    public decimal ParticipacionTrabajadores { get; set; }

    public decimal TasaImpuestoRenta { get; set; }

    public decimal ImpuestoRenta { get; set; }

    public decimal ResultadoNeto { get; set; }

    public decimal TotalPasivoCorriente { get; set; }

    public decimal TotalActivo { get; set; }

    public decimal TotalPasivo { get; set; }

    public decimal TotalPatrimonio { get; set; }

    public decimal TotalPasivoPatrimonio { get; set; }

    public decimal Diferencia { get; set; }

    public int CantidadSaldosContrarios { get; set; }

    public bool TieneSaldosContrarios => CantidadSaldosContrarios > 0;

    public int CantidadCuentasResultadoEjercicioRegistradas { get; set; }

    public bool TieneCuentasResultadoEjercicioRegistradas =>
        CantidadCuentasResultadoEjercicioRegistradas > 0;

    public bool EstaCuadrado => Diferencia == 0m;
}
