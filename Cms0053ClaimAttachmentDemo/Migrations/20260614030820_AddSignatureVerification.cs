using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cms0053ClaimAttachmentDemo.Migrations
{
    /// <inheritdoc />
    public partial class AddSignatureVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AttachmentSignatureVerifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ClaimAttachmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsVerified = table.Column<bool>(type: "INTEGER", nullable: false),
                    Algorithm = table.Column<string>(type: "TEXT", nullable: false),
                    CertificateSubject = table.Column<string>(type: "TEXT", nullable: true),
                    CertificateThumbprint = table.Column<string>(type: "TEXT", nullable: true),
                    CertificateValidFrom = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CertificateValidTo = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FailureReason = table.Column<string>(type: "TEXT", nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttachmentSignatureVerifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttachmentSignatureVerifications_ClaimAttachments_ClaimAttachmentId",
                        column: x => x.ClaimAttachmentId,
                        principalTable: "ClaimAttachments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttachmentSignatureVerifications_ClaimAttachmentId",
                table: "AttachmentSignatureVerifications",
                column: "ClaimAttachmentId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttachmentSignatureVerifications");
        }
    }
}
