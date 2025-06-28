using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataLayer.Migrations
{
    /// <inheritdoc />
    public partial class addingteamssssss : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TeamId",
                table: "AspNetUserRoles",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_TeamId",
                table: "AspNetUserRoles",
                column: "TeamId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserRoles_Teams_TeamId",
                table: "AspNetUserRoles",
                column: "TeamId",
                principalTable: "Teams",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserRoles_Teams_TeamId",
                table: "AspNetUserRoles");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUserRoles_TeamId",
                table: "AspNetUserRoles");

            migrationBuilder.DropColumn(
                name: "TeamId",
                table: "AspNetUserRoles");
        }
    }
}
