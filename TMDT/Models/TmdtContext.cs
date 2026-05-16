using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace TMDT.Models;

public partial class TmdtContext : DbContext
{
    public TmdtContext()
    {
    }

    public TmdtContext(DbContextOptions<TmdtContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Address> Addresses { get; set; }

    public virtual DbSet<Banner> Banners { get; set; }

    public virtual DbSet<Cart> Carts { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Complaint> Complaints { get; set; }

    public virtual DbSet<Conversation> Conversations { get; set; }

    public virtual DbSet<FlashSale> FlashSales { get; set; }

    public virtual DbSet<Message> Messages { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderDetail> OrderDetails { get; set; }

    public virtual DbSet<OrderStatusHistory> OrderStatusHistories { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<PointHistory> PointHistories { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductComparison> ProductComparisons { get; set; }

    public virtual DbSet<ProductImage> ProductImages { get; set; }

    public virtual DbSet<ProductVariant> ProductVariants { get; set; }

    public virtual DbSet<ReturnRequest> ReturnRequests { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<ReviewReply> ReviewReplies { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Shop> Shops { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<ViewHistory> ViewHistories { get; set; }

    public virtual DbSet<Voucher> Vouchers { get; set; }

    public virtual DbSet<Wishlist> Wishlists { get; set; }

    public virtual DbSet<WithdrawRequest> WithdrawRequests { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("workstation id=nhom8tmdt.mssql.somee.com;packet size=4096;user id=LuuThiKimNgan_SQLLogin_1;pwd=2mso97g2lj;data source=nhom8tmdt.mssql.somee.com;persist security info=False;initial catalog=nhom8tmdt;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Address>(entity =>
        {
            entity.HasKey(e => e.AddressId).HasName("PK__Address__091C2AFB63B004CF");

            entity.ToTable("Address");

            entity.Property(e => e.District).HasMaxLength(100);
            entity.Property(e => e.FullAddress).HasMaxLength(300);
            entity.Property(e => e.IsDefault).HasDefaultValue(false);
            entity.Property(e => e.Latitude).HasColumnType("decimal(10, 7)");
            entity.Property(e => e.Longitude).HasColumnType("decimal(10, 7)");
            entity.Property(e => e.Phone)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.Province).HasMaxLength(100);
            entity.Property(e => e.RecipientName).HasMaxLength(100);
            entity.Property(e => e.Ward).HasMaxLength(100);

            entity.HasOne(d => d.User).WithMany(p => p.Addresses)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Address__UserId__5441852A");
        });

        modelBuilder.Entity<Banner>(entity =>
        {
            entity.HasKey(e => e.BannerId).HasName("PK__Banner__32E86AD1A7A5F8CA");

            entity.ToTable("Banner");

            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.LinkUrl)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.SortOrder).HasDefaultValue(0);
            entity.Property(e => e.StartDate).HasColumnType("datetime");
            entity.Property(e => e.Title).HasMaxLength(200);
        });

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(e => e.CartId).HasName("PK__Cart__51BCD7B7E57A7923");

            entity.ToTable("Cart");

            entity.Property(e => e.AddedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Quantity).HasDefaultValue(1);

            entity.HasOne(d => d.Product).WithMany(p => p.Carts)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK__Cart__ProductId__797309D9");

            entity.HasOne(d => d.User).WithMany(p => p.Carts)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Cart__UserId__787EE5A0");

            entity.HasOne(d => d.Variant).WithMany(p => p.Carts)
                .HasForeignKey(d => d.VariantId)
                .HasConstraintName("FK__Cart__VariantId__7A672E12");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__Category__19093A0B5D699ACE");

            entity.ToTable("Category");

            entity.Property(e => e.CategoryName).HasMaxLength(100);
            entity.Property(e => e.Icon)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.SortOrder).HasDefaultValue(0);

            entity.HasOne(d => d.ParentCategory).WithMany(p => p.InverseParentCategory)
                .HasForeignKey(d => d.ParentCategoryId)
                .HasConstraintName("FK__Category__Parent__60A75C0F");
        });

        modelBuilder.Entity<Complaint>(entity =>
        {
            entity.HasKey(e => e.ComplaintId).HasName("PK__Complain__740D898FCD802C7C");

            entity.ToTable("Complaint");

            entity.Property(e => e.Content).HasMaxLength(500);
            entity.Property(e => e.Resolution).HasMaxLength(300);
            entity.Property(e => e.ResolvedAt).HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Open");
            entity.Property(e => e.SubmittedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Buyer).WithMany(p => p.Complaints)
                .HasForeignKey(d => d.BuyerId)
                .HasConstraintName("FK__Complaint__Buyer__5F7E2DAC");

            entity.HasOne(d => d.Order).WithMany(p => p.Complaints)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK__Complaint__Order__5E8A0973");
        });

        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(e => e.ConversationId).HasName("PK__Conversa__C050D8773120EBA1");

            entity.ToTable("Conversation");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.LastMessageAt).HasColumnType("datetime");

            entity.HasOne(d => d.Buyer).WithMany(p => p.Conversations)
                .HasForeignKey(d => d.BuyerId)
                .HasConstraintName("FK__Conversat__Buyer__367C1819");

            entity.HasOne(d => d.Shop).WithMany(p => p.Conversations)
                .HasForeignKey(d => d.ShopId)
                .HasConstraintName("FK__Conversat__ShopI__37703C52");
        });

