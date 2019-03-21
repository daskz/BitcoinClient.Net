using Microsoft.EntityFrameworkCore.Migrations;

namespace BitcoinClient.API.Data.Migrations
{
    public partial class DecimalPrecisionExpanded : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Balance",
                table: "Wallets",
                type: "decimal(16, 8)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(9, 8)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Fee",
                table: "OutputTransactions",
                type: "decimal(16, 8)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(9, 8)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "OutputTransactions",
                type: "decimal(16, 8)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(9, 8)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "InputTransactions",
                type: "decimal(16, 8)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(9, 8)");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Balance",
                table: "Wallets",
                type: "decimal(9, 8)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(16, 8)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Fee",
                table: "OutputTransactions",
                type: "decimal(9, 8)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(16, 8)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "OutputTransactions",
                type: "decimal(9, 8)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(16, 8)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "InputTransactions",
                type: "decimal(9, 8)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(16, 8)");
        }
    }
}
