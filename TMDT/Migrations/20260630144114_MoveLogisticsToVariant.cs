using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMDT.Migrations
{
    /// <inheritdoc />
    public partial class MoveLogisticsToVariant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HeightCm",
                table: "Product");

            migrationBuilder.DropColumn(
                name: "LengthCm",
                table: "Product");

            migrationBuilder.DropColumn(
                name: "WeightGrams",
                table: "Product");

            migrationBuilder.DropColumn(
                name: "WidthCm",
                table: "Product");

            migrationBuilder.AddColumn<int>(
                name: "HeightCm",
                table: "ProductVariant",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LengthCm",
                table: "ProductVariant",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WeightGrams",
                table: "ProductVariant",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WidthCm",
                table: "ProductVariant",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HeightCm",
                table: "ProductVariant");

            migrationBuilder.DropColumn(
                name: "LengthCm",
                table: "ProductVariant");

            migrationBuilder.DropColumn(
                name: "WeightGrams",
                table: "ProductVariant");

            migrationBuilder.DropColumn(
                name: "WidthCm",
                table: "ProductVariant");

            migrationBuilder.AddColumn<int>(
                name: "HeightCm",
                table: "Product",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LengthCm",
                table: "Product",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WeightGrams",
                table: "Product",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WidthCm",
                table: "Product",
                type: "int",
                nullable: true);
        }
    }
}
