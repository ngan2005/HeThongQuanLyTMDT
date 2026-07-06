namespace TMDT.DTOs;

public class CreateOrderRequest
{
    public int BuyerId { get; set; }
    public int ShopId { get; set; }
    public int? AddressId { get; set; }
    public int? VoucherId { get; set; }
    public string PaymentMethod { get; set; } = "COD";
    public decimal ShippingFee { get; set; } = 25000m;
    public List<CreateOrderItemRequest> Items { get; set; } = new();
}

public class CreateOrderItemRequest
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public class OrderDto
{
    public int OrderId { get; set; }
    public string OrderCode { get; set; } = "";
    public string? OrderStatus { get; set; }
    public string? PaymentMethod { get; set; }
    public decimal? SubTotal { get; set; }
    public decimal? ShippingFee { get; set; }
    public decimal? Discount { get; set; }
    public decimal? TotalAmount { get; set; }
    public string? TrackingCode { get; set; }
    public DateTime? OrderDate { get; set; }
    public DateTime? CompletedAt { get; set; }

    public string? BuyerName { get; set; }
    public string? ShopName { get; set; }
    public string? ShippingAddress { get; set; }

    public List<OrderDetailDto> Details { get; set; } = new();
    public List<OrderStatusHistoryDto> StatusHistory { get; set; } = new();
}

public class OrderDetailDto
{
    public int OrderDetailId { get; set; }
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string? ProductImage { get; set; }
}

public class OrderStatusHistoryDto
{
    public int HistoryId { get; set; }
    public string? NewStatus { get; set; }
    public string? Note { get; set; }
    public DateTime? ChangedAt { get; set; }
}

public class CartOrderItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public int? VariantId { get; set; }
    public string? VariantName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public class CreateOrderBatchRequest
{
    public List<CreateOrderFromCartRequest> Orders { get; set; } = new();
}

public class CreateOrderFromCartRequest
{
    public int BuyerId { get; set; }
    public int ShopId { get; set; }
    public int? AddressId { get; set; }
    public int? VoucherId { get; set; }
    public string PaymentMethod { get; set; } = "COD";
    public decimal ShippingFee { get; set; } = 25000m;
    public decimal SubTotal { get; set; }
    public List<CartOrderItem> Items { get; set; } = new();
}
