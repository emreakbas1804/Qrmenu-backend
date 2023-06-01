using Microsoft.EntityFrameworkCore.Migrations;

namespace webApi.Migrations
{
    public partial class licanceKey : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LicanceKey",
                table: "AspNetUsers",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LicanceKey",
                table: "AspNetUsers");
        }
    }
}
