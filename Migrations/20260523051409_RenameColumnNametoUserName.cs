using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace adminPage.Migrations
{
    public partial class RenameColumnNametoUserName : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("SELECT 1;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("SELECT 1;");
        }
    }
}