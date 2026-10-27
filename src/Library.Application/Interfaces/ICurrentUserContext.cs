namespace Library.Application.Interfaces;

public interface ICurrentUserContext
{
    string CorrelationId { get; }
    string CurrentUser { get; }
}
