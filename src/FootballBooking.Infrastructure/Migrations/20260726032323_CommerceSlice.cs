using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FootballBooking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CommerceSlice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "Bookings",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PromoCodeId",
                table: "Bookings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PromoCodeSnapshot",
                table: "Bookings",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BookingId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PaymentType = table.Column<int>(type: "INTEGER", nullable: false),
                    Method = table.Column<int>(type: "INTEGER", nullable: false),
                    Amount = table.Column<long>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    TransactionCode = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Note = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    EvidencePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    RecordedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ProcessedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.CheckConstraint("CK_Payments_Amount", "\"Amount\" > 0");
                    table.ForeignKey(
                        name: "FK_Payments_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PromoCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    DiscountType = table.Column<int>(type: "INTEGER", nullable: false),
                    DiscountValue = table.Column<long>(type: "INTEGER", nullable: false),
                    MaximumDiscountAmount = table.Column<long>(type: "INTEGER", nullable: true),
                    MinimumOrderAmount = table.Column<long>(type: "INTEGER", nullable: false),
                    StartsAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    EndsAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    TotalUsageLimit = table.Column<int>(type: "INTEGER", nullable: true),
                    PerPhoneUsageLimit = table.Column<int>(type: "INTEGER", nullable: true),
                    ApplicableFieldId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ApplicableStartMinute = table.Column<int>(type: "INTEGER", nullable: true),
                    ApplicableEndMinute = table.Column<int>(type: "INTEGER", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromoCodes", x => x.Id);
                    table.CheckConstraint("CK_PromoCodes_Amounts", "\"MinimumOrderAmount\" >= 0 AND (\"MaximumDiscountAmount\" IS NULL OR \"MaximumDiscountAmount\" >= 0)");
                    table.CheckConstraint("CK_PromoCodes_DiscountValue", "\"DiscountValue\" > 0");
                });

            migrationBuilder.CreateTable(
                name: "Services",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    UnitName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    UnitPrice = table.Column<long>(type: "INTEGER", nullable: false),
                    IsQuantityTracked = table.Column<bool>(type: "INTEGER", nullable: false),
                    AvailableQuantity = table.Column<int>(type: "INTEGER", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Services", x => x.Id);
                    table.CheckConstraint("CK_Services_AvailableQuantity", "\"AvailableQuantity\" IS NULL OR \"AvailableQuantity\" >= 0");
                    table.CheckConstraint("CK_Services_UnitPrice", "\"UnitPrice\" >= 0");
                });

            migrationBuilder.CreateTable(
                name: "PromoCodeUsages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PromoCodeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BookingId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CustomerPhoneNormalized = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    DiscountAmount = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromoCodeUsages", x => x.Id);
                    table.CheckConstraint("CK_PromoCodeUsages_DiscountAmount", "\"DiscountAmount\" >= 0");
                    table.ForeignKey(
                        name: "FK_PromoCodeUsages_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PromoCodeUsages_PromoCodes_PromoCodeId",
                        column: x => x.PromoCodeId,
                        principalTable: "PromoCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BookingServices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BookingId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ServiceId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ServiceCodeSnapshot = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    ServiceNameSnapshot = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    UnitNameSnapshot = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    UnitPrice = table.Column<long>(type: "INTEGER", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    LineTotal = table.Column<long>(type: "INTEGER", nullable: false),
                    AddedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingServices", x => x.Id);
                    table.CheckConstraint("CK_BookingServices_Amounts", "\"UnitPrice\" >= 0 AND \"LineTotal\" >= 0");
                    table.CheckConstraint("CK_BookingServices_Quantity", "\"Quantity\" > 0");
                    table.ForeignKey(
                        name: "FK_BookingServices_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BookingServices_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_PromoCodeId",
                table: "Bookings",
                column: "PromoCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingServices_BookingId",
                table: "BookingServices",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingServices_ServiceId",
                table: "BookingServices",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_BookingId_Status",
                table: "Payments",
                columns: new[] { "BookingId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CreatedAtUtc",
                table: "Payments",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TransactionCode",
                table: "Payments",
                column: "TransactionCode");

            migrationBuilder.CreateIndex(
                name: "IX_PromoCodes_Code",
                table: "PromoCodes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PromoCodes_IsActive_StartsAtUtc_EndsAtUtc",
                table: "PromoCodes",
                columns: new[] { "IsActive", "StartsAtUtc", "EndsAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PromoCodeUsages_BookingId",
                table: "PromoCodeUsages",
                column: "BookingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PromoCodeUsages_PromoCodeId_CustomerPhoneNormalized",
                table: "PromoCodeUsages",
                columns: new[] { "PromoCodeId", "CustomerPhoneNormalized" });

            migrationBuilder.CreateIndex(
                name: "IX_Services_Code",
                table: "Services",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Services_IsActive_SortOrder",
                table: "Services",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_PromoCodes_PromoCodeId",
                table: "Bookings",
                column: "PromoCodeId",
                principalTable: "PromoCodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_PromoCodes_PromoCodeId",
                table: "Bookings");

            migrationBuilder.DropTable(
                name: "BookingServices");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "PromoCodeUsages");

            migrationBuilder.DropTable(
                name: "Services");

            migrationBuilder.DropTable(
                name: "PromoCodes");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_PromoCodeId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "PromoCodeId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "PromoCodeSnapshot",
                table: "Bookings");
        }
    }
}
