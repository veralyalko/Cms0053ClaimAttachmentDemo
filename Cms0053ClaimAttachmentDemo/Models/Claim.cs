namespace Cms0053ClaimAttachmentDemo.Models;

public class Claim
{
    public int Id { get; set; }
    public string ClaimNumber { get; set; } = "";
    public string PatientName { get; set; } = "";
    public DateOnly PatientDOB { get; set; }
    public string ProviderNPI { get; set; } = "";
    public string ProviderName { get; set; } = "";
    public DateOnly ServiceDate { get; set; }
    public string DiagnosisCode { get; set; } = "";
    public decimal AmountBilled { get; set; }
    public string Status { get; set; } = "Open";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
