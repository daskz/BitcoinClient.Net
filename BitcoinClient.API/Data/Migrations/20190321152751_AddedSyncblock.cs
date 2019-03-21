using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BitcoinClient.API.Data.Migrations
{
    public partial class AddedSyncblock : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Fee",
                table: "InputTransactions");

            migrationBuilder.AlterColumn<long>(
                name: "ConfirmationCount",
                table: "InputTransactions",
                nullable: false,
                oldClrType: typeof(int));

            migrationBuilder.CreateTable(
                name: "SyncBlocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Index = table.Column<long>(nullable: false),
                    Hash = table.Column<string>(nullable: true),
                    CreatedDate = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncBlocks", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SyncBlocks");

            migrationBuilder.AlterColumn<int>(
                name: "ConfirmationCount",
                table: "InputTransactions",
                nullable: false,
                oldClrType: typeof(long));

            migrationBuilder.AddColumn<decimal>(
                name: "Fee",
                table: "InputTransactions",
                type: "decimal(9, 8)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