        modelBuilder.Entity<FlashSale>(entity =>
        {
            entity.HasKey(e => e.FlashSaleId).HasName("PK__FlashSal__D603A264BA0D561C");

            entity.ToTable("FlashSale");

            entity.Property(e => e.CampaignName).HasMaxLength(150);
            entity.Property(e => e.EndTime).HasColumnType("datetime");
            entity.Property(e => e.FlashPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.SoldCount).HasDefaultValue(0);
            entity.Property(e => e.StartTime).HasColumnType("datetime");

            entity.HasOne(d => d.Product).WithMany(p => p.FlashSales)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK__FlashSale__Produ__25518C17");

            entity.HasOne(d => d.Shop).WithMany(p => p.FlashSales)
                .HasForeignKey(d => d.ShopId)
                .HasConstraintName("FK__FlashSale__ShopI__245D67DE");
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.MessageId).HasName("PK__Message__C87C0C9C1BD2916B");

            entity.ToTable("Message");

            entity.Property(e => e.Content).HasMaxLength(1000);
            entity.Property(e => e.IsRead).HasDefaultValue(false);
            entity.Property(e => e.MessageType)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("Text");
            entity.Property(e => e.SentAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Conversation).WithMany(p => p.Messages)
                .HasForeignKey(d => d.ConversationId)
                .HasConstraintName("FK__Message__Convers__3B40CD36");

            entity.HasOne(d => d.Sender).WithMany(p => p.Messages)
                .HasForeignKey(d => d.SenderId)
                .HasConstraintName("FK__Message__SenderI__3C34F16F");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__Notifica__20CF2E124C1DF14C");

            entity.ToTable("Notification");

            entity.Property(e => e.Content).HasMaxLength(500);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsRead).HasDefaultValue(false);
            entity.Property(e => e.NotificationType)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Title).HasMaxLength(200);

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Notificat__UserI__41EDCAC5");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__Order__C3905BCF6E30DC2C");

            entity.ToTable("Order");

            entity.HasIndex(e => e.OrderCode, "UQ__Order__999B5229F6F9AC2B").IsUnique();

            entity.Property(e => e.CompletedAt).HasColumnType("datetime");
            entity.Property(e => e.Discount)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Note).HasMaxLength(300);
            entity.Property(e => e.OrderCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.OrderDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.OrderStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Pending");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.PlatformFee)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ShippingFee)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.SubTotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TrackingCode)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.Address).WithMany(p => p.Orders)
                .HasForeignKey(d => d.AddressId)
                .HasConstraintName("FK__Order__AddressId__09A971A2");

            entity.HasOne(d => d.Buyer).WithMany(p => p.Orders)
                .HasForeignKey(d => d.BuyerId)
                .HasConstraintName("FK__Order__BuyerId__07C12930");

            entity.HasOne(d => d.Shop).WithMany(p => p.Orders)
                .HasForeignKey(d => d.ShopId)
                .HasConstraintName("FK__Order__ShopId__08B54D69");

            entity.HasOne(d => d.Voucher).WithMany(p => p.Orders)
                .HasForeignKey(d => d.VoucherId)
                .HasConstraintName("FK__Order__VoucherId__0A9D95DB");
        });

        modelBuilder.Entity<OrderDetail>(entity =>
        {
            entity.HasKey(e => e.OrderDetailId).HasName("PK__OrderDet__D3B9D36CF8F5AE95");

            entity.ToTable("OrderDetail");

            entity.Property(e => e.ProductNameSnapshot).HasMaxLength(200);
            entity.Property(e => e.TotalPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK__OrderDeta__Order__123EB7A3");

            entity.HasOne(d => d.Product).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK__OrderDeta__Produ__1332DBDC");
        });

        modelBuilder.Entity<OrderStatusHistory>(entity =>
        {
            entity.HasKey(e => e.HistoryId).HasName("PK__OrderSta__4D7B4ABD9E213CDE");

            entity.ToTable("OrderStatusHistory");

            entity.Property(e => e.ChangedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.NewStatus)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Note).HasMaxLength(300);

            entity.HasOne(d => d.Order).WithMany(p => p.OrderStatusHistories)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK__OrderStat__Order__160F4887");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__Payment__9B556A3817DD455A");

            entity.ToTable("Payment");

            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Method)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.PaidAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Pending");
            entity.Property(e => e.TransactionCode)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.Order).WithMany(p => p.Payments)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK__Payment__OrderId__19DFD96B");
        });

        modelBuilder.Entity<PointHistory>(entity =>
        {
            entity.HasKey(e => e.PointHistoryId).HasName("PK__PointHis__DBE13F1733551875");

            entity.ToTable("PointHistory");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.Property(e => e.TransactionType)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.Order).WithMany(p => p.PointHistories)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK__PointHist__Order__4C6B5938");

            entity.HasOne(d => d.User).WithMany(p => p.PointHistories)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__PointHist__UserI__4B7734FF");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("PK__Product__B40CC6CDAF485082");

            entity.ToTable("Product");

            entity.Property(e => e.ApprovedAt).HasColumnType("datetime");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.OriginalPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ProductName).HasMaxLength(200);
            entity.Property(e => e.Rating)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(3, 2)");
            entity.Property(e => e.SoldCount).HasDefaultValue(0);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Pending");
            entity.Property(e => e.StockQuantity).HasDefaultValue(0);

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK__Product__Categor__66603565");

            entity.HasOne(d => d.Shop).WithMany(p => p.Products)
                .HasForeignKey(d => d.ShopId)
                .HasConstraintName("FK__Product__ShopId__656C112C");
        });

        modelBuilder.Entity<ProductComparison>(entity =>
        {
            entity.HasKey(e => e.ComparisonId).HasName("PK__ProductC__6E1F99579E29919E");

            entity.ToTable("ProductComparison");

            entity.Property(e => e.AddedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductComparisons)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK__ProductCo__Produ__65370702");

            entity.HasOne(d => d.User).WithMany(p => p.ProductComparisons)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__ProductCo__UserI__6442E2C9");
        });

        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.HasKey(e => e.ImageId).HasName("PK__ProductI__7516F70CCE0280C7");

            entity.ToTable("ProductImage");

            entity.Property(e => e.ImageUrl)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.IsMain).HasDefaultValue(false);
            entity.Property(e => e.SortOrder).HasDefaultValue(0);

            entity.HasOne(d => d.Product).WithMany(p => p.ProductImages)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK__ProductIm__Produ__6E01572D");
        });

        modelBuilder.Entity<ProductVariant>(entity =>
        {
            entity.HasKey(e => e.VariantId).HasName("PK__ProductV__0EA23384313BEF1D");

            entity.ToTable("ProductVariant");

            entity.HasIndex(e => e.Sku, "UQ__ProductV__CA1ECF0D010D6B02").IsUnique();

            entity.Property(e => e.ExtraPrice)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Quantity).HasDefaultValue(0);
            entity.Property(e => e.Sku)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("SKU");
            entity.Property(e => e.VariantName).HasMaxLength(100);

            entity.HasOne(d => d.Product).WithMany(p => p.ProductVariants)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK__ProductVa__Produ__73BA3083");
        });

        modelBuilder.Entity<ReturnRequest>(entity =>
        {
            entity.HasKey(e => e.ReturnId).HasName("PK__ReturnRe__F445E9A874A9339D");

            entity.ToTable("ReturnRequest");

            entity.Property(e => e.EvidenceImage)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.ProcessedAt).HasColumnType("datetime");
            entity.Property(e => e.Reason).HasMaxLength(300);
            entity.Property(e => e.RequestedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Pending");

            entity.HasOne(d => d.Buyer).WithMany(p => p.ReturnRequests)
                .HasForeignKey(d => d.BuyerId)
                .HasConstraintName("FK__ReturnReq__Buyer__59C55456");

            entity.HasOne(d => d.OrderDetail).WithMany(p => p.ReturnRequests)
                .HasForeignKey(d => d.OrderDetailId)
                .HasConstraintName("FK__ReturnReq__Order__58D1301D");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.ReviewId).HasName("PK__Review__74BC79CEA79CB0D1");

            entity.ToTable("Review");

            entity.Property(e => e.Content).HasMaxLength(500);
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.IsHidden).HasDefaultValue(false);
            entity.Property(e => e.ReviewedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.OrderDetail).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.OrderDetailId)
                .HasConstraintName("FK__Review__OrderDet__2A164134");

            entity.HasOne(d => d.Product).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK__Review__ProductI__2B0A656D");

            entity.HasOne(d => d.User).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Review__UserId__2BFE89A6");
        });

        modelBuilder.Entity<ReviewReply>(entity =>
        {
            entity.HasKey(e => e.ReplyId).HasName("PK__ReviewRe__C25E4609DEEF4BF6");

            entity.ToTable("ReviewReply");

            entity.Property(e => e.Content).HasMaxLength(500);
            entity.Property(e => e.RepliedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Review).WithMany(p => p.ReviewReplies)
                .HasForeignKey(d => d.ReviewId)
                .HasConstraintName("FK__ReviewRep__Revie__31B762FC");

            entity.HasOne(d => d.User).WithMany(p => p.ReviewReplies)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__ReviewRep__UserI__32AB8735");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Role__8AFACE1AC5D9D97A");

            entity.ToTable("Role");

            entity.Property(e => e.Description).HasMaxLength(200);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.RoleName)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Shop>(entity =>
        {
            entity.HasKey(e => e.ShopId).HasName("PK__Shop__67C557C9257EF4EB");

            entity.ToTable("Shop");

            entity.Property(e => e.CommissionRate)
                .HasDefaultValue(3.0m)
                .HasColumnType("decimal(5, 2)");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Logo)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.OpenedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Rating)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(3, 2)");
            entity.Property(e => e.ShopName).HasMaxLength(150);
            entity.Property(e => e.VacationMode).HasDefaultValue(false);
            entity.Property(e => e.WalletBalance)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.WarehouseAddress).HasMaxLength(300);

            entity.HasOne(d => d.User).WithMany(p => p.Shops)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Shop__UserId__5812160E");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__User__1788CC4C9E28DE2E");

            entity.ToTable("User");

            entity.HasIndex(e => e.Email, "UQ__User__A9D105348039FAC4").IsUnique();

            entity.Property(e => e.Avatar)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.LoyaltyPoints).HasDefaultValue(0);
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Phone)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.WalletBalance)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__User__RoleId__4D94879B");
        });

        modelBuilder.Entity<ViewHistory>(entity =>
        {
            entity.HasKey(e => e.ViewHistoryId).HasName("PK__ViewHist__55D4BB33AA69D42C");

            entity.ToTable("ViewHistory");

            entity.Property(e => e.ViewedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Product).WithMany(p => p.ViewHistories)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK__ViewHisto__Produ__55009F39");

            entity.HasOne(d => d.User).WithMany(p => p.ViewHistories)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__ViewHisto__UserI__540C7B00");
        });

        modelBuilder.Entity<Voucher>(entity =>
        {
            entity.HasKey(e => e.VoucherId).HasName("PK__Voucher__3AEE7921F6A48E02");

            entity.ToTable("Voucher");

            entity.HasIndex(e => e.VoucherCode, "UQ__Voucher__7F0ABCA95463306C").IsUnique();

            entity.Property(e => e.DiscountType)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.DiscountValue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.MaxDiscount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.MinOrderValue)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.StartDate).HasColumnType("datetime");
            entity.Property(e => e.UsedCount).HasDefaultValue(0);
            entity.Property(e => e.VoucherCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.VoucherName).HasMaxLength(150);

            entity.HasOne(d => d.Shop).WithMany(p => p.Vouchers)
                .HasForeignKey(d => d.ShopId)
                .HasConstraintName("FK__Voucher__ShopId__00200768");
        });

        modelBuilder.Entity<Wishlist>(entity =>
        {
            entity.HasKey(e => e.WishlistId).HasName("PK__Wishlist__233189EB4189EBB8");

            entity.ToTable("Wishlist");

            entity.Property(e => e.AddedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Product).WithMany(p => p.Wishlists)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK__Wishlist__Produc__47A6A41B");

            entity.HasOne(d => d.User).WithMany(p => p.Wishlists)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Wishlist__UserId__46B27FE2");
        });

        modelBuilder.Entity<WithdrawRequest>(entity =>
        {
            entity.HasKey(e => e.WithdrawId).HasName("PK__Withdraw__435D94E2E5FB2A4B");

            entity.ToTable("WithdrawRequest");

            entity.Property(e => e.AccountNumber)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.BankName).HasMaxLength(100);
            entity.Property(e => e.ProcessedAt).HasColumnType("datetime");
            entity.Property(e => e.RequestedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Pending");

            entity.HasOne(d => d.Shop).WithMany(p => p.WithdrawRequests)
                .HasForeignKey(d => d.ShopId)
                .HasConstraintName("FK__WithdrawR__ShopI__1F98B2C1");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
