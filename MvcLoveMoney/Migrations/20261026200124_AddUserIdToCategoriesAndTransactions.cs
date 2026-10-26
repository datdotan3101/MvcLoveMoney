using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MvcLoveMoney.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdToCategoriesAndTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Categories_Name_CategoryType",
                table: "Categories");

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Transactions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Categories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Existing rows were created before data belonged to a user:
            // give them to the oldest account. If there is no account yet, they cannot be kept.
            migrationBuilder.Sql(@"
                DECLARE @OwnerId int = (SELECT MIN(Id) FROM Users);
                IF @OwnerId IS NOT NULL
                BEGIN
                    UPDATE Categories   SET UserId = @OwnerId WHERE UserId = 0;
                    UPDATE Transactions SET UserId = @OwnerId WHERE UserId = 0;
                END
                ELSE
                BEGIN
                    DELETE FROM Transactions WHERE UserId = 0;
                    DELETE FROM Categories   WHERE UserId = 0;
                END");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_UserId",
                table: "Transactions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_UserId_Name_CategoryType",
                table: "Categories",
                columns: new[] { "UserId", "Name", "CategoryType" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_Users_UserId",
                table: "Categories",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Users_UserId",
                table: "Transactions",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Categories_Users_UserId",
                table: "Categories");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Users_UserId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_UserId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Categories_UserId_Name_CategoryType",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Categories");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name_CategoryType",
                table: "Categories",
                columns: new[] { "Name", "CategoryType" },
                unique: true);
        }
    }
}
