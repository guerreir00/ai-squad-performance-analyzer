namespace SquadIA.Models;

public class DashboardResumo
{
    public int TotalAnalises { get; set; }
    public double MediaScoreSaude { get; set; }
    public int PrioridadeAlta { get; set; }
    public int PrioridadeMedia { get; set; }
    public int PrioridadeBaixa { get; set; }
    public string? UltimaSquadAnalisada { get; set; }
    public DateTime? UltimaAnaliseEm { get; set; }
}