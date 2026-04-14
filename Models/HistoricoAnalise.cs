namespace SquadIA.Models;

public class HistoricoAnalise
{
    public int Id { get; set; }
    public string NomeSquad { get; set; } = string.Empty;
    public int LeadTimeMedio { get; set; }
    public int Throughput { get; set; }
    public int Bugs { get; set; }
    public int Bloqueios { get; set; }
    public string Diagnostico { get; set; } = string.Empty;
    public string Prioridade { get; set; } = string.Empty;
    public string ResumoExecutivo { get; set; } = string.Empty;
    public int ScoreSaude { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}