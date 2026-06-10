using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cms0053ClaimAttachmentDemo.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Claims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ClaimNumber = table.Column<string>(type: "TEXT", nullable: false),
                    PatientName = table.Column<string>(type: "TEXT", nullable: false),
                    PatientDOB = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    ProviderNPI = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderName = table.Column<string>(type: "TEXT", nullable: false),
                    ServiceDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    DiagnosisCode = table.Column<string>(type: "TEXT", nullable: false),
                    AmountBilled = table.Column<decimal>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Claims", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClearinghouseAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TrackingNumber = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderNPI = table.Column<string>(type: "TEXT", nullable: false),
                    PatientName = table.Column<string>(type: "TEXT", nullable: false),
                    PatientDOB = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    ServiceDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    DocumentType = table.Column<string>(type: "TEXT", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", nullable: false),
                    StoredFileName = table.Column<string>(type: "TEXT", nullable: false),
                    FileSizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    PulledAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClearinghouseAttachments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmrDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DocumentName = table.Column<string>(type: "TEXT", nullable: false),
                    DocumentType = table.Column<string>(type: "TEXT", nullable: false),
                    PatientName = table.Column<string>(type: "TEXT", nullable: false),
                    PatientDOB = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    ProviderNPI = table.Column<string>(type: "TEXT", nullable: false),
                    ServiceDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmrDocuments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AttachmentRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ClaimId = table.Column<int>(type: "INTEGER", nullable: false),
                    TrackingNumber = table.Column<string>(type: "TEXT", nullable: false),
                    DocumentTypeRequested = table.Column<string>(type: "TEXT", nullable: false),
                    RequestReason = table.Column<string>(type: "TEXT", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DueDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ProviderEmail = table.Column<string>(type: "TEXT", nullable: false),
                    SecureUploadToken = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttachmentRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttachmentRequests_Claims_ClaimId",
                        column: x => x.ClaimId,
                        principalTable: "Claims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClaimAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ClaimId = table.Column<int>(type: "INTEGER", nullable: true),
                    AttachmentRequestId = table.Column<int>(type: "INTEGER", nullable: true),
                    SourceType = table.Column<string>(type: "TEXT", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", nullable: false),
                    StoredFileName = table.Column<string>(type: "TEXT", nullable: false),
                    FileSizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", nullable: false),
                    FileHash = table.Column<string>(type: "TEXT", nullable: false),
                    SubmittedBy = table.Column<string>(type: "TEXT", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PatientName = table.Column<string>(type: "TEXT", nullable: false),
                    PatientDOB = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    ProviderNPI = table.Column<string>(type: "TEXT", nullable: false),
                    ServiceDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    DocumentType = table.Column<string>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClaimAttachments_AttachmentRequests_AttachmentRequestId",
                        column: x => x.AttachmentRequestId,
                        principalTable: "AttachmentRequests",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ClaimAttachments_Claims_ClaimId",
                        column: x => x.ClaimId,
                        principalTable: "Claims",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AttachmentStatusHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ClaimAttachmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    FromStatus = table.Column<string>(type: "TEXT", nullable: true),
                    ToStatus = table.Column<string>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ChangedBy = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttachmentStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttachmentStatusHistories_ClaimAttachments_ClaimAttachmentId",
                        column: x => x.ClaimAttachmentId,
                        principalTable: "ClaimAttachments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AttachmentValidationResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ClaimAttachmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsValid = table.Column<bool>(type: "INTEGER", nullable: false),
                    Errors = table.Column<string>(type: "TEXT", nullable: false),
                    Warnings = table.Column<string>(type: "TEXT", nullable: false),
                    ValidatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttachmentValidationResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttachmentValidationResults_ClaimAttachments_ClaimAttachmentId",
                        column: x => x.ClaimAttachmentId,
                        principalTable: "ClaimAttachments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttachmentRequests_ClaimId",
                table: "AttachmentRequests",
                column: "ClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_AttachmentRequests_SecureUploadToken",
                table: "AttachmentRequests",
                column: "SecureUploadToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttachmentRequests_TrackingNumber",
                table: "AttachmentRequests",
                column: "TrackingNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttachmentStatusHistories_ClaimAttachmentId",
                table: "AttachmentStatusHistories",
                column: "ClaimAttachmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AttachmentValidationResults_ClaimAttachmentId",
                table: "AttachmentValidationResults",
                column: "ClaimAttachmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClaimAttachments_AttachmentRequestId",
                table: "ClaimAttachments",
                column: "AttachmentRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimAttachments_ClaimId",
                table: "ClaimAttachments",
                column: "ClaimId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttachmentStatusHistories");

            migrationBuilder.DropTable(
                name: "AttachmentValidationResults");

            migrationBuilder.DropTable(
                name: "ClearinghouseAttachments");

            migrationBuilder.DropTable(
                name: "EmrDocuments");

            migrationBuilder.DropTable(
                name: "ClaimAttachments");

            migrationBuilder.DropTable(
                name: "AttachmentRequests");

            migrationBuilder.DropTable(
                name: "Claims");
        }
    }
}
