using System.ComponentModel.DataAnnotations;
using Cms0053ClaimAttachmentDemo.Models;
using Cms0053ClaimAttachmentDemo.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Cms0053ClaimAttachmentDemo.Pages.Clearinghouse;

public class SubmitModel(MockClearinghouseService clearinghouse) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public ClearinghouseAttachment? Submitted { get; set; }

    public static readonly List<string> DocumentTypes =
    [
        "Operative Report", "Lab Results", "Radiology Report",
        "Office Visit Notes", "Discharge Summary", "Physical Therapy Notes", "Other"
    ];

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        // [X12-275-CLEARINGHOUSE-PLACEHOLDER] In production, the provider system would transmit
        // an X12 275 transaction to the real clearinghouse instead of posting this form.
        Submitted = await clearinghouse.SubmitAsync(
            file:        Input.File!,
            providerNPI: Input.ProviderNPI,
            patientName: Input.PatientName,
            patientDOB:  Input.PatientDOB,
            serviceDate: Input.ServiceDate,
            documentType: Input.DocumentType
        );

        return Page();
    }

    public class InputModel
    {
        [Required(ErrorMessage = "Provider NPI is required.")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "NPI must be exactly 10 digits.")]
        public string ProviderNPI { get; set; } = "";

        [Required(ErrorMessage = "Patient name is required.")]
        public string PatientName { get; set; } = "";

        public DateOnly? PatientDOB { get; set; }

        [Required(ErrorMessage = "Service date is required.")]
        public DateOnly ServiceDate { get; set; }

        [Required(ErrorMessage = "Document type is required.")]
        public string DocumentType { get; set; } = "";

        [Required(ErrorMessage = "Please select a file to upload.")]
        public IFormFile? File { get; set; }
    }
}
