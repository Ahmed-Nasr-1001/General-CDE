using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataLayer.Migrations
{
    /// <inheritdoc />
    public partial class sss02 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IssueId",
                table: "Notifications",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_IssueId",
                table: "Notifications",
                column: "IssueId");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Issues_IssueId",
                table: "Notifications",
                column: "IssueId",
                principalTable: "Issues",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Issues_IssueId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_IssueId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "IssueId",
                table: "Notifications");
        }
    }
}
