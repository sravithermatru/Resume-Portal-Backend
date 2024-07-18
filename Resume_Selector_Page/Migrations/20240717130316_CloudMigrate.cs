using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Resume_Selector_Page.Migrations
{
    /// <inheritdoc />
    public partial class CloudMigrate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Summary",
                table: "ResumesData",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Summary",
                table: "ResumesData");
        }
    }
}
