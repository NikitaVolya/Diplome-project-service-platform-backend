using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <summary>
    /// Аналітика, фінанси та підтримка в кабінеті виконавця: денні підсумки переглядів,
    /// способи виведення коштів із заявками на виплату та звернення в підтримку.
    ///
    /// Нова колонка в AspNetUsers допускає NULL, тож наявні акаунти лишаються як були.
    /// </summary>
    public partial class AddExecutorInsights : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Nickname",
                table: "AspNetUsers",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyOrders",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyMessages",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyReviews",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyReminders",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "ProfileVisits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Day = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FromSearch = table.Column<int>(type: "int", nullable: false),
                    FromCatalog = table.Column<int>(type: "int", nullable: false),
                    FromDirect = table.Column<int>(type: "int", nullable: false),
                    FromReferral = table.Column<int>(type: "int", nullable: false),
                    FromOther = table.Column<int>(type: "int", nullable: false),
                    PortfolioViews = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfileVisits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProfileVisits_AspNetUsers_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PayoutMethods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayoutMethods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayoutMethods_AspNetUsers_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Payouts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MethodId = table.Column<int>(type: "int", nullable: true),
                    MethodLabel = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payouts_AspNetUsers_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    // Без ON DELETE: другий каскадний шлях до Payouts SQL Server не створює.
                    table.ForeignKey(
                        name: "FK_Payouts_PayoutMethods_MethodId",
                        column: x => x.MethodId,
                        principalTable: "PayoutMethods",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SupportTickets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Topic = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AnsweredAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportTickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupportTickets_AspNetUsers_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProfileVisits_OwnerId_Day",
                table: "ProfileVisits",
                columns: new[] { "OwnerId", "Day" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayoutMethods_OwnerId",
                table: "PayoutMethods",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Payouts_MethodId",
                table: "Payouts",
                column: "MethodId");

            migrationBuilder.CreateIndex(
                name: "IX_Payouts_OwnerId_CreatedAt",
                table: "Payouts",
                columns: new[] { "OwnerId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_OwnerId_CreatedAt",
                table: "SupportTickets",
                columns: new[] { "OwnerId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "SupportTickets");
            migrationBuilder.DropTable(name: "Payouts");
            migrationBuilder.DropTable(name: "PayoutMethods");
            migrationBuilder.DropTable(name: "ProfileVisits");

            migrationBuilder.DropColumn(name: "Nickname", table: "AspNetUsers");
            migrationBuilder.DropColumn(name: "NotifyOrders", table: "AspNetUsers");
            migrationBuilder.DropColumn(name: "NotifyMessages", table: "AspNetUsers");
            migrationBuilder.DropColumn(name: "NotifyReviews", table: "AspNetUsers");
            migrationBuilder.DropColumn(name: "NotifyReminders", table: "AspNetUsers");
        }
    }
}
