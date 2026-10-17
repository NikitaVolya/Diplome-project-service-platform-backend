using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddReceiverToOrderMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReceiverId",
                table: "OrderMessages",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_OrderMessages_ReceiverId",
                table: "OrderMessages",
                column: "ReceiverId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderMessages_AspNetUsers_ReceiverId",
                table: "OrderMessages",
                column: "ReceiverId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderMessages_AspNetUsers_ReceiverId",
                table: "OrderMessages");

            migrationBuilder.DropIndex(
                name: "IX_OrderMessages_ReceiverId",
                table: "OrderMessages");

            migrationBuilder.DropColumn(
                name: "ReceiverId",
                table: "OrderMessages");
        }
    }
}
