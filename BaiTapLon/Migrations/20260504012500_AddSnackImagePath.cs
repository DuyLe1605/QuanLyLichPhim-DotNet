using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaiTapLon.Migrations
{
    /// <inheritdoc />
    public partial class AddSnackImagePath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "Snacks",
                type: "nvarchar(260)",
                maxLength: 260,
                nullable: true);

            migrationBuilder.UpdateData("Snacks", "Id", 1, "ImagePath", "popcorn-small.png");
            migrationBuilder.UpdateData("Snacks", "Id", 2, "ImagePath", "popcorn-large.png");
            migrationBuilder.UpdateData("Snacks", "Id", 3, "ImagePath", "coca-cola.png");
            migrationBuilder.UpdateData("Snacks", "Id", 4, "ImagePath", "pepsi.png");
            migrationBuilder.UpdateData("Snacks", "Id", 5, "ImagePath", "water.png");
            migrationBuilder.UpdateData("Snacks", "Id", 6, "ImagePath", "combo-couple.png");
            migrationBuilder.UpdateData("Snacks", "Id", 7, "ImagePath", "combo-single.png");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "Snacks");
        }
    }
}
