IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO


CREATE TABLE [Banner] (
    [BannerId] int NOT NULL IDENTITY,
    [Title] nvarchar(200) NULL,
    [ImageUrl] varchar(300) NULL,
    [LinkUrl] varchar(300) NULL,
    [SortOrder] int NULL DEFAULT 0,
    [StartDate] datetime NULL,
    [EndDate] datetime NULL,
    [IsActive] bit NULL DEFAULT CAST(1 AS bit),
    CONSTRAINT [PK__Banner__32E86AD1A7A5F8CA] PRIMARY KEY ([BannerId])
);
GO

CREATE TABLE [Category] (
    [CategoryId] int NOT NULL IDENTITY,
    [CategoryName] nvarchar(100) NOT NULL,
    [ParentCategoryId] int NULL,
    [Icon] varchar(100) NULL,
    [SortOrder] int NULL DEFAULT 0,
    [IsActive] bit NULL DEFAULT CAST(1 AS bit),
    CONSTRAINT [PK__Category__19093A0B5D699ACE] PRIMARY KEY ([CategoryId]),
    CONSTRAINT [FK__Category__Parent__60A75C0F] FOREIGN KEY ([ParentCategoryId]) REFERENCES [Category] ([CategoryId])
);
GO

CREATE TABLE [Role] (
    [RoleId] int NOT NULL IDENTITY,
    [RoleName] varchar(20) NOT NULL,
    [Description] nvarchar(200) NULL,
    [IsActive] bit NULL DEFAULT CAST(1 AS bit),
    CONSTRAINT [PK__Role__8AFACE1AC5D9D97A] PRIMARY KEY ([RoleId])
);
GO

CREATE TABLE [User] (
    [UserId] int NOT NULL IDENTITY,
    [RoleId] int NOT NULL,
    [Email] varchar(100) NOT NULL,
    [Phone] varchar(15) NULL,
    [Password] varchar(255) NOT NULL,
    [FullName] nvarchar(100) NULL,
    [Avatar] varchar(300) NULL,
    [LoyaltyPoints] int NULL DEFAULT 0,
    [WalletBalance] decimal(18,2) NULL DEFAULT 0.0,
    [IsActive] bit NULL DEFAULT CAST(1 AS bit),
    [CreatedAt] datetime NULL DEFAULT ((getdate())),
    CONSTRAINT [PK__User__1788CC4C9E28DE2E] PRIMARY KEY ([UserId]),
    CONSTRAINT [FK__User__RoleId__4D94879B] FOREIGN KEY ([RoleId]) REFERENCES [Role] ([RoleId])
);
GO

CREATE TABLE [Address] (
    [AddressId] int NOT NULL IDENTITY,
    [UserId] int NULL,
    [RecipientName] nvarchar(100) NULL,
    [Phone] varchar(15) NULL,
    [FullAddress] nvarchar(300) NULL,
    [Ward] nvarchar(100) NULL,
    [District] nvarchar(100) NULL,
    [Province] nvarchar(100) NULL,
    [Latitude] decimal(10,7) NULL,
    [Longitude] decimal(10,7) NULL,
    [IsDefault] bit NULL DEFAULT CAST(0 AS bit),
    CONSTRAINT [PK__Address__091C2AFB63B004CF] PRIMARY KEY ([AddressId]),
    CONSTRAINT [FK__Address__UserId__5441852A] FOREIGN KEY ([UserId]) REFERENCES [User] ([UserId])
);
GO

CREATE TABLE [Notification] (
    [NotificationId] int NOT NULL IDENTITY,
    [UserId] int NULL,
    [Title] nvarchar(200) NULL,
    [Content] nvarchar(500) NULL,
    [NotificationType] varchar(30) NULL,
    [RelatedId] int NULL,
    [IsRead] bit NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetime NULL DEFAULT ((getdate())),
    CONSTRAINT [PK__Notifica__20CF2E124C1DF14C] PRIMARY KEY ([NotificationId]),
    CONSTRAINT [FK__Notificat__UserI__41EDCAC5] FOREIGN KEY ([UserId]) REFERENCES [User] ([UserId])
);
GO

