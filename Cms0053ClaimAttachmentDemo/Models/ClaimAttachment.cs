namespace Cms0053ClaimAttachmentDemo.Models;

public class ClaimAttachment
{
    public int Id { get; set; }
    public int? ClaimId { get; set; }
    public Claim? Claim { get; set; }
    public int? AttachmentRequestId { get; set; }
    public AttachmentRequest? AttachmentRequest { get; set; }
    public string SourceType { get; set; } = "";
    public string FileName { get; set; } = "";
    public string StoredFileName { get; set; } = "";
    public long FileSizeBytes { get; set; }
    public string ContentType { get; set; } = "";
    public string FileHash { get; set; } = "";
    public string SubmittedBy { get; set; } = "";
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public string PatientName { get; set; } = "";
    public DateOnly? PatientDOB { get; set; }
    public string ProviderNPI { get; set; } = "";
    public DateOnly ServiceDate { get; set; }
    public string DocumentType { get; set; } = "";
    public string? Notes { get; set; }
    public string Status { get; set; } = AttachmentStatus.Received;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public AttachmentValidationResult? ValidationResult { get; set; }
    public ICollection<AttachmentStatusHistory> StatusHistory { get; set; } = [];
}
