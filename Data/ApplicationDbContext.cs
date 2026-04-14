using Microsoft.EntityFrameworkCore;
using SquadIA.Models;

namespace SquadIA.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<HistoricoAnalise> HistoricosAnalise { get; set; }
}