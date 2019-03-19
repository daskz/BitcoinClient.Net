using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BitcoinClient.API.Data.Migrations
{
    public partial class InputTransactionConstraintsAdded : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InputTransactions_Addresses_AddressId",
                table: "InputTransactions");

            migrationBuilder.AlterColumn<string>(
                name: "TxId",
                table: "InputTransactions",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "AddressId",
                table: "InputTransactions",
                nullable: false,
                oldClrType: typeof(Guid),
                oldNullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_InputTransactions_TxId_AddressId",
                table: "InputTransactions",
                columns: new[] { "TxId", "AddressId" });

            migrationBuilder.AddForeignKey(
                name: "FK_InputTransactions_Addresses_AddressId",
                table: "InputTransactions",
                column: "AddressId",
                principalTable: "Addresses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InputTransactions_Addresses_AddressId",
                table: "InputTransactions");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_InputTransactions_TxId_AddressId",
                table: "InputTransactions");

            migrationBuilder.AlterColumn<string>(
                name: "TxId",
                table: "InputTransactions",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AlterColumn<Guid>(
                name: "AddressId",
                table: "InputTransactions",
                nullable: true,
                oldClrType: typeof(Guid));

            migrationBuilder.AddForeignKey(
                name: "FK_InputTransactions_Addresses_AddressId",
                table: "InputTransactions",
                column: "AddressId",
                principalTable: "Addresses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
