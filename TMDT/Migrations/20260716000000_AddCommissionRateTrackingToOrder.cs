using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMDT.Migrations
{
    /// <summary>
    /// 🟢 Audit phí sàn: snapshot rate đã áp dụng + nguồn rate (Shop/Global) vào Order.
    /// - AppliedCommissionRate: % đã dùng (vd 5.0)
    /// - CommissionRateSource: "Shop" hoặc "Global"
    /// Cho phép audit khi admin thay đổi rate — đơn cũ giữ nguyên rate lúc tạo.
    /// </summary>
    public partial class AddCommissionRateTrackingToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AppliedCommissionRate",
                table: "Order",
                type: "decimal(5, 2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CommissionRateSource",
                table: "Order",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: true);

            // 🟢 Backfill: gán rate hiện tại (global) cho đơn cũ để tránh null khi hiển thị audit
            migrationBuilder.Sql(@"
                UPDATE o
                SET o.AppliedCommissionRate = s.CommissionRate,
                    o.CommissionRateSource  = CASE WHEN s.CommissionRate IS NOT NULL THEN 'Shop' ELSE 'Global' END
                FROM [Order] o
                LEFT JOIN Shop s ON o.ShopId = s.ShopId
                WHERE o.AppliedCommissionRate IS NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CommissionRateSource",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "AppliedCommissionRate",
                table: "Order");
        }
    }
}
