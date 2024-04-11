using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalApp.Migrations
{
    /// <inheritdoc />
    public partial class bookingdescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "Bookings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "description",
                table: "Bookings");
        }
    }
}
