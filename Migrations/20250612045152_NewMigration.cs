using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace olx_be_api.Migrations
{
    /// <inheritdoc />
    public partial class NewMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "details",
                table: "transactions",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "transaction_item_details",
                columns: table => new
                {
                    ad_package_id = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<long>(type: "bigint", nullable: false),
                    price = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_transaction_item_details", x => new { x.ad_package_id, x.product_id });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "transaction_item_details");

            migrationBuilder.DropColumn(
                name: "details",
                table: "transactions");
        }
    }
}
