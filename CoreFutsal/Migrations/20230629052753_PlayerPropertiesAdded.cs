using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreFutsal.Migrations
{
    public partial class PlayerPropertiesAdded : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TemporaryAddress",
                table: "Players",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<bool>(
                name: "IsCaptain",
                table: "Players",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "JerseyNumber",
                table: "Players",
                type: "int",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCaptain",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "JerseyNumber",
                table: "Players");

            migrationBuilder.AlterColumn<string>(
                name: "TemporaryAddress",
                table: "Players",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
