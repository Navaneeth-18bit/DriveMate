using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace adminPage.Migrations
{
    public partial class AddExperienceToDrivers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Experience",
                table: "Drivers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Experience",
                table: "Drivers");
        }
    }
}