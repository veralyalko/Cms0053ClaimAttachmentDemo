using Cms0053ClaimAttachmentDemo.Models;
using Microsoft.EntityFrameworkCore;

namespace Cms0053ClaimAttachmentDemo.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Claim> Claims => Set<Claim>();
    public DbSet<AttachmentRequest> AttachmentRequests => Set<AttachmentRequest>();
    public DbSet<ClaimAttachment> ClaimAttachments => Set<ClaimAttachment>();
    public DbSet<AttachmentStatusHistory> AttachmentStatusHistories => Set<AttachmentStatusHistory>();
    public DbSet<AttachmentValidationResult> AttachmentValidationResults => Set<AttachmentValidationResult>();
    public DbSet<ClearinghouseAttachment> ClearinghouseAttachments => Set<ClearinghouseAttachment>();
    public DbSet<EmrDocument> EmrDocuments => Set<EmrDocument>();
    public DbSet<AttachmentSignatureVerification> AttachmentSignatureVerifications => Set<AttachmentSignatureVerification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AttachmentRequest>()
            .HasIndex(r => r.TrackingNumber).IsUnique();

        modelBuilder.Entity<AttachmentRequest>()
            .HasIndex(r => r.SecureUploadToken).IsUnique();

        modelBuilder.Entity<AttachmentValidationResult>()
            .HasIndex(v => v.ClaimAttachmentId).IsUnique();

        modelBuilder.Entity<AttachmentSignatureVerification>()
            .HasIndex(v => v.ClaimAttachmentId).IsUnique();
    }
}
