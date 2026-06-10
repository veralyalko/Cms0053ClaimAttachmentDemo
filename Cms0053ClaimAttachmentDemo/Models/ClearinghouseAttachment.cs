namespace Cms0053ClaimAttachmentDemo.Models;

public class ClearinghouseAttachment
{
    public int Id { get; set; }
    public string TrackingNumber { get; set; } = "";
    public string ProviderNPI { get; set; } = "";
    public string PatientName { get; set; } = "";
    public DateOnly? PatientDOB { get; set; }
    public DateOnly ServiceDate { get; set; }
    public string DocumentType { get; set; } = "";
    public string FileName { get; set; } = "";
    public string StoredFileName { get; set; } = "";
    public long FileSizeBytes { get; set; }
    public string ContentType { get; set; } = "";
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = ClearinghouseAttachmentStatus.New;
    public DateTime? PulledAt { get; set; }
}
