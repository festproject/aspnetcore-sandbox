namespace PageState;

public sealed class PageStateTooLargeException : Exception
{
    public PageStateTooLargeException(Type stateType, int payloadBytes, int maxPayloadBytes)
        : base($"PageState payload for '{stateType.FullName}' was {payloadBytes} bytes, " +
               $"exceeding the configured limit of {maxPayloadBytes} bytes " +
               $"(PageStateOptions.MaxPayloadBytes). Reduce the state carried by this type.")
    {
        StateType = stateType;
        PayloadBytes = payloadBytes;
        MaxPayloadBytes = maxPayloadBytes;
    }

    public Type StateType { get; }
    public int PayloadBytes { get; }
    public int MaxPayloadBytes { get; }
}
