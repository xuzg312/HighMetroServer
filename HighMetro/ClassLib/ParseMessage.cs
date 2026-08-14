using System;
using System.Text;
using HighMetro.BaseModel;

namespace HighMetro.ClassLib;

public class ParseMessage
{
    public string ParseHexMessage(SocketDataBlock socketDataBlock)
    {
        StringBuilder stringBuilder = new StringBuilder("");
        //16进制；每个字节对应一个16进制数； 
        for (int i = 0; i < socketDataBlock.Length; i++)
        {
            var value = Convert.ToString(socketDataBlock.Content![i], 16).ToUpper();
            if (value.Length < 2)
            {
                value = "0" + value;
            }

            stringBuilder.Append(value).Append(" ");
        }
        stringBuilder.Append("\r\n");
        return stringBuilder.ToString();
    }
}