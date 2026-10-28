using System.Diagnostics;

namespace Library.Application.Telemetry;

public static class LibraryTelemetry
{
    public const string ActivitySourceName = "Library.Application.Loans";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
}
