namespace Cms0053ClaimAttachmentDemo.Models;

public class AttachmentRequest
{
    public int Id { get; set; }
    public int ClaimId { get; set; }
    public Claim Claim { get; set; } = null!;
    public string TrackingNumber { get; set; } = "";
    public string DocumentTypeRequested { get; set; } = "";
    public string RequestReason { get; set; } = "";
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime DueDate { get; set; }
    public string ProviderEmail { get; set; } = "";
    public string SecureUploadToken { get; set; } = "";
    public string Status { get; set; } = AttachmentRequestStatus.Pending;
}
