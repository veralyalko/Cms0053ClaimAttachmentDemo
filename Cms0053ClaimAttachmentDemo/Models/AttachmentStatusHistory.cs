namespace Cms0053ClaimAttachmentDemo.Models;

public class AttachmentStatusHistory
{
    public int Id { get; set; }
    public int ClaimAttachmentId { get; set; }
    public ClaimAttachment ClaimAttachment { get; set; } = null!;
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = "";
    public string? Notes { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public string ChangedBy { get; set; } = "System";
}
