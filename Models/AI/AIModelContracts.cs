namespace Manage_KPI_or_OKR_System.Models.AI;

/// <summary>
/// A transient message sent to an AI provider. This type is not an EF entity and must not be persisted.
/// Callers are responsible for supplying only data authorized for the current user.
/// </summary>
public sealed record AIModelMessage(string Role, string Content)
{
    public void Validate()
    {
        if (Role is not ("system" or "user" or "assistant" or "tool"))
        {
            throw new ArgumentException("Message role is invalid.", nameof(Role));
        }

        if (string.IsNullOrWhiteSpace(Content))
        {
            throw new ArgumentException("Message content is required.", nameof(Content));
        }
    }
}

public sealed record AIModelToolDefinition(string Name, string Description, string JsonSchema)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name) || Name.Length > 64)
        {
            throw new ArgumentException("Tool name is required and must be 64 characters or fewer.", nameof(Name));
        }

        if (string.IsNullOrWhiteSpace(Description))
        {
            throw new ArgumentException("Tool description is required.", nameof(Description));
        }

        using var document = System.Text.Json.JsonDocument.Parse(JsonSchema);
        if (document.RootElement.ValueKind != System.Text.Json.JsonValueKind.Object)
        {
            throw new ArgumentException("Tool JSON schema must be an object.", nameof(JsonSchema));
        }
    }
}

public sealed record AIModelRequest(
    IReadOnlyList<AIModelMessage> Messages,
    IReadOnlyList<AIModelToolDefinition>? Tools = null,
    double Temperature = 0)
{
    public void Validate()
    {
        if (Messages is null || Messages.Count == 0)
        {
            throw new ArgumentException("At least one message is required.", nameof(Messages));
        }

        if (Temperature is < 0 or > 2)
        {
            throw new ArgumentOutOfRangeException(nameof(Temperature));
        }

        foreach (var message in Messages)
        {
            message.Validate();
        }

        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var tool in Tools ?? Array.Empty<AIModelToolDefinition>())
        {
            tool.Validate();
            if (!names.Add(tool.Name))
            {
                throw new ArgumentException("Tool names must be unique.", nameof(Tools));
            }
        }
    }
}

public sealed record AIModelToolCall(string Id, string Name, System.Text.Json.JsonElement Arguments);

public sealed record AIModelResponse(string? Content, IReadOnlyList<AIModelToolCall> ToolCalls)
{
    public bool HasContent => !string.IsNullOrWhiteSpace(Content);
}

public interface IAIModelClient
{
    Task<AIModelResponse> CompleteAsync(AIModelRequest request, CancellationToken cancellationToken = default);
}

public sealed class AIModelResponseValidationException : Exception
{
    public AIModelResponseValidationException(string message) : base(message) { }
}
