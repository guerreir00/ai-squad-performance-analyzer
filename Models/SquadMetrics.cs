using System.ComponentModel.DataAnnotations;

namespace SquadIA.Models;

public class SquadMetrics
{
    [Required(ErrorMessage = "NomeSquad é obrigatório.")]
    [StringLength(120, ErrorMessage = "NomeSquad deve ter no máximo 120 caracteres.")]
    public string NomeSquad { get; set; } = string.Empty;

    [Range(0, 10000, ErrorMessage = "LeadTimeMedio deve ser entre 0 e 10000.")]
    public int LeadTimeMedio { get; set; }

    [Range(0, 10000, ErrorMessage = "Throughput deve ser entre 0 e 10000.")]
    public int Throughput { get; set; }

    [Range(0, 100000, ErrorMessage = "Bugs deve ser entre 0 e 100000.")]
    public int Bugs { get; set; }

    [Range(0, 100000, ErrorMessage = "Bloqueios deve ser entre 0 e 100000.")]
    public int Bloqueios { get; set; }
}