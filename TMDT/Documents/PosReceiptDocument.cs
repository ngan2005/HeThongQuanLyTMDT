using System;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TMDT.Models;
using TMDT.ViewModels.Seller;

namespace TMDT.Documents
{
    public class PosReceiptDocument : IDocument
    {
        private readonly Order _order;
        private readonly decimal _givenAmount;
        private readonly decimal _changeAmount;
        private readonly string _cashierName;

        public PosReceiptDocument(Order order, decimal givenAmount, decimal changeAmount, string cashierName)
        {
            _order = order;
            _givenAmount = givenAmount;
            _changeAmount = changeAmount;
            _cashierName = cashierName;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            // Khổ giấy 80mm in nhiệt (Width = 226 points)
            container
                .Page(page =>
                {
                    page.Size(226, PageSizes.A4.Height);
                    page.Margin(10);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    page.Header().Element(ComposeHeader);
                    page.Content().Element(ComposeContent);
                    page.Footer().Element(ComposeFooter);
                });
        }

        private void ComposeHeader(IContainer container)
        {
            container.Column(column =>
            {
                column.Item().AlignCenter().Text("HỆ THỐNG TMĐT").FontSize(14).SemiBold();
                column.Item().AlignCenter().Text("--- HÓA ĐƠN BÁN LẺ ---").FontSize(10);
                column.Item().PaddingBottom(10);

                column.Item().Text($"Số phiếu: {_order.OrderCode}");
                column.Item().Text($"Ngày: {_order.OrderDate:dd/MM/yyyy HH:mm}");
                column.Item().Text($"Thu ngân: {_cashierName}");
            });
        }

        private void ComposeContent(IContainer container)
        {
            container.PaddingVertical(10).Column(column =>
            {
                column.Item().Element(ComposeTable);
                column.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                column.Item().Element(ComposeTotals);
            });
        }

        private void ComposeTable(IContainer container)
        {
            container.Table(table =>
            {
                // Cấu trúc cột: 
                // Tên SP (mở rộng), SL, Thành tiền
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3); // Tên SP
                    columns.RelativeColumn(1); // SL
                    columns.RelativeColumn(2); // TT
                });

                table.Header(header =>
                {
                    header.Cell().Text("Tên hàng").SemiBold();
                    header.Cell().AlignCenter().Text("SL").SemiBold();
                    header.Cell().AlignRight().Text("T.Tiền").SemiBold();

                    header.Cell().ColumnSpan(3).PaddingVertical(2).LineHorizontal(1).LineColor(Colors.Black);
                });

                foreach (var detail in _order.OrderDetails)
                {
                    table.Cell().Text(detail.Product?.ProductName ?? "Sản phẩm").FontSize(9);
                    table.Cell().AlignCenter().Text(detail.Quantity.ToString()).FontSize(9);
                    table.Cell().AlignRight().Text($"{(detail.UnitPrice * detail.Quantity):N0}").FontSize(9);
                }
            });
        }

        private void ComposeTotals(IContainer container)
        {
            container.Column(column =>
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Text("Tổng cộng:");
                    row.RelativeItem().AlignRight().Text($"{_order.SubTotal:N0} đ");
                });

                if (_order.Discount > 0)
                {
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Giảm giá:");
                        row.RelativeItem().AlignRight().Text($"-{_order.Discount:N0} đ");
                    });
                }

                column.Item().Row(row =>
                {
                    row.RelativeItem().Text("Thanh toán:").SemiBold().FontSize(12);
                    row.RelativeItem().AlignRight().Text($"{_order.TotalAmount:N0} đ").SemiBold().FontSize(12);
                });

                column.Item().PaddingTop(5);
                column.Item().Row(row =>
                {
                    row.RelativeItem().Text("Hình thức:");
                    row.RelativeItem().AlignRight().Text(_order.PaymentMethod ?? "Tiền mặt");
                });
                
                // Tiền khách đưa / Thối lại
                if (_order.PaymentMethod == "Tiền mặt" || _order.PaymentMethod == "COD" || _order.PaymentMethod == "POS_Cash")
                {
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Khách đưa:");
                        row.RelativeItem().AlignRight().Text($"{_givenAmount:N0} đ");
                    });

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Thối lại:");
                        row.RelativeItem().AlignRight().Text($"{_changeAmount:N0} đ");
                    });
                }
            });
        }

        private void ComposeFooter(IContainer container)
        {
            container.AlignCenter().Column(column =>
            {
                column.Item().PaddingTop(8).AlignCenter().Text($"Thu ngân: {_cashierName}").FontSize(9).SemiBold();
                column.Item().AlignCenter().Text("CẢM ƠN QUÝ KHÁCH").SemiBold();
                column.Item().AlignCenter().Text("HẸN GẶP LẠI!").FontSize(9);
                // Bạn có thể thêm Barcode ở đây nếu có font hỗ trợ hoặc image:
                // column.Item().PaddingTop(10).AlignCenter().Text($"*{_order.OrderCode}*").FontFamily("Libre Barcode 39");
            });
        }
    }
}
