using BaiTapLon.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaiTapLon.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260504030000_RepairMissingSchemaColumns")]
    public partial class RepairMissingSchemaColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('Movies', 'Code') IS NULL
                    ALTER TABLE [Movies] ADD [Code] nvarchar(max) NOT NULL CONSTRAINT [DF_Movies_Code_Repair] DEFAULT N'';

                IF COL_LENGTH('Movies', 'PosterPath') IS NULL
                    ALTER TABLE [Movies] ADD [PosterPath] nvarchar(260) NULL;

                IF COL_LENGTH('Snacks', 'ImagePath') IS NULL
                    ALTER TABLE [Snacks] ADD [ImagePath] nvarchar(260) NULL;

                IF COL_LENGTH('Seats', 'GridColumn') IS NULL
                    ALTER TABLE [Seats] ADD [GridColumn] int NOT NULL CONSTRAINT [DF_Seats_GridColumn_Repair] DEFAULT 0;

                IF COL_LENGTH('Seats', 'GridRow') IS NULL
                    ALTER TABLE [Seats] ADD [GridRow] int NOT NULL CONSTRAINT [DF_Seats_GridRow_Repair] DEFAULT 0;

                IF COL_LENGTH('Seats', 'GridSpan') IS NULL
                    ALTER TABLE [Seats] ADD [GridSpan] int NOT NULL CONSTRAINT [DF_Seats_GridSpan_Repair] DEFAULT 1;
                """);

            migrationBuilder.Sql("""
                UPDATE [Seats]
                SET
                    [GridRow] = CASE
                        WHEN [RowLabel] IS NULL OR LEN([RowLabel]) = 0 THEN [GridRow]
                        ELSE ASCII(UPPER(LEFT([RowLabel], 1))) - ASCII('A')
                    END,
                    [GridColumn] = CASE
                        WHEN [SeatNumber] > 0 THEN [SeatNumber] - 1
                        ELSE [GridColumn]
                    END,
                    [GridSpan] = CASE
                        WHEN [GridSpan] < 1 THEN 1
                        ELSE [GridSpan]
                    END;
                """);

            migrationBuilder.Sql("""
                UPDATE [Snacks] SET [ImagePath] = 'popcorn-small.png' WHERE [Id] = 1 AND [ImagePath] IS NULL;
                UPDATE [Snacks] SET [ImagePath] = 'popcorn-large.png' WHERE [Id] = 2 AND [ImagePath] IS NULL;
                UPDATE [Snacks] SET [ImagePath] = 'coca-cola.png' WHERE [Id] = 3 AND [ImagePath] IS NULL;
                UPDATE [Snacks] SET [ImagePath] = 'pepsi.png' WHERE [Id] = 4 AND [ImagePath] IS NULL;
                UPDATE [Snacks] SET [ImagePath] = 'water.png' WHERE [Id] = 5 AND [ImagePath] IS NULL;
                UPDATE [Snacks] SET [ImagePath] = 'combo-couple.png' WHERE [Id] = 6 AND [ImagePath] IS NULL;
                UPDATE [Snacks] SET [ImagePath] = 'combo-single.png' WHERE [Id] = 7 AND [ImagePath] IS NULL;
                """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE [name] = 'IX_Tickets_ShowtimeId_SeatId'
                      AND [object_id] = OBJECT_ID(N'[Tickets]')
                )
                AND COL_LENGTH('Tickets', 'ShowtimeId') IS NOT NULL
                AND COL_LENGTH('Tickets', 'SeatId') IS NOT NULL
                    CREATE UNIQUE INDEX [IX_Tickets_ShowtimeId_SeatId]
                    ON [Tickets] ([ShowtimeId], [SeatId]);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // This migration repairs databases whose migration history and schema drifted apart.
            // Rolling it back should not drop user data columns.
        }
    }
}