CREATE TABLE [Shop] (
    [ShopId] int NOT NULL IDENTITY,
    [UserId] int NULL,
    [ShopName] nvarchar(150) NOT NULL,
    [Logo] varchar(300) NULL,
    [WarehouseAddress] nvarchar(300) NULL,
    [CommissionRate] decimal(5,2) NULL DEFAULT 3.0,
    [WalletBalance] decimal(18,2) NULL DEFAULT 0.0,
    [Rating] decimal(3,2) NULL DEFAULT 0.0,
    [VacationMode] bit NULL DEFAULT CAST(0 AS bit),
    [IsActive] bit NULL DEFAULT CAST(1 AS bit),
    [OpenedAt] datetime NULL DEFAULT ((getdate())),
    CONSTRAINT [PK__Shop__67C557C9257EF4EB] PRIMARY KEY ([ShopId]),
    CONSTRAINT [FK__Shop__UserId__5812160E] FOREIGN KEY ([UserId]) REFERENCES [User] ([UserId])
);
GO

CREATE TABLE [Conversation] (
    [ConversationId] int NOT NULL IDENTITY,
    [BuyerId] int NULL,
    [ShopId] int NULL,
    [CreatedAt] datetime NULL DEFAULT ((getdate())),
    [LastMessageAt] datetime NULL,
    CONSTRAINT [PK__Conversa__C050D8773120EBA1] PRIMARY KEY ([ConversationId]),
    CONSTRAINT [FK__Conversat__Buyer__367C1819] FOREIGN KEY ([BuyerId]) REFERENCES [User] ([UserId]),
    CONSTRAINT [FK__Conversat__ShopI__37703C52] FOREIGN KEY ([ShopId]) REFERENCES [Shop] ([ShopId])
);
GO

CREATE TABLE [Product] (
    [ProductId] int NOT NULL IDENTITY,
    [ShopId] int NULL,
    [CategoryId] int NULL,
    [ProductName] nvarchar(200) NOT NULL,
    [Description] nvarchar(max) NULL,
    [Price] decimal(18,2) NOT NULL,
    [OriginalPrice] decimal(18,2) NULL,
    [StockQuantity] int NULL DEFAULT 0,
    [SoldCount] int NULL DEFAULT 0,
    [Rating] decimal(3,2) NULL DEFAULT 0.0,
    [Status] varchar(20) NULL DEFAULT 'Pending',
    [CreatedAt] datetime NULL DEFAULT ((getdate())),
    [ApprovedAt] datetime NULL,
    CONSTRAINT [PK__Product__B40CC6CDAF485082] PRIMARY KEY ([ProductId]),
    CONSTRAINT [FK__Product__Categor__66603565] FOREIGN KEY ([CategoryId]) REFERENCES [Category] ([CategoryId]),
    CONSTRAINT [FK__Product__ShopId__656C112C] FOREIGN KEY ([ShopId]) REFERENCES [Shop] ([ShopId])
);
GO

CREATE TABLE [Voucher] (
    [VoucherId] int NOT NULL IDENTITY,
    [ShopId] int NULL,
    [VoucherCode] varchar(50) NOT NULL,
    [VoucherName] nvarchar(150) NULL,
    [DiscountType] varchar(10) NULL,
    [DiscountValue] decimal(18,2) NULL,
    [MaxDiscount] decimal(18,2) NULL,
    [MinOrderValue] decimal(18,2) NULL DEFAULT 0.0,
    [TotalQuantity] int NULL,
    [UsedCount] int NULL DEFAULT 0,
    [StartDate] datetime NULL,
    [EndDate] datetime NULL,
    [IsActive] bit NULL DEFAULT CAST(1 AS bit),
    CONSTRAINT [PK__Voucher__3AEE7921F6A48E02] PRIMARY KEY ([VoucherId]),
    CONSTRAINT [FK__Voucher__ShopId__00200768] FOREIGN KEY ([ShopId]) REFERENCES [Shop] ([ShopId])
);
GO

