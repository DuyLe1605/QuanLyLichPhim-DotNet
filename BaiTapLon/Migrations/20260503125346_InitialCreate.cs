using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BaiTapLon.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Genres",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Genres", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Movies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Director = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Actors = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Duration = table.Column<int>(type: "int", nullable: false),
                    AgeRating = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Poster = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    TrailerUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ReleaseDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Movies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rooms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalSeats = table.Column<int>(type: "int", nullable: false),
                    Rows = table.Column<int>(type: "int", nullable: false),
                    Columns = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rooms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Snacks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Snacks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MovieGenres",
                columns: table => new
                {
                    MovieId = table.Column<int>(type: "int", nullable: false),
                    GenreId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovieGenres", x => new { x.MovieId, x.GenreId });
                    table.ForeignKey(
                        name: "FK_MovieGenres_Genres_GenreId",
                        column: x => x.GenreId,
                        principalTable: "Genres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MovieGenres_Movies_MovieId",
                        column: x => x.MovieId,
                        principalTable: "Movies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Seats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoomId = table.Column<int>(type: "int", nullable: false),
                    RowLabel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SeatNumber = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PriceMultiplier = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Seats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Seats_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Showtimes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MovieId = table.Column<int>(type: "int", nullable: false),
                    RoomId = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BasePrice = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Showtimes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Showtimes_Movies_MovieId",
                        column: x => x.MovieId,
                        principalTable: "Movies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Showtimes_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CustomerPhone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    ReceivedAmount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    ChangeAmount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invoices_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceSnacks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceId = table.Column<int>(type: "int", nullable: false),
                    SnackId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceSnacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceSnacks_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InvoiceSnacks_Snacks_SnackId",
                        column: x => x.SnackId,
                        principalTable: "Snacks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tickets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShowtimeId = table.Column<int>(type: "int", nullable: false),
                    SeatId = table.Column<int>(type: "int", nullable: false),
                    InvoiceId = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tickets_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Tickets_Seats_SeatId",
                        column: x => x.SeatId,
                        principalTable: "Seats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tickets_Showtimes_ShowtimeId",
                        column: x => x.ShowtimeId,
                        principalTable: "Showtimes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Genres",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Hành động" },
                    { 2, "Hài hước" },
                    { 3, "Kinh dị" },
                    { 4, "Tình cảm" },
                    { 5, "Viễn tưởng" },
                    { 6, "Hoạt hình" },
                    { 7, "Tâm lý" },
                    { 8, "Phiêu lưu" }
                });

            migrationBuilder.InsertData(
                table: "Rooms",
                columns: new[] { "Id", "Columns", "IsActive", "Name", "Rows", "TotalSeats", "Type" },
                values: new object[,]
                {
                    { 1, 10, true, "Phòng 1", 8, 80, "2D" },
                    { 2, 10, true, "Phòng 2", 6, 60, "3D" },
                    { 3, 10, true, "Phòng 3", 10, 100, "IMAX" }
                });

            migrationBuilder.InsertData(
                table: "Snacks",
                columns: new[] { "Id", "Category", "IsActive", "Name", "Price" },
                values: new object[,]
                {
                    { 1, "Food", true, "Bắp rang bơ (Nhỏ)", 49000m },
                    { 2, "Food", true, "Bắp rang bơ (Lớn)", 69000m },
                    { 3, "Drink", true, "Coca-Cola", 29000m },
                    { 4, "Drink", true, "Pepsi", 29000m },
                    { 5, "Drink", true, "Nước suối", 15000m },
                    { 6, "Combo", true, "Combo Couple (2 Bắp + 2 Nước)", 129000m },
                    { 7, "Combo", true, "Combo Single (1 Bắp + 1 Nước)", 69000m }
                });

            migrationBuilder.InsertData(
                table: "Seats",
                columns: new[] { "Id", "PriceMultiplier", "RoomId", "RowLabel", "SeatNumber", "Type" },
                values: new object[,]
                {
                    { 1, 1.0m, 1, "A", 1, "Standard" },
                    { 2, 1.0m, 1, "A", 2, "Standard" },
                    { 3, 1.0m, 1, "A", 3, "Standard" },
                    { 4, 1.0m, 1, "A", 4, "Standard" },
                    { 5, 1.0m, 1, "A", 5, "Standard" },
                    { 6, 1.0m, 1, "A", 6, "Standard" },
                    { 7, 1.0m, 1, "A", 7, "Standard" },
                    { 8, 1.0m, 1, "A", 8, "Standard" },
                    { 9, 1.0m, 1, "A", 9, "Standard" },
                    { 10, 1.0m, 1, "A", 10, "Standard" },
                    { 11, 1.0m, 1, "B", 1, "Standard" },
                    { 12, 1.0m, 1, "B", 2, "Standard" },
                    { 13, 1.0m, 1, "B", 3, "Standard" },
                    { 14, 1.0m, 1, "B", 4, "Standard" },
                    { 15, 1.0m, 1, "B", 5, "Standard" },
                    { 16, 1.0m, 1, "B", 6, "Standard" },
                    { 17, 1.0m, 1, "B", 7, "Standard" },
                    { 18, 1.0m, 1, "B", 8, "Standard" },
                    { 19, 1.0m, 1, "B", 9, "Standard" },
                    { 20, 1.0m, 1, "B", 10, "Standard" },
                    { 21, 1.0m, 1, "C", 1, "Standard" },
                    { 22, 1.0m, 1, "C", 2, "Standard" },
                    { 23, 1.0m, 1, "C", 3, "Standard" },
                    { 24, 1.0m, 1, "C", 4, "Standard" },
                    { 25, 1.0m, 1, "C", 5, "Standard" },
                    { 26, 1.0m, 1, "C", 6, "Standard" },
                    { 27, 1.0m, 1, "C", 7, "Standard" },
                    { 28, 1.0m, 1, "C", 8, "Standard" },
                    { 29, 1.0m, 1, "C", 9, "Standard" },
                    { 30, 1.0m, 1, "C", 10, "Standard" },
                    { 31, 1.0m, 1, "D", 1, "Standard" },
                    { 32, 1.0m, 1, "D", 2, "Standard" },
                    { 33, 1.0m, 1, "D", 3, "Standard" },
                    { 34, 1.0m, 1, "D", 4, "Standard" },
                    { 35, 1.0m, 1, "D", 5, "Standard" },
                    { 36, 1.0m, 1, "D", 6, "Standard" },
                    { 37, 1.0m, 1, "D", 7, "Standard" },
                    { 38, 1.0m, 1, "D", 8, "Standard" },
                    { 39, 1.0m, 1, "D", 9, "Standard" },
                    { 40, 1.0m, 1, "D", 10, "Standard" },
                    { 41, 1.0m, 1, "E", 1, "Standard" },
                    { 42, 1.0m, 1, "E", 2, "Standard" },
                    { 43, 1.0m, 1, "E", 3, "Standard" },
                    { 44, 1.0m, 1, "E", 4, "Standard" },
                    { 45, 1.0m, 1, "E", 5, "Standard" },
                    { 46, 1.0m, 1, "E", 6, "Standard" },
                    { 47, 1.0m, 1, "E", 7, "Standard" },
                    { 48, 1.0m, 1, "E", 8, "Standard" },
                    { 49, 1.0m, 1, "E", 9, "Standard" },
                    { 50, 1.0m, 1, "E", 10, "Standard" },
                    { 51, 1.5m, 1, "F", 1, "VIP" },
                    { 52, 1.5m, 1, "F", 2, "VIP" },
                    { 53, 1.5m, 1, "F", 3, "VIP" },
                    { 54, 1.5m, 1, "F", 4, "VIP" },
                    { 55, 1.5m, 1, "F", 5, "VIP" },
                    { 56, 1.5m, 1, "F", 6, "VIP" },
                    { 57, 1.5m, 1, "F", 7, "VIP" },
                    { 58, 1.5m, 1, "F", 8, "VIP" },
                    { 59, 1.5m, 1, "F", 9, "VIP" },
                    { 60, 1.5m, 1, "F", 10, "VIP" },
                    { 61, 1.5m, 1, "G", 1, "VIP" },
                    { 62, 1.5m, 1, "G", 2, "VIP" },
                    { 63, 1.5m, 1, "G", 3, "VIP" },
                    { 64, 1.5m, 1, "G", 4, "VIP" },
                    { 65, 1.5m, 1, "G", 5, "VIP" },
                    { 66, 1.5m, 1, "G", 6, "VIP" },
                    { 67, 1.5m, 1, "G", 7, "VIP" },
                    { 68, 1.5m, 1, "G", 8, "VIP" },
                    { 69, 1.5m, 1, "G", 9, "VIP" },
                    { 70, 1.5m, 1, "G", 10, "VIP" },
                    { 71, 2.0m, 1, "H", 1, "Couple" },
                    { 72, 2.0m, 1, "H", 2, "Couple" },
                    { 73, 2.0m, 1, "H", 3, "Couple" },
                    { 74, 2.0m, 1, "H", 4, "Couple" },
                    { 75, 2.0m, 1, "H", 5, "Couple" },
                    { 76, 2.0m, 1, "H", 6, "Couple" },
                    { 77, 2.0m, 1, "H", 7, "Couple" },
                    { 78, 2.0m, 1, "H", 8, "Couple" },
                    { 79, 2.0m, 1, "H", 9, "Couple" },
                    { 80, 2.0m, 1, "H", 10, "Couple" },
                    { 81, 1.0m, 2, "A", 1, "Standard" },
                    { 82, 1.0m, 2, "A", 2, "Standard" },
                    { 83, 1.0m, 2, "A", 3, "Standard" },
                    { 84, 1.0m, 2, "A", 4, "Standard" },
                    { 85, 1.0m, 2, "A", 5, "Standard" },
                    { 86, 1.0m, 2, "A", 6, "Standard" },
                    { 87, 1.0m, 2, "A", 7, "Standard" },
                    { 88, 1.0m, 2, "A", 8, "Standard" },
                    { 89, 1.0m, 2, "A", 9, "Standard" },
                    { 90, 1.0m, 2, "A", 10, "Standard" },
                    { 91, 1.0m, 2, "B", 1, "Standard" },
                    { 92, 1.0m, 2, "B", 2, "Standard" },
                    { 93, 1.0m, 2, "B", 3, "Standard" },
                    { 94, 1.0m, 2, "B", 4, "Standard" },
                    { 95, 1.0m, 2, "B", 5, "Standard" },
                    { 96, 1.0m, 2, "B", 6, "Standard" },
                    { 97, 1.0m, 2, "B", 7, "Standard" },
                    { 98, 1.0m, 2, "B", 8, "Standard" },
                    { 99, 1.0m, 2, "B", 9, "Standard" },
                    { 100, 1.0m, 2, "B", 10, "Standard" },
                    { 101, 1.0m, 2, "C", 1, "Standard" },
                    { 102, 1.0m, 2, "C", 2, "Standard" },
                    { 103, 1.0m, 2, "C", 3, "Standard" },
                    { 104, 1.0m, 2, "C", 4, "Standard" },
                    { 105, 1.0m, 2, "C", 5, "Standard" },
                    { 106, 1.0m, 2, "C", 6, "Standard" },
                    { 107, 1.0m, 2, "C", 7, "Standard" },
                    { 108, 1.0m, 2, "C", 8, "Standard" },
                    { 109, 1.0m, 2, "C", 9, "Standard" },
                    { 110, 1.0m, 2, "C", 10, "Standard" },
                    { 111, 1.5m, 2, "D", 1, "VIP" },
                    { 112, 1.5m, 2, "D", 2, "VIP" },
                    { 113, 1.5m, 2, "D", 3, "VIP" },
                    { 114, 1.5m, 2, "D", 4, "VIP" },
                    { 115, 1.5m, 2, "D", 5, "VIP" },
                    { 116, 1.5m, 2, "D", 6, "VIP" },
                    { 117, 1.5m, 2, "D", 7, "VIP" },
                    { 118, 1.5m, 2, "D", 8, "VIP" },
                    { 119, 1.5m, 2, "D", 9, "VIP" },
                    { 120, 1.5m, 2, "D", 10, "VIP" },
                    { 121, 1.5m, 2, "E", 1, "VIP" },
                    { 122, 1.5m, 2, "E", 2, "VIP" },
                    { 123, 1.5m, 2, "E", 3, "VIP" },
                    { 124, 1.5m, 2, "E", 4, "VIP" },
                    { 125, 1.5m, 2, "E", 5, "VIP" },
                    { 126, 1.5m, 2, "E", 6, "VIP" },
                    { 127, 1.5m, 2, "E", 7, "VIP" },
                    { 128, 1.5m, 2, "E", 8, "VIP" },
                    { 129, 1.5m, 2, "E", 9, "VIP" },
                    { 130, 1.5m, 2, "E", 10, "VIP" },
                    { 131, 2.0m, 2, "F", 1, "Couple" },
                    { 132, 2.0m, 2, "F", 2, "Couple" },
                    { 133, 2.0m, 2, "F", 3, "Couple" },
                    { 134, 2.0m, 2, "F", 4, "Couple" },
                    { 135, 2.0m, 2, "F", 5, "Couple" },
                    { 136, 2.0m, 2, "F", 6, "Couple" },
                    { 137, 2.0m, 2, "F", 7, "Couple" },
                    { 138, 2.0m, 2, "F", 8, "Couple" },
                    { 139, 2.0m, 2, "F", 9, "Couple" },
                    { 140, 2.0m, 2, "F", 10, "Couple" },
                    { 141, 1.0m, 3, "A", 1, "Standard" },
                    { 142, 1.0m, 3, "A", 2, "Standard" },
                    { 143, 1.0m, 3, "A", 3, "Standard" },
                    { 144, 1.0m, 3, "A", 4, "Standard" },
                    { 145, 1.0m, 3, "A", 5, "Standard" },
                    { 146, 1.0m, 3, "A", 6, "Standard" },
                    { 147, 1.0m, 3, "A", 7, "Standard" },
                    { 148, 1.0m, 3, "A", 8, "Standard" },
                    { 149, 1.0m, 3, "A", 9, "Standard" },
                    { 150, 1.0m, 3, "A", 10, "Standard" },
                    { 151, 1.0m, 3, "B", 1, "Standard" },
                    { 152, 1.0m, 3, "B", 2, "Standard" },
                    { 153, 1.0m, 3, "B", 3, "Standard" },
                    { 154, 1.0m, 3, "B", 4, "Standard" },
                    { 155, 1.0m, 3, "B", 5, "Standard" },
                    { 156, 1.0m, 3, "B", 6, "Standard" },
                    { 157, 1.0m, 3, "B", 7, "Standard" },
                    { 158, 1.0m, 3, "B", 8, "Standard" },
                    { 159, 1.0m, 3, "B", 9, "Standard" },
                    { 160, 1.0m, 3, "B", 10, "Standard" },
                    { 161, 1.0m, 3, "C", 1, "Standard" },
                    { 162, 1.0m, 3, "C", 2, "Standard" },
                    { 163, 1.0m, 3, "C", 3, "Standard" },
                    { 164, 1.0m, 3, "C", 4, "Standard" },
                    { 165, 1.0m, 3, "C", 5, "Standard" },
                    { 166, 1.0m, 3, "C", 6, "Standard" },
                    { 167, 1.0m, 3, "C", 7, "Standard" },
                    { 168, 1.0m, 3, "C", 8, "Standard" },
                    { 169, 1.0m, 3, "C", 9, "Standard" },
                    { 170, 1.0m, 3, "C", 10, "Standard" },
                    { 171, 1.0m, 3, "D", 1, "Standard" },
                    { 172, 1.0m, 3, "D", 2, "Standard" },
                    { 173, 1.0m, 3, "D", 3, "Standard" },
                    { 174, 1.0m, 3, "D", 4, "Standard" },
                    { 175, 1.0m, 3, "D", 5, "Standard" },
                    { 176, 1.0m, 3, "D", 6, "Standard" },
                    { 177, 1.0m, 3, "D", 7, "Standard" },
                    { 178, 1.0m, 3, "D", 8, "Standard" },
                    { 179, 1.0m, 3, "D", 9, "Standard" },
                    { 180, 1.0m, 3, "D", 10, "Standard" },
                    { 181, 1.0m, 3, "E", 1, "Standard" },
                    { 182, 1.0m, 3, "E", 2, "Standard" },
                    { 183, 1.0m, 3, "E", 3, "Standard" },
                    { 184, 1.0m, 3, "E", 4, "Standard" },
                    { 185, 1.0m, 3, "E", 5, "Standard" },
                    { 186, 1.0m, 3, "E", 6, "Standard" },
                    { 187, 1.0m, 3, "E", 7, "Standard" },
                    { 188, 1.0m, 3, "E", 8, "Standard" },
                    { 189, 1.0m, 3, "E", 9, "Standard" },
                    { 190, 1.0m, 3, "E", 10, "Standard" },
                    { 191, 1.0m, 3, "F", 1, "Standard" },
                    { 192, 1.0m, 3, "F", 2, "Standard" },
                    { 193, 1.0m, 3, "F", 3, "Standard" },
                    { 194, 1.0m, 3, "F", 4, "Standard" },
                    { 195, 1.0m, 3, "F", 5, "Standard" },
                    { 196, 1.0m, 3, "F", 6, "Standard" },
                    { 197, 1.0m, 3, "F", 7, "Standard" },
                    { 198, 1.0m, 3, "F", 8, "Standard" },
                    { 199, 1.0m, 3, "F", 9, "Standard" },
                    { 200, 1.0m, 3, "F", 10, "Standard" },
                    { 201, 1.0m, 3, "G", 1, "Standard" },
                    { 202, 1.0m, 3, "G", 2, "Standard" },
                    { 203, 1.0m, 3, "G", 3, "Standard" },
                    { 204, 1.0m, 3, "G", 4, "Standard" },
                    { 205, 1.0m, 3, "G", 5, "Standard" },
                    { 206, 1.0m, 3, "G", 6, "Standard" },
                    { 207, 1.0m, 3, "G", 7, "Standard" },
                    { 208, 1.0m, 3, "G", 8, "Standard" },
                    { 209, 1.0m, 3, "G", 9, "Standard" },
                    { 210, 1.0m, 3, "G", 10, "Standard" },
                    { 211, 1.5m, 3, "H", 1, "VIP" },
                    { 212, 1.5m, 3, "H", 2, "VIP" },
                    { 213, 1.5m, 3, "H", 3, "VIP" },
                    { 214, 1.5m, 3, "H", 4, "VIP" },
                    { 215, 1.5m, 3, "H", 5, "VIP" },
                    { 216, 1.5m, 3, "H", 6, "VIP" },
                    { 217, 1.5m, 3, "H", 7, "VIP" },
                    { 218, 1.5m, 3, "H", 8, "VIP" },
                    { 219, 1.5m, 3, "H", 9, "VIP" },
                    { 220, 1.5m, 3, "H", 10, "VIP" },
                    { 221, 1.5m, 3, "I", 1, "VIP" },
                    { 222, 1.5m, 3, "I", 2, "VIP" },
                    { 223, 1.5m, 3, "I", 3, "VIP" },
                    { 224, 1.5m, 3, "I", 4, "VIP" },
                    { 225, 1.5m, 3, "I", 5, "VIP" },
                    { 226, 1.5m, 3, "I", 6, "VIP" },
                    { 227, 1.5m, 3, "I", 7, "VIP" },
                    { 228, 1.5m, 3, "I", 8, "VIP" },
                    { 229, 1.5m, 3, "I", 9, "VIP" },
                    { 230, 1.5m, 3, "I", 10, "VIP" },
                    { 231, 2.0m, 3, "J", 1, "Couple" },
                    { 232, 2.0m, 3, "J", 2, "Couple" },
                    { 233, 2.0m, 3, "J", 3, "Couple" },
                    { 234, 2.0m, 3, "J", 4, "Couple" },
                    { 235, 2.0m, 3, "J", 5, "Couple" },
                    { 236, 2.0m, 3, "J", 6, "Couple" },
                    { 237, 2.0m, 3, "J", 7, "Couple" },
                    { 238, 2.0m, 3, "J", 8, "Couple" },
                    { 239, 2.0m, 3, "J", 9, "Couple" },
                    { 240, 2.0m, 3, "J", 10, "Couple" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_UserId",
                table: "Invoices",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceSnacks_InvoiceId",
                table: "InvoiceSnacks",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceSnacks_SnackId",
                table: "InvoiceSnacks",
                column: "SnackId");

            migrationBuilder.CreateIndex(
                name: "IX_MovieGenres_GenreId",
                table: "MovieGenres",
                column: "GenreId");

            migrationBuilder.CreateIndex(
                name: "IX_Seats_RoomId",
                table: "Seats",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_Showtimes_MovieId",
                table: "Showtimes",
                column: "MovieId");

            migrationBuilder.CreateIndex(
                name: "IX_Showtimes_RoomId",
                table: "Showtimes",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_InvoiceId",
                table: "Tickets",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_SeatId",
                table: "Tickets",
                column: "SeatId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_ShowtimeId",
                table: "Tickets",
                column: "ShowtimeId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvoiceSnacks");

            migrationBuilder.DropTable(
                name: "MovieGenres");

            migrationBuilder.DropTable(
                name: "Tickets");

            migrationBuilder.DropTable(
                name: "Snacks");

            migrationBuilder.DropTable(
                name: "Genres");

            migrationBuilder.DropTable(
                name: "Invoices");

            migrationBuilder.DropTable(
                name: "Seats");

            migrationBuilder.DropTable(
                name: "Showtimes");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Movies");

            migrationBuilder.DropTable(
                name: "Rooms");
        }
    }
}
