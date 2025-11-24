using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigiPayPro.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$SihxklJN/cQ.XioRlJCWxOi4M7TdsH5K60NZoaa89vp.mKHIjtn6a");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$6zk5IRIvigiNGVCjsXrzCuTnZ3S5GfjoMEyNjWuc/6X.IzE5xVIXy");
        }
    }
}
