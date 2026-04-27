using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocumentProcessing.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxMessageAbandonedAtUtc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_outbox_messages_unpublished_createdat",
                schema: "public",
                table: "outbox_messages");

            migrationBuilder.AddColumn<DateTime>(
                name: "AbandonedAtUtc",
                schema: "public",
                table: "outbox_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "idx_outbox_messages_unpublished_createdat",
                schema: "public",
                table: "outbox_messages",
                column: "CreatedAtUtc",
                filter: "\"PublishedOnUtc\" IS NULL AND \"AbandonedAtUtc\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_outbox_messages_unpublished_createdat",
                schema: "public",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "AbandonedAtUtc",
                schema: "public",
                table: "outbox_messages");

            migrationBuilder.CreateIndex(
                name: "idx_outbox_messages_unpublished_createdat",
                schema: "public",
                table: "outbox_messages",
                column: "CreatedAtUtc",
                filter: "\"PublishedOnUtc\" IS NULL AND \"ErrorMessage\" IS NULL");
        }
    }
}
