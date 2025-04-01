using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Common.Migrations
{
    /// <inheritdoc />
    public partial class WalletsAndOrdersAndTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Login",
                table: "Accounts",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "AccountId",
                table: "Accounts",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.CreateTable(
                name: "AccountWallets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountWallets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountWallets_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BitcoinOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    BtcAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BtcRemained = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BtcPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UtcCreated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UtcUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BitcoinOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BitcoinOrders_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BitcoinOrderTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BidOrderId = table.Column<int>(type: "int", nullable: false),
                    AskOrderId = table.Column<int>(type: "int", nullable: false),
                    BtcAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BtcPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UtcCreated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BitcoinOrderTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BitcoinOrderTransactions_BitcoinOrders_AskOrderId",
                        column: x => x.AskOrderId,
                        principalTable: "BitcoinOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BitcoinOrderTransactions_BitcoinOrders_BidOrderId",
                        column: x => x.BidOrderId,
                        principalTable: "BitcoinOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_AccountId",
                table: "Accounts",
                column: "AccountId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Login",
                table: "Accounts",
                column: "Login",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountWallets_AccountId_Currency",
                table: "AccountWallets",
                columns: new[] { "AccountId", "Currency" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BitcoinOrders_AccountId",
                table: "BitcoinOrders",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_BitcoinOrderTransactions_AskOrderId",
                table: "BitcoinOrderTransactions",
                column: "AskOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_BitcoinOrderTransactions_BidOrderId",
                table: "BitcoinOrderTransactions",
                column: "BidOrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountWallets");

            migrationBuilder.DropTable(
                name: "BitcoinOrderTransactions");

            migrationBuilder.DropTable(
                name: "BitcoinOrders");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_AccountId",
                table: "Accounts");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_Login",
                table: "Accounts");

            migrationBuilder.AlterColumn<string>(
                name: "Login",
                table: "Accounts",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<Guid>(
                name: "AccountId",
                table: "Accounts",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
