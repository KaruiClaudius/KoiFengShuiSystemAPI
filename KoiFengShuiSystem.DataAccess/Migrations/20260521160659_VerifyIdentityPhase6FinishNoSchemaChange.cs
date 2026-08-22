using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KoiFengShuiSystem.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class VerifyIdentityPhase6FinishNoSchemaChange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ElementId1",
                table: "Posts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ElementId1",
                table: "MarketplaceListings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ElementId1",
                table: "Accounts",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Posts_ElementId1",
                table: "Posts",
                column: "ElementId1");

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceListings_ElementId1",
                table: "MarketplaceListings",
                column: "ElementId1");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_ElementId1",
                table: "Accounts",
                column: "ElementId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Accounts_Elements_ElementId1",
                table: "Accounts",
                column: "ElementId1",
                principalTable: "Elements",
                principalColumn: "ElementId");

            migrationBuilder.AddForeignKey(
                name: "FK_MarketplaceListings_Elements_ElementId1",
                table: "MarketplaceListings",
                column: "ElementId1",
                principalTable: "Elements",
                principalColumn: "ElementId");

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_Elements_ElementId1",
                table: "Posts",
                column: "ElementId1",
                principalTable: "Elements",
                principalColumn: "ElementId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Accounts_Elements_ElementId1",
                table: "Accounts");

            migrationBuilder.DropForeignKey(
                name: "FK_MarketplaceListings_Elements_ElementId1",
                table: "MarketplaceListings");

            migrationBuilder.DropForeignKey(
                name: "FK_Posts_Elements_ElementId1",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Posts_ElementId1",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_MarketplaceListings_ElementId1",
                table: "MarketplaceListings");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_ElementId1",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "ElementId1",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "ElementId1",
                table: "MarketplaceListings");

            migrationBuilder.DropColumn(
                name: "ElementId1",
                table: "Accounts");
        }
    }
}
