using System.Threading.Tasks;
using TMDT.DTOs;

namespace TMDT.Services.Interfaces
{
    public interface IShopService
    {
        Task<ShopDto> RegisterShopAsync(ShopRegisterRequest request);
        Task<bool> HasShopForUserAsync(int userId);
    }

    public class ShopDto
    {
        public int ShopId { get; set; }
        public string ShopName { get; set; }
        public bool? IsActive { get; set; }
    }

    public class ShopRegisterRequest
    {
        public int UserId { get; set; }
        public string ShopName { get; set; }
        public string WarehouseAddress { get; set; }
    }
}
