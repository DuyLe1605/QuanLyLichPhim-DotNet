using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaiTapLon.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerUsername : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add column as nullable to allow backfill of existing rows
            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "Customers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            // Step 2: Backfill existing rows with a unique placeholder based on Id
            migrationBuilder.Sql(
                "UPDATE Customers SET Username = 'user_' + CAST(Id AS nvarchar(20)) WHERE Username IS NULL");

            // Step 3: Alter column to NOT NULL now that all rows have a value
            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "Customers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            // Step 4: Create unique index
            migrationBuilder.CreateIndex(
                name: "IX_Customers_Username",
                table: "Customers",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Customers_Username",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Username",
                table: "Customers");
        }
    }
}
