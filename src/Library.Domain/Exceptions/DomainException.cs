namespace Library.Domain.Exceptions;

public abstract class DomainException(string message) : Exception(message);

public class EntityNotFoundException(string entityName, object key)
    : DomainException($"'{entityName}' com identificador '{key}' não foi encontrado.");

public class BusinessRuleException(string message)
    : DomainException(message);

public class ConcurrencyConflictException(string message = "O recurso foi modificado simultaneamente por outra transação. Tente novamente.")
    : DomainException(message);

public class ValidationException(IDictionary<string, string[]> errors)
    : DomainException("Um ou mais erros de validação ocorreram.")
{
    public IDictionary<string, string[]> Errors { get; } = errors;
}