CREATE TABLE [WithdrawRequest] (
    [WithdrawId] int NOT NULL IDENTITY,
    [ShopId] int NULL,
    [Amount] decimal(18,2) NULL,
    [BankName] nvarchar(100) NULL,
    [AccountNumber] varchar(30) NULL,
    [Status] varchar(20) NULL DEFAULT 'Pending',
    [RequestedAt] datetime NULL DEFAULT ((getdate())),
    [ProcessedAt] datetime NULL,
    CONSTRAINT [PK__Withdraw__435D94E2E5FB2A4B] PRIMARY KEY ([WithdrawId]),
    CONSTRAINT [FK__WithdrawR__ShopI__1F98B2C1] FOREIGN KEY ([ShopId]) REFERENCES [Shop] ([ShopId])
);
GO

CREATE TABLE [Message] (
    [MessageId] int NOT NULL IDENTITY,
    [ConversationId] int NULL,
    [SenderId] int NULL,
    [Content] nvarchar(1000) NULL,
    [MessageType] varchar(10) NULL DEFAULT 'Text',
    [SentAt] datetime NULL DEFAULT ((getdate())),
    [IsRead] bit NULL DEFAULT CAST(0 AS bit),
    CONSTRAINT [PK__Message__C87C0C9C1BD2916B] PRIMARY KEY ([MessageId]),
    CONSTRAINT [FK__Message__Convers__3B40CD36] FOREIGN KEY ([ConversationId]) REFERENCES [Conversation] ([ConversationId]),
    CONSTRAINT [FK__Message__SenderI__3C34F16F] FOREIGN KEY ([SenderId]) REFERENCES [User] ([UserId])
);
GO

CREATE TABLE [FlashSale] (
    [FlashSaleId] int NOT NULL IDENTITY,
    [ShopId] int NULL,
    [CampaignName] nvarchar(150) NULL,
    [ProductId] int NULL,
    [FlashPrice] decimal(18,2) NULL,
    [StockLimit] int NULL,
    [SoldCount] int NULL DEFAULT 0,
    [StartTime] datetime NULL,
    [EndTime] datetime NULL,
    [IsActive] bit NULL DEFAULT CAST(1 AS bit),
    CONSTRAINT [PK__FlashSal__D603A264BA0D561C] PRIMARY KEY ([FlashSaleId]),
    CONSTRAINT [FK__FlashSale__Produ__25518C17] FOREIGN KEY ([ProductId]) REFERENCES [Product] ([ProductId]),
    CONSTRAINT [FK__FlashSale__ShopI__245D67DE] FOREIGN KEY ([ShopId]) REFERENCES [Shop] ([ShopId])
);
GO

CREATE TABLE [ProductComparison] (
    [ComparisonId] int NOT NULL IDENTITY,
    [UserId] int NULL,
    [ProductId] int NULL,
    [AddedAt] datetime NULL DEFAULT ((getdate())),
    CONSTRAINT [PK__ProductC__6E1F99579E29919E] PRIMARY KEY ([ComparisonId]),
    CONSTRAINT [FK__ProductCo__Produ__65370702] FOREIGN KEY ([ProductId]) REFERENCES [Product] ([ProductId]),
    CONSTRAINT [FK__ProductCo__UserI__6442E2C9] FOREIGN KEY ([UserId]) REFERENCES [User] ([UserId])
);
GO

CREATE TABLE [ProductImage] (
    [ImageId] int NOT NULL IDENTITY,
    [ProductId] int NULL,
    [ImageUrl] varchar(300) NULL,
    [IsMain] bit NULL DEFAULT CAST(0 AS bit),
    [SortOrder] int NULL DEFAULT 0,
    CONSTRAINT [PK__ProductI__7516F70CCE0280C7] PRIMARY KEY ([ImageId]),
    CONSTRAINT [FK__ProductIm__Produ__6E01572D] FOREIGN KEY ([ProductId]) REFERENCES [Product] ([ProductId])
);
GO

CREATE TABLE [ProductVariant] (
    [VariantId] int NOT NULL IDENTITY,
    [ProductId] int NULL,
    [VariantName] nvarchar(100) NULL,
    [ExtraPrice] decimal(18,2) NULL DEFAULT 0.0,
    [Quantity] int NULL DEFAULT 0,
    [SKU] varchar(50) NULL,
    CONSTRAINT [PK__ProductV__0EA23384313BEF1D] PRIMARY KEY ([VariantId]),
    CONSTRAINT [FK__ProductVa__Produ__73BA3083] FOREIGN KEY ([ProductId]) REFERENCES [Product] ([ProductId])
);
GO

