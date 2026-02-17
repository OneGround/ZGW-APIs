namespace OneGround.ZGW.Common.Handlers;

public enum CommandStatus
{
    Unspecified = HanderStatusCodes.Unspecified,
    OK = HanderStatusCodes.OK,
    ValidationError = HanderStatusCodes.ValidationError,
    NotFound = HanderStatusCodes.NotFound,
    Forbidden = HanderStatusCodes.Forbidden,
    Failed = HanderStatusCodes.Failed,
    Conflict = HanderStatusCodes.Conflict,
}
