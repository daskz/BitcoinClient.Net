using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BitcoinClient.API.Data.Migrations
{
    public partial class Bitcoinentities : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AspNetUserTokens",
                nullable: false,
                oldClrType: typeof(string),
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserTokens",
                nullable: false,
                oldClrType: typeof(string),
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "ProviderKey",
                table: "AspNetUserLogins",
                nullable: false,
                oldClrType: typeof(string),
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserLogins",
                nullable: false,
                oldClrType: typeof(string),
                oldMaxLength: 128);

            migrationBuilder.CreateTable(
                name: "Wallets",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Balance = table.Column<decimal>(type: "decimal(9, 8)", nullable: false),
                    UserId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wallets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Wallets_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Addresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    AddressId = table.Column<string>(nullable: true),
                    WalletId = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Addresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Addresses_Wallets_WalletId",
                        column: x => x.WalletId,
                        principalTable: "Wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InputTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    TxId = table.Column<string>(nullable: true),
                    Time = table.Column<DateTime>(nullable: false),
                    AddressId = table.Column<Guid>(nullable: true),
                    WalletId = table.Column<Guid>(nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(9, 8)", nullable: false),
                    Fee = table.Column<decimal>(type: "decimal(9, 8)", nullable: false),
                    ConfirmationCount = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InputTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InputTransactions_Addresses_AddressId",
                        column: x => x.AddressId,
                        principalTable: "Addresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InputTransactions_Wallets_WalletId",
                        column: x => x.WalletId,
                        principalTable: "Wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OutputTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    TxId = table.Column<string>(nullable: true),
                    Time = table.Column<DateTime>(nullable: false),
                    AddressId = table.Column<Guid>(nullable: true),
                    WalletId = table.Column<Guid>(nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(9, 8)", nullable: false),
                    Fee = table.Column<decimal>(type: "decimal(9, 8)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutputTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OutputTransactions_Addresses_AddressId",
                        column: x => x.AddressId,
                        principalTable: "Addresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OutputTransactions_Wallets_WalletId",
                        column: x => x.WalletId,
                        principalTable: "Wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_WalletId",
                table: "Addresses",
                column: "WalletId");

            migrationBuilder.CreateIndex(
                name: "IX_InputTransactions_AddressId",
                table: "InputTransactions",
                column: "AddressId");

            migrationBuilder.CreateIndex(
                name: "IX_InputTransactions_WalletId",
                table: "InputTransactions",
                column: "WalletId");

            migrationBuilder.CreateIndex(
                name: "IX_OutputTransactions_AddressId",
                table: "OutputTransactions",
                column: "AddressId");

            migrationBuilder.CreateIndex(
                name: "IX_OutputTransactions_WalletId",
                table: "OutputTransactions",
                column: "WalletId");

            migrationBuilder.CreateIndex(
                name: "IX_Wallets_UserId",
                table: "Wallets",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InputTransactions");

            migrationBuilder.DropTable(
                name: "OutputTransactions");

            migrationBuilder.DropTable(
                name: "Addresses");

            migrationBuilder.DropTable(
                name: "Wallets");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AspNetUserTokens",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string));

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserTokens",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string));

            migrationBuilder.AlterColumn<string>(
                name: "ProviderKey",
                table: "AspNetUserLogins",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string));

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserLogins",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string));
        }
    }
}
