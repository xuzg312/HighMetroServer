using System;
using System.IO;
using System.Text.Json;
using HighMetroServer.Aot;
using HighMetroServer.ClassLib;
using HighMetroServer.Parameters;

namespace HighMetroServer.Services;

public class ConfigService : IConfigService
{
    private readonly string _configPath = Path.Combine(SystemInfo.SysConfigDir, "config.json");

    public DbSetting LoadDbConfig()
    {
        var appConfig = LoadAppConfig();
        if (appConfig.Database != null)
        {
            appConfig.Database.DbPassword=CryptoHelper.Decrypt(appConfig.Database.DbPassword);
            return appConfig.Database;
        }
        return new DbSetting();
    }
    public LoginSetting LoadLoginConfig()
    {
        var appConfig = LoadAppConfig();
        if (appConfig.LoginInfo != null)
        {
            appConfig.LoginInfo.LoginPassword=CryptoHelper.Decrypt(appConfig.LoginInfo.LoginPassword);
            return appConfig.LoginInfo;
        }
        return new LoginSetting();
    }
    public HostSetting LoadHostConfig()
    {
        var appConfig = LoadAppConfig();
        return appConfig.HostInfo ?? new HostSetting();
    }
    public void SaveDbConfig(DbSetting setting)
    {
        var passwd = setting.DbPassword;
        try
        {
            var appConfig = LoadAppConfig();
            setting.DbPassword = CryptoHelper.Encrypt(passwd);
            appConfig.Database = setting;
            SaveAppConfig(appConfig);
        }
        catch (IOException ex)
        {
            throw new InvalidOperationException("保存配置文件失败，请检查权限或磁盘空间", ex);
        }
        finally
        {
            setting.DbPassword = passwd;
        }
    }
    public void SaveLoginConfig(LoginSetting setting)
    {
        var passwd = setting.LoginPassword;
        try
        {
            var appConfig = LoadAppConfig();
            setting.LoginPassword = CryptoHelper.Encrypt(passwd);
            appConfig.LoginInfo = setting;
            SaveAppConfig(appConfig);
        }
        catch (IOException ex)
        {
            throw new InvalidOperationException("保存配置文件失败，请检查权限或磁盘空间", ex);
        }
        finally
        {
            setting.LoginPassword = passwd;
        }
    }
    public void SaveHostConfig(HostSetting setting)
    {
        try
        {
            var appConfig = LoadAppConfig();
            appConfig.HostInfo = setting;
            SaveAppConfig(appConfig);
        }
        catch (IOException ex)
        {
            throw new InvalidOperationException("保存配置文件失败，请检查权限或磁盘空间", ex);
        }
    }
    private AppConfig LoadAppConfig()
    {
        if (!File.Exists(_configPath))
        {
            return new AppConfig();
        }
        try
        {
            var json = File.ReadAllText(_configPath);
            return JsonSerializer.Deserialize(json, AppConfigJsonContext.Default.AppConfig) ?? new AppConfig();
        }
        catch
        {
            return new AppConfig();
        }
    }
    private void SaveAppConfig(AppConfig appConfig)
    {
        var json = JsonSerializer.Serialize(appConfig, AppConfigJsonContext.Default.AppConfig);
        var tempFile = _configPath + ".tmp";
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