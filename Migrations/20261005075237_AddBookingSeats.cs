using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusTicketBookingSystem.Migrations
{
    public partial class AddBookingSeats : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BookingSeats",
                columns: table => new
                {
                    BookingSeatID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingID = table.Column<int>(type: "int", nullable: false),
                    BusSeatID = table.Column<int>(type: "int", nullable: false),
                    Fare = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingSeats", x => x.BookingSeatID);
                    table.ForeignKey(
                        name: "FK_BookingSeats_Bookings_BookingID",
                        column: x => x.BookingID,
                        principalTable: "Bookings",
                        principalColumn: "BookingID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BookingSeats_BusSeats_BusSeatID",
                        column: x => x.BusSeatID,
                        principalTable: "BusSeats",
                        principalColumn: "BusSeatID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookingSeats_BookingID",
                table: "BookingSeats",
                column: "BookingID");

            migrationBuilder.CreateIndex(
                name: "IX_BookingSeats_BusSeatID",
                table: "BookingSeats",
                column: "BusSeatID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookingSeats");
        }
    }
}
