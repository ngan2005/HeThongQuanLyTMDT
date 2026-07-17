using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMDT.Migrations
{
    /// <summary>
    /// 🟢 Bảng ConfigChangeLog — lưu lịch sử thay đổi config (phí sàn global + per-shop).
    /// Cho phép audit "ai đổi rate từ X → Y lúc nào, lý do gì".
    /// </summary>
    public partial class AddConfigChangeLogTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConfigChangeLog",
                columns: table => new
                {
                    LogId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConfigType = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    TargetId = table.Column<int>(type: "int", nullable: true),
                    ConfigKey = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    OldValue = table.Column<decimal>(type: "decimal(5, 2)", nullable: true),
                    NewValue = table.Column<decimal>(type: "decimal(5, 2)", nullable: true),
                    ChangedBy = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfigChangeLog", x => x.LogId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConfigChangeLog_Type_Target_Key_At",
                table: "ConfigChangeLog",
                columns: new[] { "ConfigType", "TargetId", "ConfigKey", "ChangedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfigChangeLog");
        }
    }
}
