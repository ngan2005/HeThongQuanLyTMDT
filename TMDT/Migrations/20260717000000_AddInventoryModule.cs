using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMDT.Migrations
{
    /// <summary>
    /// 🟢 Module Quản lý kho cho Seller — bảng InventoryTransaction (lịch sử biến động tồn kho)
    /// + cột Product.LowStockThreshold (ngưỡng cảnh báo sắp hết hàng per-product).
    /// </summary>
    public partial class AddInventoryModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LowStockThreshold",
                table: "Product",
                type: "int",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.CreateTable(
                name: "InventoryTransaction",
                columns: table => new
                {
                    TransactionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: true),
                    VariantId = table.Column<int>(type: "int", nullable: true),
                    ShopId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    QuantityBefore = table.Column<int>(type: "int", nullable: false),
                    QuantityChange = table.Column<int>(type: "int", nullable: false),
                    QuantityAfter = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ReferenceOrderCode = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    ReferenceType = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    PerformedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryTransaction", x => x.TransactionId);
                    table.ForeignKey(
                        name: "FK_InventoryTransaction_Product",
                        column: x => x.ProductId,
                        principalTable: "Product",
                        principalColumn: "ProductId");
                    table.ForeignKey(
                        name: "FK_InventoryTransaction_Variant",
                        column: x => x.VariantId,
                        principalTable: "ProductVariant",
                        principalColumn: "VariantId");
                    table.ForeignKey(
                        name: "FK_InventoryTransaction_Shop",
                        column: x => x.ShopId,
                        principalTable: "Shop",
                        principalColumn: "ShopId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransaction_Shop_CreatedAt",
                table: "InventoryTransaction",
                columns: new[] { "ShopId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransaction_Product_Variant_CreatedAt",
                table: "InventoryTransaction",
                columns: new[] { "ProductId", "VariantId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryTransaction");

            migrationBuilder.DropColumn(
                name: "LowStockThreshold",
                table: "Product");
        }
    }
}
