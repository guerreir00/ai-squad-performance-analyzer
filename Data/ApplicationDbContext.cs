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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<HistoricoAnalise>(entity =>
        {
            entity.HasIndex(h => h.NomeSquad);
            entity.HasIndex(h => h.CriadoEm);

            entity.Property(h => h.NomeSquad)
                .HasMaxLength(120)
                .IsRequired();

            entity.Property(h => h.Prioridade)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(h => h.Diagnostico)
                .IsRequired();

            entity.Property(h => h.ResumoExecutivo)
                .IsRequired();
        });
    }
}