namespace OneGround.ZGW.Common.Handlers;

public enum QueryStatus
{
    Unspecified = HanderStatusCodes.Unspecified,
    OK = HanderStatusCodes.OK,
    ValidationError = HanderStatusCodes.ValidationError,
    NotFound = HanderStatusCodes.NotFound,
    Failed = HanderStatusCodes.Failed,
    Forbidden = HanderStatusCodes.Forbidden,
}
