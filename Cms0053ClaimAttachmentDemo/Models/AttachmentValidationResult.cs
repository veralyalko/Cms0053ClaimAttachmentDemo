namespace Cms0053ClaimAttachmentDemo.Models;

public class AttachmentValidationResult
{
    public int Id { get; set; }
    public int ClaimAttachmentId { get; set; }
    public ClaimAttachment ClaimAttachment { get; set; } = null!;
    public bool IsValid { get; set; }
    public string Errors { get; set; } = "[]";
    public string Warnings { get; set; } = "[]";
    public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
}
