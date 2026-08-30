using System.Collections.ObjectModel;
using System.IO.Ports;
using HighMetroServer.Models;

namespace HighMetroServer.Parameters;

public static class CommParameter
{
    static CommParameter()
    {
        string[] commList = SerialPort.GetPortNames();
        PortNameList.Clear();

        // 循环遍历，索引+1作为Code，串口名作为Name
        for (var i = 0; i < commList.Length; i++)
        {
            var code = i + 1;
            var port = commList[i];
            PortNameList.Add(new CodeNameModals(code, port));
        }
    }

    public static ObservableCollection<CodeNameModals> PortNameList { get; } = [];
    public static readonly ObservableCollection<CodeNameModals> BaudRateList =
        [
            new CodeNameModals(9600, "9600"),
            new CodeNameModals(19200, "19200"),
            new CodeNameModals(38400, "38400"),
            new CodeNameModals(57600, "57600"),
            new CodeNameModals(115200, "115200")
        ];
    public static readonly ObservableCollection<CodeNameModals> DataBitsList = 
        [
            new CodeNameModals(7, "7:位数据"),
            new CodeNameModals(8, "8:位数据")
        ];
    public static readonly ObservableCollection<CodeNameModals> ParityList = 
        [
            new CodeNameModals(0, "0:无校验"),
            new CodeNameModals(1, "1:奇校验"),
            new CodeNameModals(2, "2:偶校验")
        ];
    public static readonly ObservableCollection<CodeNameModals> StopBitsList = 
        [
            new CodeNameModals(1, "1:停止位"),
            new CodeNameModals(2, "2:停止位")
        ];
}