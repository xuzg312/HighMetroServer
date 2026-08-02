using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace HighMetro.Attributes;

public class IpAddressAttribute : ValidationAttribute
{
    // 标准的 IPv4 正则表达式 (0-255)
    private const string IpPattern = @"^((25[0-5]|2[0-4]\d|[01]?\d\d?)\.){3}(25[0-5]|2[0-4]\d|[01]?\d\d?)$";
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
        {
            // 如果允许为空，这里返回 Success；如果不允许，请配合 [Required] 使用
            return ValidationResult.Success; 
        }

        string ip = value.ToString()!;
        if (Regex.IsMatch(ip, IpPattern))
        {
            return ValidationResult.Success;
        }

        return new ValidationResult(ErrorMessage ?? "请输入有效的 IP 地址格式。");
    }
}