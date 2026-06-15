using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMDT.Migrations
{
    /// <inheritdoc />
    public partial class SplitCartTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK__Cart__ProductId__797309D9",
                table: "Cart");

            migrationBuilder.DropForeignKey(
                name: "FK__Cart__UserId__787EE5A0",
                table: "Cart");

            migrationBuilder.DropForeignKey(
                name: "FK__Cart__VariantId__7A672E12",
                table: "Cart");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Cart__51BCD7B7E57A7923",
                table: "Cart");

            migrationBuilder.DropIndex(
                name: "IX_Cart_ProductId",
                table: "Cart");

            migrationBuilder.DropIndex(
                name: "IX_Cart_VariantId",
                table: "Cart");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "Cart");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "Cart");

            migrationBuilder.DropColumn(
                name: "VariantId",
                table: "Cart");

            migrationBuilder.RenameColumn(
                name: "AddedAt",
                table: "Cart",
                newName: "CreatedAt");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Cart",
                table: "Cart",
                column: "CartId");

            migrationBuilder.CreateTable(
                name: "CartItem",
                columns: table => new
                {
                    CartItemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CartId = table.Column<int>(type: "int", nullable: true),
                    ProductId = table.Column<int>(type: "int", nullable: true),
                    VariantId = table.Column<int>(type: "int", nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: true, defaultValue: 1),
                    AddedAt = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CartItem", x => x.CartItemId);
                    table.ForeignKey(
                        name: "FK_CartItem_Cart",
                        column: x => x.CartId,
                        principalTable: "Cart",
                        principalColumn: "CartId");
                    table.ForeignKey(
                        name: "FK_CartItem_Product",
                        column: x => x.ProductId,
                        principalTable: "Product",
                        principalColumn: "ProductId");
                    table.ForeignKey(
                        name: "FK_CartItem_Variant",
                        column: x => x.VariantId,
                        principalTable: "ProductVariant",
                        principalColumn: "VariantId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CartItem_CartId",
                table: "CartItem",
                column: "CartId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItem_ProductId",
                table: "CartItem",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItem_VariantId",
                table: "CartItem",
                column: "VariantId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cart_User",
                table: "Cart",
                column: "UserId",
                principalTable: "User",
                principalColumn: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cart_User",
                table: "Cart");

            migrationBuilder.DropTable(
                name: "CartItem");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Cart",
                table: "Cart");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Cart",
                newName: "AddedAt");

            migrationBuilder.AddColumn<int>(
                name: "ProductId",
                table: "Cart",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "Cart",
                type: "int",
                nullable: true,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "VariantId",
                table: "Cart",
                type: "int",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK__Cart__51BCD7B7E57A7923",
                table: "Cart",
                column: "CartId");

            migrationBuilder.CreateIndex(
                name: "IX_Cart_ProductId",
                table: "Cart",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Cart_VariantId",
                table: "Cart",
                column: "VariantId");

            migrationBuilder.AddForeignKey(
                name: "FK__Cart__ProductId__797309D9",
                table: "Cart",
                column: "ProductId",
                principalTable: "Product",
                principalColumn: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK__Cart__UserId__787EE5A0",
                table: "Cart",
                column: "UserId",
                principalTable: "User",
                principalColumn: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK__Cart__VariantId__7A672E12",
                table: "Cart",
                column: "VariantId",
                principalTable: "ProductVariant",
                principalColumn: "VariantId");
        }
    }
}
