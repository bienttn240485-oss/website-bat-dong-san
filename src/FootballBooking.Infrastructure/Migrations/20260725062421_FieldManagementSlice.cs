using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FootballBooking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FieldManagementSlice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Fields",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    FieldType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Capacity = table.Column<int>(type: "INTEGER", nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Address = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    AmenitiesJson = table.Column<string>(type: "TEXT", nullable: true),
                    MinimumBookingMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    SlotStepMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fields", x => x.Id);
                    table.CheckConstraint("CK_Fields_MinimumBookingMinutes_Positive", "\"MinimumBookingMinutes\" > 0");
                    table.CheckConstraint("CK_Fields_SlotStepMinutes_Positive", "\"SlotStepMinutes\" > 0");
                });

            migrationBuilder.CreateTable(
                name: "FieldBlocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FieldId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BlockDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    StartMinute = table.Column<int>(type: "INTEGER", nullable: false),
                    EndMinute = table.Column<int>(type: "INTEGER", nullable: false),
                    BlockType = table.Column<int>(type: "INTEGER", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldBlocks", x => x.Id);
                    table.CheckConstraint("CK_FieldBlocks_Minutes", "\"StartMinute\" >= 0 AND \"StartMinute\" < \"EndMinute\" AND \"EndMinute\" <= 1440");
                    table.ForeignKey(
                        name: "FK_FieldBlocks_Fields_FieldId",
                        column: x => x.FieldId,
                        principalTable: "Fields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FieldImages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FieldId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StoragePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    AltText = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsCover = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FieldImages_Fields_FieldId",
                        column: x => x.FieldId,
                        principalTable: "Fields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FieldOperatingHours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FieldId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DayOfWeek = table.Column<int>(type: "INTEGER", nullable: false),
                    IsClosed = table.Column<bool>(type: "INTEGER", nullable: false),
                    OpenMinute = table.Column<int>(type: "INTEGER", nullable: true),
                    CloseMinute = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldOperatingHours", x => x.Id);
                    table.CheckConstraint("CK_FieldOperatingHours_DayOfWeek", "\"DayOfWeek\" >= 0 AND \"DayOfWeek\" <= 6");
                    table.CheckConstraint("CK_FieldOperatingHours_Minutes", "\"IsClosed\" = 1 OR (\"OpenMinute\" IS NOT NULL AND \"CloseMinute\" IS NOT NULL AND \"OpenMinute\" >= 0 AND \"OpenMinute\" < \"CloseMinute\" AND \"CloseMinute\" <= 1440)");
                    table.ForeignKey(
                        name: "FK_FieldOperatingHours_Fields_FieldId",
                        column: x => x.FieldId,
                        principalTable: "Fields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PricingRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FieldId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    RuleType = table.Column<int>(type: "INTEGER", nullable: false),
                    SpecificDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    DayOfWeek = table.Column<int>(type: "INTEGER", nullable: true),
                    EffectiveFrom = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    StartMinute = table.Column<int>(type: "INTEGER", nullable: false),
                    EndMinute = table.Column<int>(type: "INTEGER", nullable: false),
                    PricePerHour = table.Column<long>(type: "INTEGER", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PricingRules", x => x.Id);
                    table.CheckConstraint("CK_PricingRules_EffectiveTo", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\"");
                    table.CheckConstraint("CK_PricingRules_Minutes", "\"StartMinute\" >= 0 AND \"StartMinute\" < \"EndMinute\" AND \"EndMinute\" <= 1440");
                    table.CheckConstraint("CK_PricingRules_Price", "\"PricePerHour\" >= 0");
                    table.ForeignKey(
                        name: "FK_PricingRules_Fields_FieldId",
                        column: x => x.FieldId,
                        principalTable: "Fields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FieldBlocks_FieldId_BlockDate_StartMinute_EndMinute",
                table: "FieldBlocks",
                columns: new[] { "FieldId", "BlockDate", "StartMinute", "EndMinute" });

            migrationBuilder.CreateIndex(
                name: "IX_FieldImages_FieldId_SortOrder",
                table: "FieldImages",
                columns: new[] { "FieldId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_FieldOperatingHours_FieldId_DayOfWeek",
                table: "FieldOperatingHours",
                columns: new[] { "FieldId", "DayOfWeek" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Fields_Code",
                table: "Fields",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Fields_Slug",
                table: "Fields",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PricingRules_FieldId_DayOfWeek_StartMinute_EndMinute",
                table: "PricingRules",
                columns: new[] { "FieldId", "DayOfWeek", "StartMinute", "EndMinute" });

            migrationBuilder.CreateIndex(
                name: "IX_PricingRules_FieldId_EffectiveFrom_EffectiveTo_IsActive",
                table: "PricingRules",
                columns: new[] { "FieldId", "EffectiveFrom", "EffectiveTo", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_PricingRules_FieldId_SpecificDate",
                table: "PricingRules",
                columns: new[] { "FieldId", "SpecificDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FieldBlocks");

            migrationBuilder.DropTable(
                name: "FieldImages");

            migrationBuilder.DropTable(
                name: "FieldOperatingHours");

            migrationBuilder.DropTable(
                name: "PricingRules");

            migrationBuilder.DropTable(
                name: "Fields");
        }
    }
}
