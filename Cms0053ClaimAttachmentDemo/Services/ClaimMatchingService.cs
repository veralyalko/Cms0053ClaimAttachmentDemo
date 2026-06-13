using Cms0053ClaimAttachmentDemo.Data;
using Cms0053ClaimAttachmentDemo.Models;
using Microsoft.EntityFrameworkCore;

namespace Cms0053ClaimAttachmentDemo.Services;

public record ClaimMatchResult(Claim? Claim, List<string> Warnings);

public class ClaimMatchingService(AppDbContext db)
{
    // [X12-837-LINKAGE-PLACEHOLDER] In production, use the X12 837 ICN (Internal Control Number)
    // from the original claim transaction for exact matching instead of name + NPI + date proximity.
    public async Task<ClaimMatchResult> MatchAsync(
        string providerNPI, string patientName, DateOnly serviceDate, DateOnly? patientDOB)
    {
        var warnings = new List<string>();
        var normalized = patientName.Trim().ToLower();

        var candidates = await db.Claims
            .Where(c => c.ProviderNPI == providerNPI)
            .ToListAsync();

        var matches = candidates
            .Where(c => c.PatientName.Trim().ToLower() == normalized)
            .Where(c => Math.Abs(c.ServiceDate.DayNumber - serviceDate.DayNumber) <= 7)
            .OrderBy(c => Math.Abs(c.ServiceDate.DayNumber - serviceDate.DayNumber))
            .ToList();

        if (matches.Count == 0)
            return new ClaimMatchResult(null, warnings);

        if (matches.Count > 1)
        {
            warnings.Add($"Multiple claims matched NPI + patient + service date (±7 days). " +
                         $"Closest service date selected. Manual verification recommended.");
        }

        // PatientDOB tiebreaker: if provided and exactly one candidate matches on DOB, prefer it.
        if (patientDOB.HasValue && matches.Count > 1)
        {
            var dobMatch = matches.FirstOrDefault(c => c.PatientDOB == patientDOB.Value);
            if (dobMatch is not null)
            {
                warnings.Add($"Tie broken by patient date of birth.");
                return new ClaimMatchResult(dobMatch, warnings);
            }
        }

        return new ClaimMatchResult(matches[0], warnings);
    }
}
