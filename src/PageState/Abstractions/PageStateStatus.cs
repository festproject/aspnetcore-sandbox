namespace PageState;

public enum PageStateStatus
{
    Success,
    Missing,
    TooLarge,
    InvalidToken,
    Expired,
    WrongOwner,
    InvalidEnvelope,
    InvalidSchema,
    InvalidPayload
}
