using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    public partial class AddUserProfileFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "About",
                table: "AspNetUsers",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BirthDate",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "AspNetUsers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyName",
                table: "AspNetUsers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Experience",
                table: "AspNetUsers",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "AspNetUsers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCompany",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyByEmail",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyByPush",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyBySms",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "AspNetUsers",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SpecializationCategoryId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "About", table: "AspNetUsers");
            migrationBuilder.DropColumn(name: "BirthDate", table: "AspNetUsers");
            migrationBuilder.DropColumn(name: "City", table: "AspNetUsers");
            migrationBuilder.DropColumn(name: "CompanyName", table: "AspNetUsers");
            migrationBuilder.DropColumn(name: "Experience", table: "AspNetUsers");
            migrationBuilder.DropColumn(name: "Gender", table: "AspNetUsers");
            migrationBuilder.DropColumn(name: "IsCompany", table: "AspNetUsers");
            migrationBuilder.DropColumn(name: "NotifyByEmail", table: "AspNetUsers");
            migrationBuilder.DropColumn(name: "NotifyByPush", table: "AspNetUsers");
            migrationBuilder.DropColumn(name: "NotifyBySms", table: "AspNetUsers");
            migrationBuilder.DropColumn(name: "PostalCode", table: "AspNetUsers");
            migrationBuilder.DropColumn(name: "SpecializationCategoryId", table: "AspNetUsers");
        }
    }
}
