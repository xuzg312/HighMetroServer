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

    public ResultInfo TestConnection(DbSetting setting)
    {
        ResultInfo resultInfo = new ResultInfo();
        try
        {
            using (MySqlConnection conn = new MySqlConnection(setting.GetConnectionString()))
            {
                string sql = "select count(*) from t_user;";
                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
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
        ResultInfo resultInfo = new ResultInfo();
        resultInfo.Code = PublicConst.FlagYes;
        try
        {
            using (MySqlConnection conn = new MySqlConnection(dbSetting.GetConnectionString()))
            {
                string sql = "select password from t_user where username=@username and efftflag=@efftFlag;";
                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@username", loginSetting.LoginUser);
                    cmd.Parameters.AddWithValue("@efftFlag", PublicConst.FlagYes);
                    conn.Open();
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string loginPassword = reader["password"].ToString() ?? string.Empty;
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
        ResultInfo resultInfo = new ResultInfo();
        try
        {
            using (MySqlConnection conn = new MySqlConnection(dbSetting.GetConnectionString()))
            {
                string sql = "select code,name,ip,port from t_host where bh=@bh and flag=@flag;";
                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@bh", hostSetting.Bh);
                    cmd.Parameters.AddWithValue("@flag", PublicConst.FlagYes);
                    conn.Open();
                    using (MySqlDataReader reader = cmd.ExecuteReader())
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
            using (MySqlConnection conn = new MySqlConnection(dbSetting.GetConnectionString()))
            {
                string sql = "select bh,code,name,ip,port from t_host where flag=@flag;";
                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@flag", PublicConst.FlagYes);
                    conn.Open();
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        List<HostInfo> hostInfoList = new List<HostInfo>();
                        while (reader.Read())
                        {
                            HostInfo hostInfo = new HostInfo();
                            hostInfo.Bh = Convert.ToInt32(reader["bh"]);
                            hostInfo.Code = reader["code"].ToString()??string.Empty;
                            hostInfo.Name = reader["name"].ToString()??string.Empty;
                            hostInfo.Ip = reader["ip"].ToString()??string.Empty;
                            hostInfo.Port = Convert.ToInt32(reader["port"]);
                            hostInfoList.Add(hostInfo);
                        }
                        ResultInfo resultInfo = new ResultInfo();
                        resultInfo.Code = PublicConst.FlagYes;
                        resultInfo.Message = "";
                        ResultHostInfo resultHostInfo = new ResultHostInfo();
                        resultHostInfo.HostList = hostInfoList;
                        resultHostInfo.ReturnInfo = resultInfo;
                        return resultHostInfo;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ResultInfo resultInfo = new ResultInfo();
            resultInfo.Code = PublicConst.FlagNo;
            resultInfo.Message = "获取工控机信息异常，错误原因:" + ex.Message;
            ResultHostInfo resultHostInfo = new ResultHostInfo();
            resultHostInfo.ReturnInfo = resultInfo;
            return resultHostInfo;
        }
    }

    public ResultInfo GetHostInfo(HostInfo hostInfo)
    {
        ResultInfo resultInfo = new ResultInfo();
        try
        {
            using (MySqlConnection conn = new MySqlConnection(GetConnectionString()))
            {
                string sql = "select code,name,ip,port from t_host where bh=@bh and flag=@flag;";
                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@bh", hostInfo.Bh);
                    cmd.Parameters.AddWithValue("@flag", PublicConst.FlagYes);
                    conn.Open();
                    using (MySqlDataReader reader = cmd.ExecuteReader())
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
        ResultInfo resultInfo = new ResultInfo();
        try
        {
            using (MySqlConnection conn = new MySqlConnection(GetConnectionString()))
            {
                string sql =
                    "select bh,ip,port,username,password,type from t_hardcamera where hostbh=@hostBh and type=@type;";
                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@hostBh", hardInfo.HostBh);
                    cmd.Parameters.AddWithValue("@type", hardInfo.Type);
                    conn.Open();
                    using (MySqlDataReader reader = cmd.ExecuteReader())
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
        ResultSerialCommInfo resultSerialCommInfo=new ResultSerialCommInfo();
        try
        {
            using (MySqlConnection conn = new MySqlConnection(GetConnectionString()))
            {
                string sql = "SELECT * FROM t_mainbord WHERE hostbh = @hostbh and commType=@commType";
                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@hostbh", hostInfo.Bh);
                    cmd.Parameters.AddWithValue("@commType", commType);
                    conn.Open();

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        List<SerialCommInfo> serialList = new List<SerialCommInfo>();
                        // 循环读取所有行
                        while (reader.Read())
                        {
                            SerialCommInfo serialComm00 = new SerialCommInfo();
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
                        ResultInfo resultInfo = new ResultInfo();
                        resultInfo.Code = PublicConst.FlagYes;
                        resultInfo.Message = "";
                        resultSerialCommInfo.ReturnInfo = resultInfo;
                        return resultSerialCommInfo;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ResultInfo resultInfo = new ResultInfo();
            resultInfo.Code = PublicConst.FlagNo;
            resultInfo.Message = "连接异常，请检查配置信息：" + ex.Message;
            resultSerialCommInfo.ReturnInfo = resultInfo;
            return resultSerialCommInfo;
        }
    }
}