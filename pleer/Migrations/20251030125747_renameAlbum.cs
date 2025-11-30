using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pleer.Migrations
{
    /// <inheritdoc />
    public partial class renameAlbum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Albums_Artists_ArtistId",
                table: "Albums");

            migrationBuilder.RenameColumn(
                name: "ArtistId",
                table: "Albums",
                newName: "CreatorId");

            migrationBuilder.RenameIndex(
                name: "IX_Albums_ArtistId",
                table: "Albums",
                newName: "IX_Albums_CreatorId");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Albums",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.CreateIndex(
                name: "IX_Playlists_CoverId",
                table: "Playlists",
                column: "CoverId");

            migrationBuilder.CreateIndex(
                name: "IX_Albums_CoverId",
                table: "Albums",
                column: "CoverId");

            migrationBuilder.AddForeignKey(
                name: "FK_Albums_AlbumCovers_CoverId",
                table: "Albums",
                column: "CoverId",
                principalTable: "AlbumCovers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Albums_Artists_CreatorId",
                table: "Albums",
                column: "CreatorId",
                principalTable: "Artists",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Playlists_PlaylistCovers_CoverId",
                table: "Playlists",
                column: "CoverId",
                principalTable: "PlaylistCovers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Albums_AlbumCovers_CoverId",
                table: "Albums");

            migrationBuilder.DropForeignKey(
                name: "FK_Albums_Artists_CreatorId",
                table: "Albums");

            migrationBuilder.DropForeignKey(
                name: "FK_Playlists_PlaylistCovers_CoverId",
                table: "Playlists");

            migrationBuilder.DropIndex(
                name: "IX_Playlists_CoverId",
                table: "Playlists");

            migrationBuilder.DropIndex(
                name: "IX_Albums_CoverId",
                table: "Albums");

            migrationBuilder.RenameColumn(
                name: "CreatorId",
                table: "Albums",
                newName: "ArtistId");

            migrationBuilder.RenameIndex(
                name: "IX_Albums_CreatorId",
                table: "Albums",
                newName: "IX_Albums_ArtistId");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Albums",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150);

            migrationBuilder.AddForeignKey(
                name: "FK_Albums_Artists_ArtistId",
                table: "Albums",
                column: "ArtistId",
                principalTable: "Artists",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
