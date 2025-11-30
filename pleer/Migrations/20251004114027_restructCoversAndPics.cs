using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pleer.Migrations
{
    /// <inheritdoc />
    public partial class restructCoversAndPics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AlbumCovers_Albums_AlbumId",
                table: "AlbumCovers");

            migrationBuilder.DropForeignKey(
                name: "FK_PlaylistCovers_Playlists_PlaylistId",
                table: "PlaylistCovers");

            migrationBuilder.DropIndex(
                name: "IX_PlaylistCovers_PlaylistId",
                table: "PlaylistCovers");

            migrationBuilder.DropIndex(
                name: "IX_AlbumCovers_AlbumId",
                table: "AlbumCovers");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ProfilePictures");

            migrationBuilder.DropColumn(
                name: "PlaylistId",
                table: "PlaylistCovers");

            migrationBuilder.DropColumn(
                name: "AlbumId",
                table: "AlbumCovers");

            migrationBuilder.AddColumn<int>(
                name: "CoverId",
                table: "Playlists",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProfilePictureId",
                table: "Listeners",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProfilePictureId",
                table: "Artists",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CoverId",
                table: "Albums",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoverId",
                table: "Playlists");

            migrationBuilder.DropColumn(
                name: "ProfilePictureId",
                table: "Listeners");

            migrationBuilder.DropColumn(
                name: "ProfilePictureId",
                table: "Artists");

            migrationBuilder.DropColumn(
                name: "CoverId",
                table: "Albums");

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "ProfilePictures",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PlaylistId",
                table: "PlaylistCovers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AlbumId",
                table: "AlbumCovers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_PlaylistCovers_PlaylistId",
                table: "PlaylistCovers",
                column: "PlaylistId");

            migrationBuilder.CreateIndex(
                name: "IX_AlbumCovers_AlbumId",
                table: "AlbumCovers",
                column: "AlbumId");

            migrationBuilder.AddForeignKey(
                name: "FK_AlbumCovers_Albums_AlbumId",
                table: "AlbumCovers",
                column: "AlbumId",
                principalTable: "Albums",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PlaylistCovers_Playlists_PlaylistId",
                table: "PlaylistCovers",
                column: "PlaylistId",
                principalTable: "Playlists",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
