namespace Cms0053ClaimAttachmentDemo.Models;

public class EmrDocument
{
    public int Id { get; set; }
    public string DocumentName { get; set; } = "";
    public string DocumentType { get; set; } = "";
    public string PatientName { get; set; } = "";
    public DateOnly PatientDOB { get; set; }
    public string ProviderNPI { get; set; } = "";
    public DateOnly ServiceDate { get; set; }
    public string FileName { get; set; } = "";
}
