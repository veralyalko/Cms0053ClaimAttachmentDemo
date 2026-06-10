using Cms0053ClaimAttachmentDemo.Data;
using Cms0053ClaimAttachmentDemo.Models;
using Microsoft.EntityFrameworkCore;

namespace Cms0053ClaimAttachmentDemo.Services;

public class MockClearinghouseService(AppDbContext db, FileStorageService fileStorage)
{
    // [X12-275-CLEARINGHOUSE-PLACEHOLDER] In production, SubmitAsync would accept an X12 275
    // transaction from the provider system and route it to the real clearinghouse via AS2/SFTP.
    public async Task<ClearinghouseAttachment> SubmitAsync(
        IFormFile file,
        string providerNPI,
        string patientName,
        DateOnly? patientDOB,
        DateOnly serviceDate,
        string documentType)
    {
        var (storedFileName, fileSizeBytes) = await fileStorage.StoreFileAsync(file);

        var record = new ClearinghouseAttachment
        {
            TrackingNumber = "CH-" + Guid.NewGuid().ToString("N")[..12].ToUpper(),
            ProviderNPI    = providerNPI,
            PatientName    = patientName,
            PatientDOB     = patientDOB,
            ServiceDate    = serviceDate,
            DocumentType   = documentType,
            FileName       = file.FileName,
            StoredFileName = storedFileName,
            FileSizeBytes  = fileSizeBytes,
            ContentType    = file.ContentType,
            SubmittedAt    = DateTime.UtcNow,
            Status         = ClearinghouseAttachmentStatus.New
        };

        db.ClearinghouseAttachments.Add(record);
        await db.SaveChangesAsync();
        return record;
    }

    // [X12-275-CLEARINGHOUSE-PLACEHOLDER] In production, PullNewAttachmentsAsync would query
    // the real clearinghouse SFTP/API for pending X12 275 transactions rather than reading
    // from the local staging table.
    public async Task<List<ClearinghouseAttachment>> PullNewAttachmentsAsync()
    {
        var pending = await db.ClearinghouseAttachments
            .Where(a => a.Status == ClearinghouseAttachmentStatus.New)
            .ToListAsync();

        foreach (var a in pending)
        {
            a.Status   = ClearinghouseAttachmentStatus.Pulled;
            a.PulledAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
        return pending;
    }

    public Task<int> CountNewAsync() =>
        db.ClearinghouseAttachments.CountAsync(a => a.Status == ClearinghouseAttachmentStatus.New);
}
