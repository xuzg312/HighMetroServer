using System;
using System.IO;
using System.Text.Json;
using HighMetro.Parameters;

namespace HighMetro.Services;

public class ConfigService : IConfigService
{
    private readonly string _configPath;

    public ConfigService()
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "Config");
        Directory.CreateDirectory(folder);
        _configPath = Path.Combine(folder, "config.json");
    }

    public DbSetting LoadDbConfig()
    {
        var appConfig = LoadAppConfig();
        return appConfig.Database ?? new DbSetting();
    }
    public LoginSetting LoadLoginConfig()
    {
        var appConfig = LoadAppConfig();
        return appConfig.LoginInfo ?? new LoginSetting();
    }
    public HostSetting LoadHostConfig()
    {
        var appConfig = LoadAppConfig();
        return appConfig.HostInfo ?? new HostSetting();
    }
    public void SaveDbConfig(DbSetting setting)
    {
        try
        {
            // 1. 读取现有的全局配置（如果文件不存在则返回空配置）
            var appConfig = LoadAppConfig();
        
            // 2. 将新的数据库设置赋值给 Database 节点
            appConfig.Database = setting;

            // 3. 执行安全的原子写入
            SaveAppConfig(appConfig);
        }
        catch (IOException ex)
        {
            // 捕获IO异常，上层ViewModel弹窗提示用户保存失败
            throw new InvalidOperationException("保存配置文件失败，请检查权限或磁盘空间", ex);
        }
    }
    public void SaveLoginConfig(LoginSetting setting)
    {
        try
        {
            // 1. 读取现有的全局配置（如果文件不存在则返回空配置）
            var appConfig = LoadAppConfig();
        
            // 2. 将新的数据库设置赋值给 Database 节点
            appConfig.LoginInfo = setting;

            // 3. 执行安全的原子写入
            SaveAppConfig(appConfig);
        }
        catch (IOException ex)
        {
            // 捕获IO异常，上层ViewModel弹窗提示用户保存失败
            throw new InvalidOperationException("保存配置文件失败，请检查权限或磁盘空间", ex);
        }
    }
    public void SaveHostConfig(HostSetting setting)
    {
        try
        {
            // 1. 读取现有的全局配置（如果文件不存在则返回空配置）
            var appConfig = LoadAppConfig();
        
            // 2. 将新的数据库设置赋值给 Database 节点
            appConfig.HostInfo = setting;

            // 3. 执行安全的原子写入
            SaveAppConfig(appConfig);
        }
        catch (IOException ex)
        {
            // 捕获IO异常，上层ViewModel弹窗提示用户保存失败
            throw new InvalidOperationException("保存配置文件失败，请检查权限或磁盘空间", ex);
        }
    }
    private AppConfig LoadAppConfig()
    {
        if (File.Exists(_configPath))
        {
            try
            {
                var json = File.ReadAllText(_configPath);
                return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
            }
            catch
            {
                // 如果 JSON 格式损坏，返回默认配置
                return new AppConfig();
            }
        }
        return new AppConfig();
    }
    private void SaveAppConfig(AppConfig appConfig)
    {
        // 确保父目录存在
        string? directory = Path.GetDirectoryName(_configPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string json = JsonSerializer.Serialize(appConfig, new JsonSerializerOptions { WriteIndented = true });
        
        // 原子写入策略
        string tempFile = _configPath + ".tmp";
        File.WriteAllText(tempFile, json);

        if (File.Exists(_configPath))
        {
            File.Replace(tempFile, _configPath, null);
        }
        else
        {
            File.Move(tempFile, _configPath);
        }
    }
}