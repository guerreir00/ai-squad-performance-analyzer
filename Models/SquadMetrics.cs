namespace SquadIA.Models;

public class SquadMetrics
{
    public string NomeSquad { get; set; } = string.Empty;
    public int LeadTimeMedio { get; set; }
    public int Throughput { get; set; }
    public int Bugs { get; set; }
    public int Bloqueios { get; set; }
}