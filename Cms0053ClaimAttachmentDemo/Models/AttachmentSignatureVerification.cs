namespace Cms0053ClaimAttachmentDemo.Models;

public class AttachmentSignatureVerification
{
    public int Id { get; set; }
    public int ClaimAttachmentId { get; set; }
    public ClaimAttachment ClaimAttachment { get; set; } = null!;

    public bool IsVerified { get; set; }
    public string Algorithm { get; set; } = "";
    public string? CertificateSubject { get; set; }
    public string? CertificateThumbprint { get; set; }
    public DateTime? CertificateValidFrom { get; set; }
    public DateTime? CertificateValidTo { get; set; }
    public string? FailureReason { get; set; }
    public DateTime VerifiedAt { get; set; } = DateTime.UtcNow;
}
