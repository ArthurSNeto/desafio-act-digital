namespace Library.Application.Telemetry;

public interface ILibraryMetrics
{
    void IncrementLoansCreated();
    void IncrementUnavailableRejection();
    void IncrementIdempotentHit();
    void RecordLoanCreationLatency(double milliseconds);
}
