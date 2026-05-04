using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaiTapLon.Migrations
{
    /// <inheritdoc />
    [Migration("20260504014500_AddMoviePosterPathAndTicketUniqueIndex")]
    public partial class AddMoviePosterPathAndTicketUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PosterPath",
                table: "Movies",
                type: "nvarchar(260)",
                maxLength: 260,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_ShowtimeId_SeatId",
                table: "Tickets",
                columns: new[] { "ShowtimeId", "SeatId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tickets_ShowtimeId_SeatId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "PosterPath",
                table: "Movies");
        }
    }
}