CREATE TABLE [ViewHistory] (
    [ViewHistoryId] int NOT NULL IDENTITY,
    [UserId] int NULL,
    [ProductId] int NULL,
    [ViewedAt] datetime NULL DEFAULT ((getdate())),
    CONSTRAINT [PK__ViewHist__55D4BB33AA69D42C] PRIMARY KEY ([ViewHistoryId]),
    CONSTRAINT [FK__ViewHisto__Produ__55009F39] FOREIGN KEY ([ProductId]) REFERENCES [Product] ([ProductId]),
    CONSTRAINT [FK__ViewHisto__UserI__540C7B00] FOREIGN KEY ([UserId]) REFERENCES [User] ([UserId])
);
GO

CREATE TABLE [Wishlist] (
    [WishlistId] int NOT NULL IDENTITY,
    [UserId] int NULL,
    [ProductId] int NULL,
    [AddedAt] datetime NULL DEFAULT ((getdate())),
    CONSTRAINT [PK__Wishlist__233189EB4189EBB8] PRIMARY KEY ([WishlistId]),
    CONSTRAINT [FK__Wishlist__Produc__47A6A41B] FOREIGN KEY ([ProductId]) REFERENCES [Product] ([ProductId]),
    CONSTRAINT [FK__Wishlist__UserId__46B27FE2] FOREIGN KEY ([UserId]) REFERENCES [User] ([UserId])
);
GO

CREATE TABLE [Order] (
    [OrderId] int NOT NULL IDENTITY,
    [OrderCode] varchar(20) NOT NULL,
    [BuyerId] int NULL,
    [ShopId] int NULL,
    [AddressId] int NULL,
    [VoucherId] int NULL,
    [SubTotal] decimal(18,2) NULL,
    [ShippingFee] decimal(18,2) NULL DEFAULT 0.0,
    [Discount] decimal(18,2) NULL DEFAULT 0.0,
    [TotalAmount] decimal(18,2) NULL,
    [PlatformFee] decimal(18,2) NULL DEFAULT 0.0,
    [PaymentMethod] varchar(20) NULL,
    [OrderStatus] varchar(20) NULL DEFAULT 'Pending',
    [TrackingCode] varchar(100) NULL,
    [Note] nvarchar(300) NULL,
    [OrderDate] datetime NULL DEFAULT ((getdate())),
    [CompletedAt] datetime NULL,
    CONSTRAINT [PK__Order__C3905BCF6E30DC2C] PRIMARY KEY ([OrderId]),
    CONSTRAINT [FK__Order__AddressId__09A971A2] FOREIGN KEY ([AddressId]) REFERENCES [Address] ([AddressId]),
    CONSTRAINT [FK__Order__BuyerId__07C12930] FOREIGN KEY ([BuyerId]) REFERENCES [User] ([UserId]),
    CONSTRAINT [FK__Order__ShopId__08B54D69] FOREIGN KEY ([ShopId]) REFERENCES [Shop] ([ShopId]),
    CONSTRAINT [FK__Order__VoucherId__0A9D95DB] FOREIGN KEY ([VoucherId]) REFERENCES [Voucher] ([VoucherId])
);
GO

CREATE TABLE [Cart] (
    [CartId] int NOT NULL IDENTITY,
    [UserId] int NULL,
    [ProductId] int NULL,
    [VariantId] int NULL,
    [Quantity] int NULL DEFAULT 1,
    [AddedAt] datetime NULL DEFAULT ((getdate())),
    CONSTRAINT [PK__Cart__51BCD7B7E57A7923] PRIMARY KEY ([CartId]),
    CONSTRAINT [FK__Cart__ProductId__797309D9] FOREIGN KEY ([ProductId]) REFERENCES [Product] ([ProductId]),
    CONSTRAINT [FK__Cart__UserId__787EE5A0] FOREIGN KEY ([UserId]) REFERENCES [User] ([UserId]),
    CONSTRAINT [FK__Cart__VariantId__7A672E12] FOREIGN KEY ([VariantId]) REFERENCES [ProductVariant] ([VariantId])
);
GO

