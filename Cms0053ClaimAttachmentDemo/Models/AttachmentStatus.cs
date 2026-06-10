namespace Cms0053ClaimAttachmentDemo.Models;

public static class AttachmentStatus
{
    public const string Received = "Received";
    public const string Processing = "Processing";
    public const string Validated = "Validated";
    public const string ValidationFailed = "ValidationFailed";
    public const string Matched = "Matched";
    public const string MatchFailed = "MatchFailed";
    public const string Accepted = "Accepted";
    public const string Rejected = "Rejected";
}

public static class AttachmentRequestStatus
{
    public const string Pending = "Pending";
    public const string Fulfilled = "Fulfilled";
    public const string Expired = "Expired";
    public const string Cancelled = "Cancelled";
}

public static class ClearinghouseAttachmentStatus
{
    public const string New = "New";
    public const string Pulled = "Pulled";
}
