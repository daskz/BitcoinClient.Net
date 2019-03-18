using Microsoft.EntityFrameworkCore.Migrations;

namespace BitcoinClient.API.Data.Migrations
{
    public partial class InputTransactionIsRequestedAdded : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRequested",
                table: "InputTransactions",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRequested",
                table: "InputTransactions");
        }
    }
}
