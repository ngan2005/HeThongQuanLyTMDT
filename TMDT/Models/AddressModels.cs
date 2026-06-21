using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TMDT.Models;

public class VnProvince
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("districts")]
    public List<VnDistrict> Districts { get; set; } = new();
}

public class VnDistrict
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("wards")]
    public List<VnWard> Wards { get; set; } = new();
}

public class VnWard
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
}


public class FullAddress
{
    public string? Street { get; set; }
    public string? Ward { get; set; }
    public string? District { get; set; }
    public string? Province { get; set; }

    public override string ToString()
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(Street)) parts.Add(Street);
        if (!string.IsNullOrWhiteSpace(Ward)) parts.Add(Ward);
        if (!string.IsNullOrWhiteSpace(District)) parts.Add(District);
        if (!string.IsNullOrWhiteSpace(Province)) parts.Add(Province);
        return string.Join(", ", parts);
    }
}
