using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TMDT.Utilities
{
    public class LocationItem
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public class Province : LocationItem
    {
        [JsonProperty("districts")]
        public List<District> Districts { get; set; }
    }

    public class District : LocationItem
    {
        [JsonProperty("wards")]
        public List<Ward> Wards { get; set; }
    }

    public class Ward : LocationItem
    {
    }

    public class LocationService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        
        public static async Task<List<Province>> GetProvincesAsync()
        {
            try
            {
                var response = await _httpClient.GetStringAsync("https://provinces.open-api.vn/api/p/");
                return JsonConvert.DeserializeObject<List<Province>>(response) ?? new List<Province>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error fetching provinces: " + ex.Message);
                return new List<Province>();
            }
        }

        public static async Task<List<District>> GetDistrictsAsync(int provinceCode)
        {
            try
            {
                var response = await _httpClient.GetStringAsync($"https://provinces.open-api.vn/api/p/{provinceCode}?depth=2");
                var province = JsonConvert.DeserializeObject<Province>(response);
                return province?.Districts ?? new List<District>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error fetching districts: " + ex.Message);
                return new List<District>();
            }
        }

        public static async Task<List<Ward>> GetWardsAsync(int districtCode)
        {
            try
            {
                var response = await _httpClient.GetStringAsync($"https://provinces.open-api.vn/api/d/{districtCode}?depth=2");
                var district = JsonConvert.DeserializeObject<District>(response);
                return district?.Wards ?? new List<Ward>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error fetching wards: " + ex.Message);
                return new List<Ward>();
            }
        }
    }
}
