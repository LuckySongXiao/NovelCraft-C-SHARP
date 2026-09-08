using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NovelManagement.WPF.Services;

/// <summary>
/// 启动密码验证：密码不以明文存储，仅保存其 SHA-256 哈希进行比对。
/// 用户可通过「发布管理 → 修改启动密码」自定义密码，自定义哈希持久化于
/// 用户配置 appsettings.user.json 的 Security:LaunchPasswordHash 节点（不入库不分发）；
/// 未自定义时使用出厂默认哈希。
/// </summary>
public static class PasswordGate
{
    /// <summary>
    /// 出厂默认启动密码的 SHA-256 哈希值（UTF-8 编码，十六进制小写）
    /// </summary>
    public const string DefaultPasswordHash = "4ad1a27abb190c79ffce86a86d7d740913e8c624faad1d02b99ee01506bfb582";

    private const string ApplicationDirectoryName = "NovelManagement";

    /// <summary>
    /// 计算密码的 SHA-256 哈希（十六进制小写）
    /// </summary>
    public static string ComputeHash(string password)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// 用户配置文件路径（与 App.xaml.cs 的数据目录约定一致）
    /// </summary>
    public static string GetUserConfigurationPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            ApplicationDirectoryName, "config", "appsettings.user.json");
    }

    /// <summary>
    /// 读取用户自定义密码哈希；未自定义或配置异常时返回 null（回退默认密码）
    /// </summary>
    public static string? GetStoredPasswordHash()
    {
        try
        {
            var path = GetUserConfigurationPath();
            if (!File.Exists(path))
            {
                return null;
            }

            var root = JObject.Parse(File.ReadAllText(path));
            return root["Security"]?["LaunchPasswordHash"]?.Value<string>();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 当前生效的密码哈希（用户自定义优先，否则出厂默认）
    /// </summary>
    public static string GetEffectivePasswordHash()
    {
        var stored = GetStoredPasswordHash();
        return string.IsNullOrWhiteSpace(stored) ? DefaultPasswordHash : stored;
    }

    /// <summary>
    /// 校验输入密码是否与当前生效哈希匹配。
    /// 说明：密码门在配置管线构建之前执行，因此直接读取用户配置文件而非 IConfiguration。
    /// </summary>
    public static bool Verify(string? password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return false;
        }

        return string.Equals(ComputeHash(password), GetEffectivePasswordHash(), StringComparison.Ordinal);
    }

    /// <summary>
    /// 修改启动密码：验证旧密码通过后写入新哈希
    /// </summary>
    /// <param name="currentPassword">当前启动密码</param>
    /// <param name="newPassword">新启动密码</param>
    /// <returns>旧密码验证失败返回 false；成功返回 true</returns>
    public static bool ChangePassword(string currentPassword, string newPassword)
    {
        if (!Verify(currentPassword))
        {
            return false;
        }

        SetPasswordHash(ComputeHash(newPassword));
        return true;
    }

    /// <summary>
    /// 将新密码哈希写入用户配置（Security:LaunchPasswordHash），下次启动生效
    /// </summary>
    public static void SetPasswordHash(string newHash)
    {
        var path = GetUserConfigurationPath();
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var root = File.Exists(path)
            ? JObject.Parse(File.ReadAllText(path))
            : new JObject();

        var security = root["Security"] as JObject ?? new JObject();
        root["Security"] = security;
        security["LaunchPasswordHash"] = newHash;

        File.WriteAllText(path, root.ToString(Formatting.Indented));
    }
}
