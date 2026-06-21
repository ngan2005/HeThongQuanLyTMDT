using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using TMDT.Models;

namespace TMDT.Services;

public interface IAddressService
{
    Task<List<VnProvince>> GetProvincesAsync();
    Task<List<VnDistrict>> GetDistrictsAsync(int provinceCode);
    Task<List<VnWard>> GetWardsAsync(int districtCode);
}

public class AddressService : IAddressService
{
    private readonly HttpClient _http;
    private List<VnProvince>? _cachedProvinces;

    public AddressService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<VnProvince>> GetProvincesAsync()
    {
        if (_cachedProvinces != null)
            return _cachedProvinces;

        var response = await _http.GetAsync("https://provinces.open-api.vn/api/");
        response.EnsureSuccessStatusCode();

        var data = await response.Content.ReadFromJsonAsync<List<VnProvince>>();
        _cachedProvinces = data ?? new List<VnProvince>();
        return _cachedProvinces;
    }

    public async Task<List<VnDistrict>> GetDistrictsAsync(int provinceCode)
    {
        var response = await _http.GetAsync($"https://provinces.open-api.vn/api/p/{provinceCode}?depth=2");
        response.EnsureSuccessStatusCode();

        var province = await response.Content.ReadFromJsonAsync<VnProvince>();
        return province?.Districts ?? new List<VnDistrict>();
    }

    public async Task<List<VnWard>> GetWardsAsync(int districtCode)
    {
        var response = await _http.GetAsync($"https://provinces.open-api.vn/api/d/{districtCode}?depth=2");
        response.EnsureSuccessStatusCode();

        var district = await response.Content.ReadFromJsonAsync<VnDistrict>();
        return district?.Wards ?? new List<VnWard>();
    }
}
