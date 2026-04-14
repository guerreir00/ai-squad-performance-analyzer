namespace SquadIA.Models;

public class AnaliseResultado
{
    public string Diagnostico { get; set; } = string.Empty;
    public List<string> Problemas { get; set; } = new();
    public List<string> Acoes { get; set; } = new();
    public string Prioridade { get; set; } = string.Empty;
    public string ResumoExecutivo { get; set; } = string.Empty;
    public int ScoreSaude { get; set; }
}