CREATE TABLE [Complaint] (
    [ComplaintId] int NOT NULL IDENTITY,
    [OrderId] int NULL,
    [BuyerId] int NULL,
    [Content] nvarchar(500) NULL,
    [Status] varchar(20) NULL DEFAULT 'Open',
    [Resolution] nvarchar(300) NULL,
    [SubmittedAt] datetime NULL DEFAULT ((getdate())),
    [ResolvedAt] datetime NULL,
    CONSTRAINT [PK__Complain__740D898FCD802C7C] PRIMARY KEY ([ComplaintId]),
    CONSTRAINT [FK__Complaint__Buyer__5F7E2DAC] FOREIGN KEY ([BuyerId]) REFERENCES [User] ([UserId]),
    CONSTRAINT [FK__Complaint__Order__5E8A0973] FOREIGN KEY ([OrderId]) REFERENCES [Order] ([OrderId])
);
GO

CREATE TABLE [OrderDetail] (
    [OrderDetailId] int NOT NULL IDENTITY,
    [OrderId] int NULL,
    [ProductId] int NULL,
    [ProductNameSnapshot] nvarchar(200) NULL,
    [Quantity] int NULL,
    [UnitPrice] decimal(18,2) NULL,
    [TotalPrice] decimal(18,2) NULL,
    CONSTRAINT [PK__OrderDet__D3B9D36CF8F5AE95] PRIMARY KEY ([OrderDetailId]),
    CONSTRAINT [FK__OrderDeta__Order__123EB7A3] FOREIGN KEY ([OrderId]) REFERENCES [Order] ([OrderId]),
    CONSTRAINT [FK__OrderDeta__Produ__1332DBDC] FOREIGN KEY ([ProductId]) REFERENCES [Product] ([ProductId])
);
GO

CREATE TABLE [OrderStatusHistory] (
    [HistoryId] int NOT NULL IDENTITY,
    [OrderId] int NULL,
    [NewStatus] varchar(20) NULL,
    [Note] nvarchar(300) NULL,
    [ChangedAt] datetime NULL DEFAULT ((getdate())),
    CONSTRAINT [PK__OrderSta__4D7B4ABD9E213CDE] PRIMARY KEY ([HistoryId]),
    CONSTRAINT [FK__OrderStat__Order__160F4887] FOREIGN KEY ([OrderId]) REFERENCES [Order] ([OrderId])
);
GO

CREATE TABLE [Payment] (
    [PaymentId] int NOT NULL IDENTITY,
    [OrderId] int NULL,
    [Method] varchar(20) NULL,
    [Amount] decimal(18,2) NULL,
    [Status] varchar(20) NULL DEFAULT 'Pending',
    [TransactionCode] varchar(100) NULL,
    [PaidAt] datetime NULL DEFAULT ((getdate())),
    CONSTRAINT [PK__Payment__9B556A3817DD455A] PRIMARY KEY ([PaymentId]),
    CONSTRAINT [FK__Payment__OrderId__19DFD96B] FOREIGN KEY ([OrderId]) REFERENCES [Order] ([OrderId])
);
GO

CREATE TABLE [PointHistory] (
    [PointHistoryId] int NOT NULL IDENTITY,
    [UserId] int NULL,
    [Points] int NULL,
    [TransactionType] varchar(20) NULL,
    [OrderId] int NULL,
    [Description] nvarchar(200) NULL,
    [CreatedAt] datetime NULL DEFAULT ((getdate())),
    CONSTRAINT [PK__PointHis__DBE13F1733551875] PRIMARY KEY ([PointHistoryId]),
    CONSTRAINT [FK__PointHist__Order__4C6B5938] FOREIGN KEY ([OrderId]) REFERENCES [Order] ([OrderId]),
    CONSTRAINT [FK__PointHist__UserI__4B7734FF] FOREIGN KEY ([UserId]) REFERENCES [User] ([UserId])
);
GO

