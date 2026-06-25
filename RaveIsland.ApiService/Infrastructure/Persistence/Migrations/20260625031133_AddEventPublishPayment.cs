using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaveIsland.ApiService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEventPublishPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventPublishPayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    StripeCheckoutSessionId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AmountCents = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PaidAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventPublishPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventPublishPayments_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventPublishPayments_EventId",
                table: "EventPublishPayments",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_EventPublishPayments_StripeCheckoutSessionId",
                table: "EventPublishPayments",
                column: "StripeCheckoutSessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventPublishPayments");
        }
    }
}
