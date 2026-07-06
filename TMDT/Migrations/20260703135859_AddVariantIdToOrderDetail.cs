using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMDT.Migrations
{
    /// <inheritdoc />
    public partial class AddVariantIdToOrderDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VariantId",
                table: "OrderDetail",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetail_VariantId",
                table: "OrderDetail",
                column: "VariantId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderDetail_Variant",
                table: "OrderDetail",
                column: "VariantId",
                principalTable: "ProductVariant",
                principalColumn: "VariantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderDetail_Variant",
                table: "OrderDetail");

            migrationBuilder.DropIndex(
                name: "IX_OrderDetail_VariantId",
                table: "OrderDetail");

            migrationBuilder.DropColumn(
                name: "VariantId",
                table: "OrderDetail");
        }
    }
}
