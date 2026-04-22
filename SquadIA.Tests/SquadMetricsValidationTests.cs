using System.ComponentModel.DataAnnotations;
using SquadIA.Models;
using Xunit;

namespace SquadIA.Tests;

public class SquadMetricsValidationTests
{
    [Fact]
    public void Deve_ser_valido_quando_metricas_estao_no_intervalo_correto()
    {
        var model = new SquadMetrics
        {
            NomeSquad = "Payments Squad",
            LeadTimeMedio = 50,
            Throughput = 10,
            Bugs = 5,
            Bloqueios = 2
        };

        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(
            model,
            new ValidationContext(model),
            validationResults,
            validateAllProperties: true);

        Assert.True(isValid);
        Assert.Empty(validationResults);
    }

    [Fact]
    public void Deve_ser_invalido_quando_nomeSquad_esta_vazio()
    {
        var model = new SquadMetrics
        {
            NomeSquad = "",
            LeadTimeMedio = 50,
            Throughput = 10,
            Bugs = 5,
            Bloqueios = 2
        };

        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(
            model,
            new ValidationContext(model),
            validationResults,
            validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(validationResults, r => r.MemberNames.Contains(nameof(SquadMetrics.NomeSquad)));
    }

    [Fact]
    public void Deve_ser_invalido_quando_leadTime_e_negativo()
    {
        var model = new SquadMetrics
        {
            NomeSquad = "Payments Squad",
            LeadTimeMedio = -1,
            Throughput = 10,
            Bugs = 5,
            Bloqueios = 2
        };

        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(
            model,
            new ValidationContext(model),
            validationResults,
            validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(validationResults, r => r.MemberNames.Contains(nameof(SquadMetrics.LeadTimeMedio)));
    }

    [Fact]
    public void Deve_ser_invalido_quando_throughput_e_negativo()
    {
        var model = new SquadMetrics
        {
            NomeSquad = "Payments Squad",
            LeadTimeMedio = 10,
            Throughput = -2,
            Bugs = 5,
            Bloqueios = 2
        };

        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(
            model,
            new ValidationContext(model),
            validationResults,
            validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(validationResults, r => r.MemberNames.Contains(nameof(SquadMetrics.Throughput)));
    }
}