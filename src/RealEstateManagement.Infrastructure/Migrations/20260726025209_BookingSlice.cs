using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RealEstateManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BookingSlice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Bookings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BookingCode = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    FieldId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BookingDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    StartMinute = table.Column<int>(type: "INTEGER", nullable: false),
                    EndMinute = table.Column<int>(type: "INTEGER", nullable: false),
                    CustomerName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    CustomerPhone = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    CustomerPhoneNormalized = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    CustomerEmail = table.Column<string>(type: "TEXT", maxLength: 180, nullable: true),
                    CustomerUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Source = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    PaymentStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    CourtAmount = table.Column<long>(type: "INTEGER", nullable: false),
                    ServiceAmount = table.Column<long>(type: "INTEGER", nullable: false),
                    DiscountAmount = table.Column<long>(type: "INTEGER", nullable: false),
                    CancellationFeeAmount = table.Column<long>(type: "INTEGER", nullable: false),
                    RefundedAmount = table.Column<long>(type: "INTEGER", nullable: false),
                    TotalAmount = table.Column<long>(type: "INTEGER", nullable: false),
                    PaidAmount = table.Column<long>(type: "INTEGER", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Note = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CancelledAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookings", x => x.Id);
                    table.CheckConstraint("CK_Bookings_Amounts", "\"CourtAmount\" >= 0 AND \"ServiceAmount\" >= 0 AND \"DiscountAmount\" >= 0 AND \"TotalAmount\" >= 0 AND \"PaidAmount\" >= 0 AND \"RefundedAmount\" >= 0");
                    table.CheckConstraint("CK_Bookings_Minutes", "\"StartMinute\" >= 0 AND \"StartMinute\" < \"EndMinute\" AND \"EndMinute\" <= 1440");
                    table.ForeignKey(
                        name: "FK_Bookings_Fields_FieldId",
                        column: x => x.FieldId,
                        principalTable: "Fields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_BookingCode",
                table: "Bookings",
                column: "BookingCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CustomerPhoneNormalized_BookingCode",
                table: "Bookings",
                columns: new[] { "CustomerPhoneNormalized", "BookingCode" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_FieldId_BookingDate_StartMinute_EndMinute",
                table: "Bookings",
                columns: new[] { "FieldId", "BookingDate", "StartMinute", "EndMinute" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bookings");
        }
    }
}

