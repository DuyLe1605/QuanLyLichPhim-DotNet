using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaiTapLon.Migrations
{
    /// <inheritdoc />
    [Migration("20260504021000_AddSeatGridCoordinates")]
    public partial class AddSeatGridCoordinates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GridColumn",
                table: "Seats",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GridRow",
                table: "Seats",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GridSpan",
                table: "Seats",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.Sql("""
                UPDATE Seats
                SET
                    GridRow = ASCII(UPPER(LEFT(RowLabel, 1))) - ASCII('A'),
                    GridColumn = SeatNumber - 1,
                    GridSpan = 1
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GridColumn",
                table: "Seats");

            migrationBuilder.DropColumn(
                name: "GridRow",
                table: "Seats");

            migrationBuilder.DropColumn(
                name: "GridSpan",
                table: "Seats");
        }
    }
}
