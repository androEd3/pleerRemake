using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pleer.Migrations
{
    /// <inheritdoc />
    public partial class addBlockStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Status",
                table: "Songs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Status",
                table: "Listeners",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Status",
                table: "Artists",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Listeners_ProfilePictureId",
                table: "Listeners",
                column: "ProfilePictureId");

            migrationBuilder.AddForeignKey(
                name: "FK_Listeners_ProfilePictures_ProfilePictureId",
                table: "Listeners",
                column: "ProfilePictureId",
                principalTable: "ProfilePictures",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Listeners_ProfilePictures_ProfilePictureId",
                table: "Listeners");

            migrationBuilder.DropIndex(
                name: "IX_Listeners_ProfilePictureId",
                table: "Listeners");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Songs");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Listeners");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Artists");
        }
    }
}