CREATE TABLE [ReturnRequest] (
    [ReturnId] int NOT NULL IDENTITY,
    [OrderDetailId] int NULL,
    [BuyerId] int NULL,
    [Reason] nvarchar(300) NULL,
    [EvidenceImage] varchar(300) NULL,
    [Status] varchar(20) NULL DEFAULT 'Pending',
    [RequestedAt] datetime NULL DEFAULT ((getdate())),
    [ProcessedAt] datetime NULL,
    CONSTRAINT [PK__ReturnRe__F445E9A874A9339D] PRIMARY KEY ([ReturnId]),
    CONSTRAINT [FK__ReturnReq__Buyer__59C55456] FOREIGN KEY ([BuyerId]) REFERENCES [User] ([UserId]),
    CONSTRAINT [FK__ReturnReq__Order__58D1301D] FOREIGN KEY ([OrderDetailId]) REFERENCES [OrderDetail] ([OrderDetailId])
);
GO

CREATE TABLE [Review] (
    [ReviewId] int NOT NULL IDENTITY,
    [OrderDetailId] int NULL,
    [ProductId] int NULL,
    [UserId] int NULL,
    [StarRating] tinyint NULL,
    [Content] nvarchar(500) NULL,
    [ImageUrl] varchar(300) NULL,
    [IsHidden] bit NULL DEFAULT CAST(0 AS bit),
    [ReviewedAt] datetime NULL DEFAULT ((getdate())),
    CONSTRAINT [PK__Review__74BC79CEA79CB0D1] PRIMARY KEY ([ReviewId]),
    CONSTRAINT [FK__Review__OrderDet__2A164134] FOREIGN KEY ([OrderDetailId]) REFERENCES [OrderDetail] ([OrderDetailId]),
    CONSTRAINT [FK__Review__ProductI__2B0A656D] FOREIGN KEY ([ProductId]) REFERENCES [Product] ([ProductId]),
    CONSTRAINT [FK__Review__UserId__2BFE89A6] FOREIGN KEY ([UserId]) REFERENCES [User] ([UserId])
);
GO

CREATE TABLE [ReviewReply] (
    [ReplyId] int NOT NULL IDENTITY,
    [ReviewId] int NULL,
    [UserId] int NULL,
    [Content] nvarchar(500) NULL,
    [RepliedAt] datetime NULL DEFAULT ((getdate())),
    CONSTRAINT [PK__ReviewRe__C25E4609DEEF4BF6] PRIMARY KEY ([ReplyId]),
    CONSTRAINT [FK__ReviewRep__Revie__31B762FC] FOREIGN KEY ([ReviewId]) REFERENCES [Review] ([ReviewId]),
    CONSTRAINT [FK__ReviewRep__UserI__32AB8735] FOREIGN KEY ([UserId]) REFERENCES [User] ([UserId])
);
GO

CREATE INDEX [IX_Address_UserId] ON [Address] ([UserId]);
GO

CREATE INDEX [IX_Cart_ProductId] ON [Cart] ([ProductId]);
GO

CREATE INDEX [IX_Cart_UserId] ON [Cart] ([UserId]);
GO

CREATE INDEX [IX_Cart_VariantId] ON [Cart] ([VariantId]);
GO

CREATE INDEX [IX_Category_ParentCategoryId] ON [Category] ([ParentCategoryId]);
GO

CREATE INDEX [IX_Complaint_BuyerId] ON [Complaint] ([BuyerId]);
GO

CREATE INDEX [IX_Complaint_OrderId] ON [Complaint] ([OrderId]);
GO

CREATE INDEX [IX_Conversation_BuyerId] ON [Conversation] ([BuyerId]);
GO

CREATE INDEX [IX_Conversation_ShopId] ON [Conversation] ([ShopId]);
GO

CREATE INDEX [IX_FlashSale_ProductId] ON [FlashSale] ([ProductId]);
GO

CREATE INDEX [IX_FlashSale_ShopId] ON [FlashSale] ([ShopId]);
GO

CREATE INDEX [IX_Message_ConversationId] ON [Message] ([ConversationId]);
GO

CREATE INDEX [IX_Message_SenderId] ON [Message] ([SenderId]);
GO

