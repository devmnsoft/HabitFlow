namespace HabitFlow.Application;

public abstract class HabitFlowException(string code, string safeMessage, int statusCode, Exception? inner = null) : Exception(safeMessage, inner)
{
    public string Code { get; } = code;
    public string SafeMessage { get; } = safeMessage;
    public int StatusCode { get; } = statusCode;
}

public sealed class RequestValidationException(string message) : HabitFlowException("validation.failed", message, 422);
public sealed class BusinessRuleException(string message) : HabitFlowException("business.rule", message, 409);
public sealed class ResourceNotFoundException(string message) : HabitFlowException("resource.not_found", message, 404);
public sealed class AuthenticationRequiredException(string message) : HabitFlowException("authentication.required", message, 401);
public sealed class AuthorizationDeniedException(string message) : HabitFlowException("authorization.denied", message, 403);
public sealed class IntegrationUnavailableException(string message, Exception? inner = null) : HabitFlowException("integration.unavailable", message, 503, inner);
public sealed class DatabaseUnavailableException(string message, Exception? inner = null) : HabitFlowException("database.unavailable", message, 503, inner);
