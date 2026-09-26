using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HighMetroServer.BaseModel;
using HighMetroServer.Models;
using HighMetroServer.Parameters;
using MySqlConnector;

namespace HighMetroServer.Services;

public class DbService : IDbService
{
    private string _connectionString = string.Empty;
    private string _currDate = string.Empty;
    private int _bh;
    private string GetConnectionString()
    {
        if (_connectionString.Equals(string.Empty))
        {
            _connectionString = DataBaseConnect.Instance.GetConnectionString();
        }
        return _connectionString;
    }
    public async Task<ResultInfo> TestConnection(DbSetting setting)
    {
        var resultInfo = new ResultInfo();
        try
        {
            await using var conn = new MySqlConnection(setting.GetConnectionString());
            await conn.OpenAsync();
            resultInfo.Code = PublicConst.FlagYes;
            resultInfo.Message = "";
            return resultInfo;
        }
        catch (Exception ex)
        {
            resultInfo.Code = PublicConst.FlagNo;
            resultInfo.Message = "连接异常，请检查配置信息：" + ex.Message;
            return resultInfo;
        }
    }
    public async Task<ResultInfo> VerifyUser(LoginSetting loginSetting, DbSetting dbSetting)
    {
        var resultInfo = new ResultInfo
        {
            Code = PublicConst.FlagYes
        };
        try
        {
            await using var conn = new MySqlConnection(dbSetting.GetConnectionString());
            await conn.OpenAsync();
            var sql = "select password from t_user where username=@username and efftflag=@efftFlag;";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@username", loginSetting.LoginUser);
            cmd.Parameters.AddWithValue("@efftFlag", PublicConst.FlagYes);
            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var loginPassword = reader["password"].ToString() ?? string.Empty;
                if (loginPassword.Equals(string.Empty) || !loginPassword.Equals(loginSetting.LoginPassword))
                {
                    resultInfo.Code = PublicConst.FlagNo;
                    resultInfo.Message = "用户名或密码无效！";
                }
            }
            else
            {
                resultInfo.Code = PublicConst.FlagNo;
                resultInfo.Message = "用户名或密码无效！";
            }
            return resultInfo;
        }
        catch (Exception ex)
        {
            resultInfo.Code = PublicConst.FlagNo;
            resultInfo.Message = "连接异常，请检查配置信息：" + ex.Message;
            return resultInfo;
        }
    }

    public async Task<ResultInfo> VerifyHost(HostSetting hostSetting, DbSetting dbSetting)
    {
        var resultInfo = new ResultInfo();
        try
        {
            await using var conn = new MySqlConnection(dbSetting.GetConnectionString());
            await conn.OpenAsync();
            var sql = "select code,name,ip,port from t_host where bh=@bh and flag=@flag;";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@bh", hostSetting.Bh);
            cmd.Parameters.AddWithValue("@flag", PublicConst.FlagYes);
            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                resultInfo.Code = PublicConst.FlagYes;
                return resultInfo;
            }
            resultInfo.Code = PublicConst.FlagNo;
            resultInfo.Message = "工控机编号无效！";
            return resultInfo;
        }
        catch (Exception ex)
        {
            resultInfo.Code = PublicConst.FlagNo;
            resultInfo.Message = "连接异常，请检查配置信息：" + ex.Message;
            return resultInfo;
        }
    }
    public async Task<ResultHostInfo> GetHostList(DbSetting dbSetting)
    {
        var resultHostInfo = new ResultHostInfo();
        try
        {
            await using var conn = new MySqlConnection(dbSetting.GetConnectionString());
            await conn.OpenAsync();
            var sql = "select bh,code,name,ip,port from t_host where flag=@flag;";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@flag", PublicConst.FlagYes);
            await using var reader = await cmd.ExecuteReaderAsync();
            List<HostInfo> hostInfoList = [];
            while (await reader.ReadAsync())
            {
                var hostInfo = new HostInfo
                {
                    Bh = Convert.ToInt32(reader["bh"]),
                    Code = reader["code"].ToString() ?? string.Empty,
                    Name = reader["name"].ToString() ?? string.Empty,
                    Ip = reader["ip"].ToString() ?? string.Empty,
                    Port = Convert.ToInt32(reader["port"])
                };
                hostInfoList.Add(hostInfo);
            }
            resultHostInfo.HostList = hostInfoList;
            var resultInfo = new ResultInfo
            {
                Code = PublicConst.FlagYes
            };
            resultHostInfo.ReturnInfo = resultInfo;
            return resultHostInfo;
        }
        catch (Exception ex)
        {
            var resultInfo = new ResultInfo
            {
                Code = PublicConst.FlagNo,
                Message = "获取工控机信息异常，错误原因:" + ex.Message
            };
            resultHostInfo.ReturnInfo = resultInfo;
            return resultHostInfo;
        }
    }

    public async Task<ResultInfo> GetHostInfo(HostInfo hostInfo)
    {
        var resultInfo = new ResultInfo();
        try
        {
            await using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync();
            var sql = "select code,name,ip,port from t_host where bh=@bh and flag=@flag;";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@bh", hostInfo.Bh);
            cmd.Parameters.AddWithValue("@flag", PublicConst.FlagYes);
            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                hostInfo.Code = reader["code"].ToString() ?? string.Empty;
                hostInfo.Name = reader["name"].ToString() ?? string.Empty;
                hostInfo.Ip = reader["ip"].ToString() ?? string.Empty;
                hostInfo.Port = Convert.ToInt32(reader["port"]);
                resultInfo.Code = PublicConst.FlagYes;
                return resultInfo;
            }
            resultInfo.Code = PublicConst.FlagNo;
            resultInfo.Message = "工控机编号无效！";
            return resultInfo;
        }
        catch (Exception ex)
        {
            resultInfo.Code = PublicConst.FlagNo;
            resultInfo.Message = "连接异常，请检查配置信息：" + ex.Message;
            return resultInfo;
        }
    }
    public async Task<ResultInfo> GetHardCamera(HardInfo hardInfo)
    {
        var resultInfo = new ResultInfo();
        try
        {
            await using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync();
            var sql =
                "select bh,ip,port,username,password,type from t_hardcamera where hostbh=@hostBh and type=@type;";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@hostBh", hardInfo.HostBh);
            cmd.Parameters.AddWithValue("@type", hardInfo.Type);
            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                hardInfo.UserName = reader["username"].ToString() ?? string.Empty;
                hardInfo.PassWord = reader["password"].ToString() ?? string.Empty;
                hardInfo.Ip = reader["ip"].ToString() ?? string.Empty;
                hardInfo.Port = Convert.ToInt32(reader["port"]);
                hardInfo.Bh = Convert.ToInt32(reader["bh"]);
                hardInfo.Type = reader["type"].ToString() ?? string.Empty;
                resultInfo.Tag = 1;
            }
            resultInfo.Code = PublicConst.FlagYes;
            return resultInfo;
        }
        catch (Exception ex)
        {
            resultInfo.Code = PublicConst.FlagNo;
            resultInfo.Message = "连接异常，请检查配置信息：" + ex.Message;
            return resultInfo;
        }
    }
    public async Task<ResultSerialCommInfo> GetCommInfoList(HostInfo hostInfo, string commType)
    {
        var resultSerialCommInfo = new ResultSerialCommInfo();
        try
        {
            await using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync();
            var sql = "SELECT * FROM t_mainbord WHERE hostbh = @hostbh and commType=@commType";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@hostbh", hostInfo.Bh);
            cmd.Parameters.AddWithValue("@commType", commType);
            await using var reader = await cmd.ExecuteReaderAsync();
            var serialList = new List<SerialCommInfo>();
            // 循环读取所有行
            while (await reader.ReadAsync())
            {
                var serialComm00 = new SerialCommInfo
                {
                    HostBh = Convert.ToInt32(reader["hostbh"]),
                    Bh = Convert.ToInt32(reader["bh"]),
                    Id = Convert.ToInt32(reader["id"]),
                    Name = reader["name"].ToString() ?? string.Empty,
                    CommName = reader["commname"].ToString() ?? string.Empty,
                    BaudRate = Convert.ToInt32(reader["baudRate"]),
                    Parity = Convert.ToInt32(reader["parity"]),
                    DataBits = Convert.ToInt32(reader["dataBits"]),
                    StopBits = Convert.ToInt32(reader["stopBits"]),
                    CommType = reader["CommType"].ToString() ?? string.Empty,
                };
                serialList.Add(serialComm00);
            }
            resultSerialCommInfo.SerialCommList = serialList;
            var resultInfo = new ResultInfo
            {
                Code = PublicConst.FlagYes,
            };
            resultSerialCommInfo.ReturnInfo = resultInfo;
            return resultSerialCommInfo;
        }
        catch (Exception ex)
        {
            var resultInfo = new ResultInfo
            {
                Code = PublicConst.FlagNo,
                Message = "连接异常，请检查配置信息：" + ex.Message
            };
            resultSerialCommInfo.ReturnInfo = resultInfo;
            return resultSerialCommInfo;
        }
    }
    public async Task<ResultInfo> AddHardCamera(HardInfo hardInfo)
    {
        var resultInfo = new ResultInfo();
        try
        {
            await using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync();
            var sql = "INSERT INTO t_hardcamera (hostbh,type,ip, port,username,password) " +
                      "VALUES (@hostbh,@type,@ip, @port,@username,@password);";
            await using var cmd = new MySqlCommand(sql, conn);
            // 添加参数（避免拼接字符串导致SQL注入）
            cmd.Parameters.AddWithValue("@hostbh", hardInfo.HostBh);
            cmd.Parameters.AddWithValue("@type", hardInfo.Type);
            cmd.Parameters.AddWithValue("@ip", hardInfo.Ip);
            cmd.Parameters.AddWithValue("@port", hardInfo.Port);
            cmd.Parameters.AddWithValue("@username", hardInfo.UserName);
            cmd.Parameters.AddWithValue("@password", hardInfo.PassWord);
            resultInfo.Tag = await cmd.ExecuteNonQueryAsync();
            if (resultInfo.Tag == 1)
            {
                resultInfo.Code = PublicConst.FlagYes;
            }
            else
            {
                resultInfo.Code = PublicConst.FlagNo;
                resultInfo.Message = "保存摄像头参数失败！";
            }
            return resultInfo;
        }
        catch (Exception ex)
        {
            resultInfo.Code = PublicConst.FlagNo;
            resultInfo.Message = "保存摄像机信息异常：" + ex.Message;
            return resultInfo;
        }
    }
    public async Task<ResultInfo> EditHardCamera(HardInfo hardInfo)
    {
        var resultInfo = new ResultInfo();
        try
        {
            await using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync();
            var sql =
                "update t_hardcamera set hostbh=@hostbh,ip=@ip,port=@port,username=@username,password=@password where bh=@bh";
            await using var cmd = new MySqlCommand(sql, conn);
            // 添加参数（避免拼接字符串导致SQL注入）
            cmd.Parameters.AddWithValue("@hostbh", hardInfo.HostBh);
            cmd.Parameters.AddWithValue("@ip", hardInfo.Ip);
            cmd.Parameters.AddWithValue("@port", hardInfo.Port);
            cmd.Parameters.AddWithValue("@username", hardInfo.UserName);
            cmd.Parameters.AddWithValue("@password", hardInfo.PassWord);
            cmd.Parameters.AddWithValue("@bh", hardInfo.Bh);
            resultInfo.Tag = await cmd.ExecuteNonQueryAsync();
            if (resultInfo.Tag == 1)
            {
                resultInfo.Code = PublicConst.FlagYes;
            }
            else
            {
                resultInfo.Code = PublicConst.FlagNo;
                resultInfo.Message = "更新摄像头参数失败！";
            }
            return resultInfo;
        }
        catch (Exception ex)
        {
            resultInfo.Code = PublicConst.FlagNo;
            resultInfo.Message = "保存摄像机信息异常：" + ex.Message;
            return resultInfo;
        }
    }
    public async Task<ResultInfo> AddHeart(MainInfoBean mainInfoBean)
    {
        var resultInfo = new ResultInfo();
        try
        {
            await using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync();
            var sql = "INSERT INTO t_heart (hostbh,id, length,txzs,kmcs,yxms,agzms,astate,a1gzm,a2gzm,a1zs,a2zs," +
                      "a1dl,a2dl,a1wz,a2wz,bstate,b1gzm,b2gzm,b1zs,b2zs,b1dl,b2dl,b1wz,b2wz,dlcgqzt,dostate,kzdldo,total,datetime) " +
                      "VALUES (@hostbh,@id, @length,@txzs,@kmcs,@yxms,@agzms,@astate,@a1gzm,@a2gzm,@a1zs,@a2zs," +
                      "@a1dl,@a2dl,@a1wz,@a2wz,@bstate,@b1gzm,@b2gzm,@b1zs,@b2zs,@b1dl,@b2dl,@b1wz,@b2wz,@dlcgqzt,@dostate,@kzdldo,@total,@datetime);";
            await using var cmd = new MySqlCommand(sql, conn);
            // 添加参数（避免拼接字符串导致SQL注入）
            cmd.Parameters.AddWithValue("@hostbh", mainInfoBean.HostBh);
            cmd.Parameters.AddWithValue("@id", mainInfoBean.Id);
            cmd.Parameters.AddWithValue("@length", mainInfoBean.Length);
            cmd.Parameters.AddWithValue("@txzs", mainInfoBean.Txzs);
            cmd.Parameters.AddWithValue("@kmcs", mainInfoBean.Kmcs);
            cmd.Parameters.AddWithValue("@yxms", mainInfoBean.Yxms);
            cmd.Parameters.AddWithValue("@agzms", mainInfoBean.Agzms);
            cmd.Parameters.AddWithValue("@astate", mainInfoBean.Astate);
            cmd.Parameters.AddWithValue("@a1gzm", mainInfoBean.A1gzm);
            cmd.Parameters.AddWithValue("@a2gzm", mainInfoBean.A2gzm);
            cmd.Parameters.AddWithValue("@a1zs", mainInfoBean.A1zs);
            cmd.Parameters.AddWithValue("@a2zs", mainInfoBean.A2zs);
            cmd.Parameters.AddWithValue("@a1dl", mainInfoBean.A1dl);
            cmd.Parameters.AddWithValue("@a2dl", mainInfoBean.A2dl);
            cmd.Parameters.AddWithValue("@a1wz", mainInfoBean.A1wz);
            cmd.Parameters.AddWithValue("@a2wz", mainInfoBean.A2wz);
            cmd.Parameters.AddWithValue("@bstate", mainInfoBean.Bstate);
            cmd.Parameters.AddWithValue("@b1gzm", mainInfoBean.B1gzm);
            cmd.Parameters.AddWithValue("@b2gzm", mainInfoBean.B2gzm);
            cmd.Parameters.AddWithValue("@b1zs", mainInfoBean.B1zs);
            cmd.Parameters.AddWithValue("@b2zs", mainInfoBean.B2zs);
            cmd.Parameters.AddWithValue("@b1dl", mainInfoBean.B1dl);
            cmd.Parameters.AddWithValue("@b2dl", mainInfoBean.B2dl);
            cmd.Parameters.AddWithValue("@b1wz", mainInfoBean.B1wz);
            cmd.Parameters.AddWithValue("@b2wz", mainInfoBean.B2Wz);
            cmd.Parameters.AddWithValue("@dlcgqzt", mainInfoBean.Dlcgqzt);
            cmd.Parameters.AddWithValue("@dostate", mainInfoBean.Dostate);
            cmd.Parameters.AddWithValue("@kzdldo", mainInfoBean.Kzdldo);
            cmd.Parameters.AddWithValue("@total", mainInfoBean.Total);
            cmd.Parameters.AddWithValue("@datetime", mainInfoBean.Datetime);
            resultInfo.Tag = await cmd.ExecuteNonQueryAsync();
            if (resultInfo.Tag == 1)
            {
                resultInfo.Code = PublicConst.FlagYes;
            }
            else
            {
                resultInfo.Code = PublicConst.FlagNo;
                resultInfo.Message = "保存心跳数据失败！";
            }
            return resultInfo;
        }
        catch (Exception ex)
        {
            resultInfo.Code = PublicConst.FlagNo;
            resultInfo.Message = "保存心跳信息异常：" + ex.Message;
            return resultInfo;
        }
    }

    public async Task<ResultInfo> SavePersonDay(MainInfoBean mainInfoBean)
    {
        if (_currDate.Equals(""))
        {
            //刚启动程序，校验数据库是否存在？
            var mainInfoBean00 = new MainInfoBean
            {
                HostBh = mainInfoBean.HostBh,
                Id = mainInfoBean.Id,
                Datetime = mainInfoBean.Datetime
            };
            var returnValue = await GetPersonDay(mainInfoBean00);
            if (!returnValue.Code.Equals(PublicConst.FlagYes))
            {
                return returnValue;
            }
            if (returnValue.Tag == 1)
            {
                _currDate = mainInfoBean.Datetime;
                _bh = mainInfoBean00.Bh;
            }
        }
        if (mainInfoBean.Datetime.Equals(_currDate))
        {
            return await UpdatePersonDay(mainInfoBean);
        }
        return await AddPersonDay(mainInfoBean);
    }

    private async Task<ResultInfo> GetPersonDay(MainInfoBean mainInfoBean)
    {
        var resultInfo = new ResultInfo();
        try
        {
            await using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync();
            var sql = "select bh,personcount from t_personday where hostbh=@hostbh and id=@id and date=@date;";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@hostbh", mainInfoBean.HostBh);
            cmd.Parameters.AddWithValue("@id", mainInfoBean.Id);
            cmd.Parameters.AddWithValue("@date", mainInfoBean.Datetime);
            await using var reader = await cmd.ExecuteReaderAsync();
            // 循环读取所有行
            if (await reader.ReadAsync())
            {
                mainInfoBean.Bh = Convert.ToInt32(reader["bh"]);
                mainInfoBean.Kmcs = Convert.ToInt32(reader["personcount"]);
                resultInfo.Tag = 1;
            }
            else
            {
                resultInfo.Tag = 0;
            }
            resultInfo.Code = PublicConst.FlagYes;
            return resultInfo;
        }
        catch (Exception ex)
        {
            resultInfo.Code = PublicConst.FlagNo;
            resultInfo.Message = "获取人数信息异常：" + ex.Message;
            return resultInfo;
        }
    }
    private async Task<ResultInfo> AddPersonDay(MainInfoBean mainInfoBean)
    {
        var resultInfo = new ResultInfo();
        try
        {
            await using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync();
            var sql =
                "INSERT INTO t_personday (hostbh,id,personcount,date) values(@hostbh,@id,@personcount,@date);SELECT LAST_INSERT_ID();";
            await using var cmd = new MySqlCommand(sql, conn);
            // 添加参数（避免拼接字符串导致SQL注入）
            cmd.Parameters.AddWithValue("@hostbh", mainInfoBean.HostBh);
            cmd.Parameters.AddWithValue("@id", mainInfoBean.Id);
            cmd.Parameters.AddWithValue("@personcount", mainInfoBean.Kmcs);
            cmd.Parameters.AddWithValue("@date", mainInfoBean.Datetime);
            // 执行并返回自增ID
            var scalarResult = await cmd.ExecuteScalarAsync();
            if (scalarResult != null && scalarResult != DBNull.Value)
            {
                _bh = Convert.ToInt32(scalarResult);
                _currDate = mainInfoBean.Datetime;
            }
            resultInfo.Code = PublicConst.FlagYes;
            return resultInfo;
        }
        catch (Exception ex)
        {
            resultInfo.Code = PublicConst.FlagNo;
            resultInfo.Message = "保存心跳信息异常：" + ex.Message;
            return resultInfo;
        }
    }
    private async Task<ResultInfo> UpdatePersonDay(MainInfoBean mainInfoBean)
    {
        var resultInfo = new ResultInfo();
        try
        {
            await using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync();
            var sql = "update t_personday set personcount=@personcount where bh=@bh;";
            await using var cmd = new MySqlCommand(sql, conn);
            // 添加参数（避免拼接字符串导致SQL注入）
            cmd.Parameters.AddWithValue("@personcount", mainInfoBean.Kmcs);
            cmd.Parameters.AddWithValue("@bh", _bh);
            // 执行并返回行数
            resultInfo.Tag = await cmd.ExecuteNonQueryAsync();
            if (resultInfo.Tag == 1)
            {
                resultInfo.Code = PublicConst.FlagYes;
            }
            else
            {
                resultInfo.Code = PublicConst.FlagNo;
                resultInfo.Message = "更新每日通过人数失败！";
            }
            return resultInfo;
        }
        catch (Exception ex)
        {
            resultInfo.Code = PublicConst.FlagNo;
            resultInfo.Message = "保存每天通过人数信息异常：" + ex.Message;
            return resultInfo;
        }
    }
    public async Task<ResultInfo> AddError(CameraBean cameraBean)
    {
        var resultInfo = new ResultInfo();
        try
        {
            await using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync();
            var sql = "INSERT INTO t_error (hostbh,door,type,datetime,id,message,serial) " +
                      "VALUES (@hostbh, @door,@type,@datetime,@id,@message,@serial);";
            await using var cmd = new MySqlCommand(sql, conn);
            // 添加参数（避免拼接字符串导致SQL注入）
            cmd.Parameters.AddWithValue("@hostbh", cameraBean.HostBh);
            cmd.Parameters.AddWithValue("@door", cameraBean.Door);
            cmd.Parameters.AddWithValue("@type", cameraBean.Type);
            cmd.Parameters.AddWithValue("@datetime", cameraBean.DateTime);
            cmd.Parameters.AddWithValue("@id", cameraBean.Id);
            cmd.Parameters.AddWithValue("@message", cameraBean.Message);
            cmd.Parameters.AddWithValue("@serial", cameraBean.Serial);
            resultInfo.Tag = await cmd.ExecuteNonQueryAsync();
            if (resultInfo.Tag == 1)
            {
                resultInfo.Code = PublicConst.FlagYes;
            }
            else
            {
                resultInfo.Code = PublicConst.FlagNo;
                resultInfo.Message = "更新日志信息失败！";
            }
            return resultInfo;
        }
        catch (Exception ex)
        {
            resultInfo.Code = PublicConst.FlagNo;
            resultInfo.Message = "保存日志信息异常：" + ex.Message;
            return resultInfo;
        }
    }
    public async Task<ResultInfo> AddAlarm(CameraBean cameraBean)
    {
        var resultInfo = new ResultInfo();
        try
        {
            await using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync();
            var sql = "INSERT INTO t_alarm (id,door,type,datetime,upload,filepath,serial,hostbh) " +
                      "VALUES (@id,@door,@type,@datetime,@upload,@filepath,@serial,@hostbh);";
            await using var cmd = new MySqlCommand(sql, conn);
            // 添加参数（避免拼接字符串导致SQL注入）
            cmd.Parameters.AddWithValue("@id", cameraBean.Id);
            cmd.Parameters.AddWithValue("@door", cameraBean.Door);
            cmd.Parameters.AddWithValue("@type", cameraBean.Type);
            cmd.Parameters.AddWithValue("@datetime", cameraBean.DateTime);
            cmd.Parameters.AddWithValue("@upload", "N");
            cmd.Parameters.AddWithValue("@filepath", cameraBean.FilePath);
            cmd.Parameters.AddWithValue("@serial", cameraBean.Serial);
            cmd.Parameters.AddWithValue("@hostbh", cameraBean.HostBh);
            resultInfo.Tag = await cmd.ExecuteNonQueryAsync();
            if (resultInfo.Tag == 1)
            {
                resultInfo.Code = PublicConst.FlagYes;
            }
            else
            {
                resultInfo.Code = PublicConst.FlagNo;
                resultInfo.Message = "更新拍照信息失败！";
            }
            return resultInfo;
        }
        catch (Exception ex)
        {
            resultInfo.Code = PublicConst.FlagNo;
            resultInfo.Message = "保存拍照信息异常：" + ex.Message;
            return resultInfo;
        }
    }
    public async Task<ResultInfo> AddCommInfo(SerialComm serialComm)
    {
        var resultInfo = new ResultInfo();
        try
        {
            await using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync();
            var sql =
                "INSERT INTO t_mainbord (hostbh,id, name,commname,baudRate,parity,dataBits,stopBits,commType) " +
                "VALUES (@hostbh,@id, @name,@commname,@baudRate,@parity,@dataBits,@stopBits,@commType);";
            await using var cmd = new MySqlCommand(sql, conn);
            // 添加参数（避免拼接字符串导致SQL注入）
            cmd.Parameters.AddWithValue("@hostbh", serialComm.HostBh);
            cmd.Parameters.AddWithValue("@id", serialComm.Id);
            cmd.Parameters.AddWithValue("@name", serialComm.Name);
            cmd.Parameters.AddWithValue("@commname", serialComm.CommName);
            cmd.Parameters.AddWithValue("@baudRate", serialComm.BaudRate);
            cmd.Parameters.AddWithValue("@parity", serialComm.Parity);
            cmd.Parameters.AddWithValue("@dataBits", serialComm.DataBits);
            cmd.Parameters.AddWithValue("@stopBits", serialComm.StopBits);
            cmd.Parameters.AddWithValue("@commType", serialComm.CommType);
            resultInfo.Tag = await cmd.ExecuteNonQueryAsync();
            if (resultInfo.Tag == 1)
            {
                resultInfo.Code = PublicConst.FlagYes;
            }
            else
            {
                resultInfo.Code = PublicConst.FlagNo;
                resultInfo.Message = "保存主板信息失败！";
            }
            return resultInfo;
        }
        catch (Exception ex)
        {
            resultInfo.Code = PublicConst.FlagNo;
            resultInfo.Message = "保存主板信息异常：" + ex.Message;
            return resultInfo;
        }
    }
    public async Task<ResultInfo> EditCommInfo(SerialComm serialComm)
    {
        var resultInfo = new ResultInfo();
        try
        {
            await using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync();
            var sql = 
                "update t_mainbord set hostbh=@hostbh,id=@id,name=@name,commname=@commname,baudRate=@baudRate,"
                + "parity=@parity,dataBits=@dataBits,stopBits=@stopBits,commType=@commType where bh=@bh;";
            await using var cmd = new MySqlCommand(sql, conn);
            // 添加参数（避免拼接字符串导致SQL注入）
            cmd.Parameters.AddWithValue("@hostbh", serialComm.HostBh);
            cmd.Parameters.AddWithValue("@id", serialComm.Id);
            cmd.Parameters.AddWithValue("@name", serialComm.Name);
            cmd.Parameters.AddWithValue("@commname", serialComm.CommName);
            cmd.Parameters.AddWithValue("@baudRate", serialComm.BaudRate);
            cmd.Parameters.AddWithValue("@parity", serialComm.Parity);
            cmd.Parameters.AddWithValue("@dataBits", serialComm.DataBits);
            cmd.Parameters.AddWithValue("@stopBits", serialComm.StopBits);
            cmd.Parameters.AddWithValue("@commType", serialComm.CommType);
            cmd.Parameters.AddWithValue("@bh", serialComm.Bh);
            resultInfo.Tag = await cmd.ExecuteNonQueryAsync();
            if (resultInfo.Tag == 1)
            {
                resultInfo.Code = PublicConst.FlagYes;
            }
            else
            {
                resultInfo.Code = PublicConst.FlagNo;
                resultInfo.Message = "更新主板信息失败！";
            }
            return resultInfo;
        }
        catch (Exception ex)
        {
            resultInfo.Code = PublicConst.FlagNo;
            resultInfo.Message = "更新主板信息异常：" + ex.Message;
            return resultInfo;        
        }
    }
    public async Task<ResultCamAlarmInfo> QueryCamAlarm(CameraBean cameraBean,DataBaseQueryPage page)
    {
        var resultInfo = new ResultCamAlarmInfo();
        try
        {
            await using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync();
            var sql = "SELECT * FROM t_alarm WHERE hostbh=@hostbh and datetime >= @datetime order by datetime desc LIMIT @offset , @pageSize;";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@hostbh", cameraBean.HostBh);
            cmd.Parameters.AddWithValue("@datetime", cameraBean.DateTime);
            cmd.Parameters.AddWithValue("@offset", (page.CurrentPage-1) * page.PageSize);
            cmd.Parameters.AddWithValue("@pageSize", page.PageSize);
            await using var reader = await cmd.ExecuteReaderAsync();
            var cameraList = new List<CameraBean>();
            // 循环读取所有行
            while (await reader.ReadAsync())
            {
                var ordDateTime = reader.GetOrdinal("datetime");
                var ordUploadTime = reader.GetOrdinal("uploadtime");
                var cameraBean00 = new CameraBean
                {
                    Bh = Convert.ToInt32(reader["bh"]),
                    Id = Convert.ToInt32(reader["id"]),
                    HostBh = Convert.ToInt32(reader["hostbh"]),
                    Type = reader["type"].ToString()??string.Empty,
                    Door = reader["door"].ToString()??string.Empty,
                    Upload = reader["upload"].ToString()??string.Empty,
                    DateTime = reader.GetDateTime(ordDateTime),
                    UploadDateTime = reader.IsDBNull(ordUploadTime) ? null : reader.GetDateTime(ordUploadTime),
                    FilePath = reader["filepath"].ToString()??string.Empty,
                    Serial = Convert.ToInt32(reader["serial"]),
                };
                cameraList.Add(cameraBean00);
            }
            resultInfo.CameraList = cameraList;
            resultInfo.ReturnInfo = new ResultInfo
            {
                Code = PublicConst.FlagYes,
            };
            return resultInfo;
        }
        catch (Exception ex)
        {
            resultInfo.ReturnInfo = new ResultInfo
            {
                Code = PublicConst.FlagNo,
                Message = $"获取拍照记录异常：{ex.Message}",
            };
            return resultInfo;
        }
    } 
    public async Task<ResultInfo> QueryCamAlarmCount(CameraBean cameraBean)
    {
        var resultInfo = new ResultInfo();
        try
        {
            await using var conn = new MySqlConnection(GetConnectionString());
            await conn.OpenAsync();
            var sql = "SELECT count(*) total FROM t_alarm WHERE hostbh=@hostbh and datetime >= @datetime";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@hostbh", cameraBean.HostBh);
            cmd.Parameters.AddWithValue("@datetime", cameraBean.DateTime);
            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                resultInfo.Tag = Convert.ToInt32(reader["total"]);
            }
            resultInfo.Code = PublicConst.FlagYes;
            return resultInfo;
        }
        catch (Exception ex)
        {
            resultInfo.Code = PublicConst.FlagNo;
            resultInfo.Message = $"获取拍照记录异常：{ex.Message}";
            return resultInfo;
        }
    }  
    public async Task<ResultInfo> AddHost(HostInfo hostInfo,DbSetting dbSetting)
    {
        var resultInfo = new ResultInfo();
        try
        {
            await using var conn = new MySqlConnection(dbSetting.GetConnectionString());
            await conn.OpenAsync();
            var sql = "INSERT INTO t_host (code, name,ip,port,flag) " +
                         "VALUES (@code, @name,@ip,@port,@flag); SELECT LAST_INSERT_ID();";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@code", hostInfo.Code);
            cmd.Parameters.AddWithValue("@name", hostInfo.Name);
            cmd.Parameters.AddWithValue("@ip", hostInfo.Ip);
            cmd.Parameters.AddWithValue("@port", hostInfo.Port);
            cmd.Parameters.AddWithValue("@flag", PublicConst.FlagYes);
            var scalarResult = await cmd.ExecuteScalarAsync();
            if (scalarResult != null && scalarResult != DBNull.Value)
            {
                hostInfo.Bh = Convert.ToInt32(scalarResult);
                resultInfo.Tag = 1;
            }
            if (resultInfo.Tag == 1)
            {
                resultInfo.Code = PublicConst.FlagYes;
            }
            else
            {
                resultInfo.Code = PublicConst.FlagNo;
                resultInfo.Message = "插入工控机信息失败！";
            }
            return resultInfo;
        }
        catch (Exception ex)
        {
            resultInfo.Code = PublicConst.FlagNo;
            resultInfo.Message = ex.Message;
            return resultInfo;
        }
    }
    public async Task<ResultInfo> EditHost(HostInfo hostInfo,DbSetting dbSetting)
    {
        var resultInfo = new ResultInfo();
        try
        {
            await using var conn = new MySqlConnection(dbSetting.GetConnectionString());
            await conn.OpenAsync();
            var sql = "update t_host set code=@code,name=@name,ip=@ip,port=@port where bh=@bh;";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@code", hostInfo.Code);
            cmd.Parameters.AddWithValue("@name", hostInfo.Name);
            cmd.Parameters.AddWithValue("@ip", hostInfo.Ip);
            cmd.Parameters.AddWithValue("@port", hostInfo.Port);
            cmd.Parameters.AddWithValue("@bh", hostInfo.Bh);
            resultInfo.Tag = await cmd.ExecuteNonQueryAsync();
            if (resultInfo.Tag == 1)
            {
                resultInfo.Code = PublicConst.FlagYes;
            }
            else
            {
                resultInfo.Code = PublicConst.FlagNo;
                resultInfo.Message = "更新工控机信息失败！";
            }
            return resultInfo;
        }
        catch (Exception ex)
        {
            resultInfo.Code = PublicConst.FlagNo;
            resultInfo.Message = $"更新工控机信息异常：{ex.Message}";
            return resultInfo;
        }
    }
}