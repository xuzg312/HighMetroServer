using System;
using System.Collections.Generic;
using HighMetro.BaseModel;
using HighMetro.Models;
using HighMetro.Parameters;
using MySqlConnector;

namespace HighMetro.Services;

public class DbService : IDbService
{
    private string _connectionString = string.Empty;
    private string _currDate=string.Empty; 
    private int _bh;
    public ResultInfo TestConnection(DbSetting setting)
    {
        var resultInfo = new ResultInfo();
        try
        {
            using (var conn = new MySqlConnection(setting.GetConnectionString()))
            {
                var sql = "select count(*) from t_user;";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    conn.Open();
                }
            }

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

    public ResultInfo VerifyUser(LoginSetting loginSetting, DbSetting dbSetting)
    {
        var resultInfo = new ResultInfo
        {
            Code = PublicConst.FlagYes
        };
        try
        {
            using (var conn = new MySqlConnection(dbSetting.GetConnectionString()))
            {
                var sql = "select password from t_user where username=@username and efftflag=@efftFlag;";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@username", loginSetting.LoginUser);
                    cmd.Parameters.AddWithValue("@efftFlag", PublicConst.FlagYes);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
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
                    }
                }
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

    public ResultInfo VerifyHost(HostSetting hostSetting, DbSetting dbSetting)
    {
        var resultInfo = new ResultInfo();
        try
        {
            using (var conn = new MySqlConnection(dbSetting.GetConnectionString()))
            {
                var sql = "select code,name,ip,port from t_host where bh=@bh and flag=@flag;";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@bh", hostSetting.Bh);
                    cmd.Parameters.AddWithValue("@flag", PublicConst.FlagYes);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            resultInfo.Code = PublicConst.FlagYes;
                            resultInfo.Message = "";
                            return resultInfo;
                        }
                    }
                }
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

    private string GetConnectionString()
    {
        if (_connectionString.Equals(string.Empty))
        {
            _connectionString = DataBaseConnect.Instance.GetConnectionString();
        }
        return _connectionString;
    }

    public ResultHostInfo GetHostList(DbSetting dbSetting)
    {
        try
        {
            using (var conn = new MySqlConnection(dbSetting.GetConnectionString()))
            {
                var sql = "select bh,code,name,ip,port from t_host where flag=@flag;";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@flag", PublicConst.FlagYes);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        var hostInfoList = new List<HostInfo>();
                        while (reader.Read())
                        {
                            var hostInfo = new HostInfo
                            {
                                Bh = Convert.ToInt32(reader["bh"]),
                                Code = reader["code"].ToString()??string.Empty,
                                Name = reader["name"].ToString()??string.Empty,
                                Ip = reader["ip"].ToString()??string.Empty,
                                Port = Convert.ToInt32(reader["port"])
                            };
                            hostInfoList.Add(hostInfo);
                        }
                        var resultInfo = new ResultInfo();
                        resultInfo.Code = PublicConst.FlagYes;
                        resultInfo.Message = "";
                        var resultHostInfo = new ResultHostInfo
                        {
                            HostList = hostInfoList,
                            ReturnInfo = resultInfo
                        };
                        return resultHostInfo;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            var resultInfo = new ResultInfo();
            resultInfo.Code = PublicConst.FlagNo;
            resultInfo.Message = "获取工控机信息异常，错误原因:" + ex.Message;
            var resultHostInfo = new ResultHostInfo
            {
                ReturnInfo = resultInfo
            };
            return resultHostInfo;
        }
    }

    public ResultInfo GetHostInfo(HostInfo hostInfo)
    {
        var resultInfo = new ResultInfo();
        try
        {
            using (var conn = new MySqlConnection(GetConnectionString()))
            {
                var sql = "select code,name,ip,port from t_host where bh=@bh and flag=@flag;";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@bh", hostInfo.Bh);
                    cmd.Parameters.AddWithValue("@flag", PublicConst.FlagYes);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            hostInfo.Code = reader["code"].ToString()??string.Empty;
                            hostInfo.Name = reader["name"].ToString()??string.Empty;
                            hostInfo.Ip = reader["ip"].ToString()??string.Empty;
                            hostInfo.Port = Convert.ToInt32(reader["port"]);
                            resultInfo.Code = PublicConst.FlagYes;
                            resultInfo.Message = "";
                            return resultInfo;
                        }

                        ;
                    }
                }
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

    public ResultInfo GetHardCamera(HardInfo hardInfo)
    {
        var resultInfo = new ResultInfo();
        try
        {
            using (var conn = new MySqlConnection(GetConnectionString()))
            {
                var sql = "select bh,ip,port,username,password,type from t_hardcamera where hostbh=@hostBh and type=@type;";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@hostBh", hardInfo.HostBh);
                    cmd.Parameters.AddWithValue("@type", hardInfo.Type);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            hardInfo.UserName = reader["username"].ToString()??string.Empty;
                            hardInfo.PassWord = reader["password"]?.ToString()??string.Empty;
                            hardInfo.Ip = reader["ip"].ToString()??string.Empty;
                            hardInfo.Port = Convert.ToInt32(reader["port"]);
                            hardInfo.Bh = Convert.ToInt32(reader["bh"]);
                            hardInfo.Type = reader["type"].ToString()??string.Empty;
                        }
                    }
                }
            }
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

    public ResultSerialCommInfo GetCommInfoList(HostInfo hostInfo, string commType)
    {
        var resultSerialCommInfo=new ResultSerialCommInfo();
        try
        {
            using (var conn = new MySqlConnection(GetConnectionString()))
            {
                var sql = "SELECT * FROM t_mainbord WHERE hostbh = @hostbh and commType=@commType";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@hostbh", hostInfo.Bh);
                    cmd.Parameters.AddWithValue("@commType", commType);
                    conn.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        var serialList = new List<SerialCommInfo>();
                        // 循环读取所有行
                        while (reader.Read())
                        {
                            var serialComm00 = new SerialCommInfo();
                            serialComm00.HostBh = Convert.ToInt32(reader["hostbh"]);
                            serialComm00.Bh = Convert.ToInt32(reader["bh"]);
                            serialComm00.Id = Convert.ToInt32(reader["id"]);
                            serialComm00.Name = reader["name"].ToString()??string.Empty;
                            serialComm00.CommName = reader["commname"].ToString()??string.Empty;
                            serialComm00.BaudRate = Convert.ToInt32(reader["baudRate"]);
                            serialComm00.Parity = Convert.ToInt32(reader["parity"]);
                            serialComm00.DataBits = Convert.ToInt32(reader["dataBits"]);
                            serialComm00.StopBits = Convert.ToInt32(reader["stopBits"]);
                            serialComm00.CommType = reader["CommType"].ToString()??string.Empty;
                            serialList.Add(serialComm00);
                        }
                        resultSerialCommInfo.SerialCommList = serialList;
                    }
                }
            }
            var resultInfo = new ResultInfo
            {
                Code = PublicConst.FlagYes,
                Message = ""
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
    public ResultInfo AddHardCamera(HardInfo hardInfo)
    {
        var resultInfo = new ResultInfo();
        try
        {
            using (var conn = new MySqlConnection(GetConnectionString()))
            {
                conn.Open();
                var sql = "INSERT INTO t_hardcamera (hostbh,type,ip, port,username,password) " +
                             "VALUES (@hostbh,@type,@ip, @port,@username,@password);SELECT LAST_INSERT_ID();";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    // 添加参数（避免拼接字符串导致SQL注入）
                    cmd.Parameters.AddWithValue("@hostbh", hardInfo.HostBh);
                    cmd.Parameters.AddWithValue("@type", hardInfo.Type);
                    cmd.Parameters.AddWithValue("@ip", hardInfo.Ip);
                    cmd.Parameters.AddWithValue("@port", hardInfo.Port);
                    cmd.Parameters.AddWithValue("@username", hardInfo.UserName);
                    cmd.Parameters.AddWithValue("@password", hardInfo.PassWord);
                    var row = cmd.ExecuteNonQuery();
                }
            }
            resultInfo.Code = PublicConst.FlagYes;
            resultInfo.Message = "";
            return resultInfo;
        }
        catch (Exception ex)
        {
            resultInfo.Code = PublicConst.FlagNo;
            resultInfo.Message = "保存摄像机信息异常："+ ex.Message;
            return resultInfo;
        }
    }
    public ResultInfo EditHardCamera(HardInfo hardInfo)
    {
        var resultInfo = new ResultInfo();
        try
        {
            using (var conn = new MySqlConnection(GetConnectionString()))
            {
                conn.Open();
                var sql = "update t_hardcamera set hostbh=@hostbh,ip=@ip,port=@port,username=@username,password=@password where bh=@bh";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    // 添加参数（避免拼接字符串导致SQL注入）
                    cmd.Parameters.AddWithValue("@hostbh", hardInfo.HostBh);
                    cmd.Parameters.AddWithValue("@ip", hardInfo.Ip);
                    cmd.Parameters.AddWithValue("@port", hardInfo.Port);
                    cmd.Parameters.AddWithValue("@username", hardInfo.UserName);
                    cmd.Parameters.AddWithValue("@password", hardInfo.PassWord);
                    cmd.Parameters.AddWithValue("@bh", hardInfo.Bh);
                    cmd.ExecuteNonQuery();
                }
            }
            resultInfo.Code = PublicConst.FlagYes;
            resultInfo.Message = "";
            return resultInfo;
        }
        catch (Exception ex)
        {
            resultInfo.Code = PublicConst.FlagNo;
            resultInfo.Message = "保存摄像机信息异常："+ ex.Message;
            return resultInfo;
        }
    }
    public ResultInfo AddHeart(MainInfoBean mainInfoBean)
    {
        var resultInfo = new ResultInfo();
        try
        {
            using (var conn = new MySqlConnection(GetConnectionString()))
            {
                conn.Open();
                var sql = "INSERT INTO t_heart (hostbh,id, length,txzs,kmcs,yxms,agzms,astate,a1gzm,a2gzm,a1zs,a2zs," +
                    "a1dl,a2dl,a1wz,a2wz,bstate,b1gzm,b2gzm,b1zs,b2zs,b1dl,b2dl,b1wz,b2wz,dlcgqzt,dostate,kzdldo,total,datetime) " +
                    "VALUES (@hostbh,@id, @length,@txzs,@kmcs,@yxms,@agzms,@astate,@a1gzm,@a2gzm,@a1zs,@a2zs," +
                    "@a1dl,@a2dl,@a1wz,@a2wz,@bstate,@b1gzm,@b2gzm,@b1zs,@b2zs,@b1dl,@b2dl,@b1wz,@b2wz,@dlcgqzt,@dostate,@kzdldo,@total,@datetime);";
                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                {
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
                    cmd.ExecuteNonQuery();
                }
            }
            resultInfo.Code = PublicConst.FlagYes;
            resultInfo.Message = "";
            return resultInfo;
        }
        catch (Exception ex)
        {
            resultInfo.Code = PublicConst.FlagNo;
            resultInfo.Message = "保存心跳信息异常："+ ex.Message;
            return resultInfo;
        }
    }
    public ResultInfo SavePersonDay(MainInfoBean mainInfoBean)
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
            var returnValue = GetPersonDay(mainInfoBean00);
            if (returnValue.Code.Equals(PublicConst.FlagNo))
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
            return UpdatePersonDay(mainInfoBean);
        }
        else
        {
            return AddPersonDay(mainInfoBean);
        }
    }
    private ResultInfo GetPersonDay(MainInfoBean mainInfoBean)
    {
        var resultInfo = new ResultInfo();
        try
        {
            using (var conn = new MySqlConnection(GetConnectionString()))
            {
                var sql = "select bh,personcount from t_personday where hostbh=@hostbh and id=@id and date=@date;";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@hostbh", mainInfoBean.HostBh);
                    cmd.Parameters.AddWithValue("@id", mainInfoBean.Id);
                    cmd.Parameters.AddWithValue("@date", mainInfoBean.Datetime);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            mainInfoBean.Bh = Convert.ToInt32(reader["bh"]);
                            mainInfoBean.Kmcs = Convert.ToInt32(reader["personcount"]);
                            resultInfo.Tag = 1;
                        }
                        else
                        {
                            resultInfo.Tag = 0;
                        }
                    }
                }
            }
            resultInfo.Code = PublicConst.FlagYes;
            return resultInfo;               
        }
        catch (Exception ex)
        {
            resultInfo.Code = PublicConst.FlagNo;
            resultInfo.Message = "获取人数信息异常："+ ex.Message;
            return resultInfo;
        }
    }
    private ResultInfo AddPersonDay(MainInfoBean mainInfoBean)
    {
        var resultInfo = new ResultInfo();
        try
        {
            using (var conn = new MySqlConnection(GetConnectionString()))
            {
                conn.Open();
                var sql = "INSERT INTO t_personday (hostbh,id,personcount,date) values(@hostbh,@id,@personcount,@date);SELECT LAST_INSERT_ID();";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    // 添加参数（避免拼接字符串导致SQL注入）
                    cmd.Parameters.AddWithValue("@hostbh", mainInfoBean.HostBh);
                    cmd.Parameters.AddWithValue("@id", mainInfoBean.Id);
                    cmd.Parameters.AddWithValue("@personcount", mainInfoBean.Kmcs);
                    cmd.Parameters.AddWithValue("@date", mainInfoBean.Datetime);
                    // 执行并返回自增ID
                    object result = cmd.ExecuteScalar();
                    _bh = Convert.ToInt32(result);
                    _currDate = mainInfoBean.Datetime;
                }
            }
            resultInfo.Code = PublicConst.FlagYes;
            return resultInfo;    
        }
        catch (Exception ex)
        {
            resultInfo.Code = PublicConst.FlagNo;
            resultInfo.Message = "保存心跳信息异常："+ ex.Message;
            return resultInfo;
        }
    }
    private ResultInfo UpdatePersonDay(MainInfoBean mainInfoBean)
    {
        var resultInfo = new ResultInfo();
        try
        {
            using (var conn = new MySqlConnection(GetConnectionString()))
            {
                conn.Open();
                var sql = "update t_personday set personcount=@personcount where bh=@bh;";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    // 添加参数（避免拼接字符串导致SQL注入）
                    cmd.Parameters.AddWithValue("@personcount", mainInfoBean.Kmcs);
                    cmd.Parameters.AddWithValue("@bh", _bh);
                    cmd.ExecuteNonQuery();
                    // 执行并返回行数
                    cmd.ExecuteNonQuery();
                }
            }
            resultInfo.Code = PublicConst.FlagYes;
            return resultInfo;   
        }
        catch (Exception ex)
        {
            resultInfo.Code = PublicConst.FlagNo;
            resultInfo.Message = "保存每天通过人数信息异常："+ ex.Message;
            return resultInfo;
        }
    }
    public ResultInfo AddError(CameraBean cameraBean)
    {
        var resultInfo = new ResultInfo();
        try
        {
            using (var conn = new MySqlConnection(GetConnectionString()))
            {
                conn.Open();
                var sql = "INSERT INTO t_error (hostbh,door,type,datetime,id,message,serial) " +
                             "VALUES (@hostbh, @door,@type,@datetime,@id,@message,@serial);";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    // 添加参数（避免拼接字符串导致SQL注入）
                    cmd.Parameters.AddWithValue("@hostbh", cameraBean.HostBh);
                    cmd.Parameters.AddWithValue("@door", cameraBean.Door);
                    cmd.Parameters.AddWithValue("@type", cameraBean.Type);
                    cmd.Parameters.AddWithValue("@datetime", cameraBean.DateTime);
                    cmd.Parameters.AddWithValue("@id", cameraBean.Id);
                    cmd.Parameters.AddWithValue("@message", cameraBean.Message);
                    cmd.Parameters.AddWithValue("@serial", cameraBean.Serial);
                    cmd.ExecuteNonQuery();
                }
            }
            resultInfo.Code = PublicConst.FlagYes;
            return resultInfo;   
        }
        catch (Exception ex)
        {
            resultInfo.Code = PublicConst.FlagNo;
            resultInfo.Message = "保存日志信息异常："+ ex.Message;
            return resultInfo;
        }
    }
    public ResultInfo AddAlarm(CameraBean cameraBean)
    {
        var resultInfo = new ResultInfo();
        try
        {
            using (var conn = new MySqlConnection(GetConnectionString()))
            {
                conn.Open();
                var sql = "INSERT INTO t_alarm (id,door,type,datetime,upload,filepath,serial,hostbh) " +
                             "VALUES (@id,@door,@type,@datetime,@upload,@filepath,@serial,@hostbh);";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    // 添加参数（避免拼接字符串导致SQL注入）
                    cmd.Parameters.AddWithValue("@id", cameraBean.Id);
                    cmd.Parameters.AddWithValue("@door", cameraBean.Door);
                    cmd.Parameters.AddWithValue("@type", cameraBean.Type);
                    cmd.Parameters.AddWithValue("@datetime", cameraBean.DateTime);
                    cmd.Parameters.AddWithValue("@upload", "N");
                    cmd.Parameters.AddWithValue("@filepath", cameraBean.FilePath);
                    cmd.Parameters.AddWithValue("@serial", cameraBean.Serial);
                    cmd.Parameters.AddWithValue("@hostbh", cameraBean.HostBh);
                    cmd.ExecuteNonQuery();
                }
            }
            resultInfo.Code = PublicConst.FlagYes;
            return resultInfo;   
        }
        catch (Exception ex)
        {
            resultInfo.Code = PublicConst.FlagNo;
            resultInfo.Message = "保存拍照信息异常："+ ex.Message;
            return resultInfo;
        }
    }
}