CREATE INDEX [IX_Notification_UserId] ON [Notification] ([UserId]);
GO

CREATE INDEX [IX_Order_AddressId] ON [Order] ([AddressId]);
GO

CREATE INDEX [IX_Order_BuyerId] ON [Order] ([BuyerId]);
GO

CREATE INDEX [IX_Order_ShopId] ON [Order] ([ShopId]);
GO

CREATE INDEX [IX_Order_VoucherId] ON [Order] ([VoucherId]);
GO

CREATE UNIQUE INDEX [UQ__Order__999B5229F6F9AC2B] ON [Order] ([OrderCode]);
GO

CREATE INDEX [IX_OrderDetail_OrderId] ON [OrderDetail] ([OrderId]);
GO

CREATE INDEX [IX_OrderDetail_ProductId] ON [OrderDetail] ([ProductId]);
GO

CREATE INDEX [IX_OrderStatusHistory_OrderId] ON [OrderStatusHistory] ([OrderId]);
GO

CREATE INDEX [IX_Payment_OrderId] ON [Payment] ([OrderId]);
GO

CREATE INDEX [IX_PointHistory_OrderId] ON [PointHistory] ([OrderId]);
GO

CREATE INDEX [IX_PointHistory_UserId] ON [PointHistory] ([UserId]);
GO

CREATE INDEX [IX_Product_CategoryId] ON [Product] ([CategoryId]);
GO

CREATE INDEX [IX_Product_ShopId] ON [Product] ([ShopId]);
GO

CREATE INDEX [IX_ProductComparison_ProductId] ON [ProductComparison] ([ProductId]);
GO

CREATE INDEX [IX_ProductComparison_UserId] ON [ProductComparison] ([UserId]);
GO

CREATE INDEX [IX_ProductImage_ProductId] ON [ProductImage] ([ProductId]);
GO

CREATE INDEX [IX_ProductVariant_ProductId] ON [ProductVariant] ([ProductId]);
GO

CREATE UNIQUE INDEX [UQ__ProductV__CA1ECF0D010D6B02] ON [ProductVariant] ([SKU]) WHERE [SKU] IS NOT NULL;
GO

CREATE INDEX [IX_ReturnRequest_BuyerId] ON [ReturnRequest] ([BuyerId]);
GO

CREATE INDEX [IX_ReturnRequest_OrderDetailId] ON [ReturnRequest] ([OrderDetailId]);
GO

CREATE INDEX [IX_Review_OrderDetailId] ON [Review] ([OrderDetailId]);
GO

CREATE INDEX [IX_Review_ProductId] ON [Review] ([ProductId]);
GO

CREATE INDEX [IX_Review_UserId] ON [Review] ([UserId]);
GO

CREATE INDEX [IX_ReviewReply_ReviewId] ON [ReviewReply] ([ReviewId]);
GO

CREATE INDEX [IX_ReviewReply_UserId] ON [ReviewReply] ([UserId]);
GO

CREATE INDEX [IX_Shop_UserId] ON [Shop] ([UserId]);
GO

CREATE INDEX [IX_User_RoleId] ON [User] ([RoleId]);
GO

CREATE UNIQUE INDEX [UQ__User__A9D105348039FAC4] ON [User] ([Email]);
GO

CREATE INDEX [IX_ViewHistory_ProductId] ON [ViewHistory] ([ProductId]);
GO

CREATE INDEX [IX_ViewHistory_UserId] ON [ViewHistory] ([UserId]);
GO

CREATE INDEX [IX_Voucher_ShopId] ON [Voucher] ([ShopId]);
GO

CREATE UNIQUE INDEX [UQ__Voucher__7F0ABCA95463306C] ON [Voucher] ([VoucherCode]);
GO

CREATE INDEX [IX_Wishlist_ProductId] ON [Wishlist] ([ProductId]);
GO

CREATE INDEX [IX_Wishlist_UserId] ON [Wishlist] ([UserId]);
GO

CREATE INDEX [IX_WithdrawRequest_ShopId] ON [WithdrawRequest] ([ShopId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260516163359_InitialSomeeDB', N'8.0.13');
GO

COMMIT;
GO

