using System;
using System.Collections.Generic;

namespace TMDT.Models;

/// <summary>Cấu hình hệ thống lưu tập trung trong DB — thay thế file systemsettings.json.</summary>
public partial class SystemConfig
{
    public int ConfigId { get; set; }

    /// <summary>Tên khóa cấu hình, ví dụ: "PlatformCommissionRate"</summary>
    public string ConfigKey { get; set; } = null!;

    /// <summary>Giá trị dạng chuỗi, deserialize theo từng key</summary>
    public string? ConfigValue { get; set; }

    /// <summary>Mô tả ý nghĩa của key này</summary>
    public string? Description { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
