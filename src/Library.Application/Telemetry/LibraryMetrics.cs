using System.Diagnostics.Metrics;

namespace Library.Application.Telemetry;

public sealed class LibraryMetrics : ILibraryMetrics
{
    public const string MeterName = "Library.API.Metrics";

    private readonly Counter<long> _loansCreatedCounter;
    private readonly Counter<long> _stockUnavailableCounter;
    private readonly Counter<long> _idempotentHitsCounter;
    private readonly Histogram<double> _loanCreationDuration;

    public LibraryMetrics(IMeterFactory? meterFactory = null)
    {
        Meter meter = meterFactory?.Create(MeterName) ?? new Meter(MeterName);

        _loansCreatedCounter = meter.CreateCounter<long>(
            name: "library.loans.created",
            unit: "{loans}",
            description: "Total de empréstimos criados com sucesso.");

        _stockUnavailableCounter = meter.CreateCounter<long>(
            name: "library.loans.rejected.unavailable",
            unit: "{rejections}",
            description: "Tentativas de empréstimo rejeitadas por falta de exemplares disponíveis.");

        _idempotentHitsCounter = meter.CreateCounter<long>(
            name: "library.loans.idempotent.hits",
            unit: "{hits}",
            description: "Requisições repetidas atendidas pelo mecanismo de idempotência.");

        _loanCreationDuration = meter.CreateHistogram<double>(
            name: "library.loans.creation.duration",
            unit: "ms",
            description: "Duração do processamento de criação de empréstimo em milissegundos.");

        _loansCreatedCounter.Add(0);
        _stockUnavailableCounter.Add(0);
        _idempotentHitsCounter.Add(0);
    }

    public void IncrementLoansCreated()
    {
        _loansCreatedCounter.Add(1);
    }

    public void IncrementUnavailableRejection()
    {
        _stockUnavailableCounter.Add(1);
    }

    public void IncrementIdempotentHit()
    {
        _idempotentHitsCounter.Add(1);
    }

    public void RecordLoanCreationLatency(double milliseconds)
    {
        _loanCreationDuration.Record(milliseconds);
    }
}
