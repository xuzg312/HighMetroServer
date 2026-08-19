using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media.Imaging;

namespace HighMetroServer.Views.Controls;

public partial class ImagePreview : Window
{
    public ImagePreview()
    {
        InitializeComponent();
    }
    public void LoadImage(string filePath)
    {
        if (!File.Exists(filePath))
        {
            ImgViewer.Source = null;
            this.Title = "文件不存在";
            return;
        }
        // Avalonia跨平台加载本地图片
        var bitmap = new Bitmap(filePath);
        ImgViewer.Source = bitmap;
        this.Title = Path.GetFileName(filePath);
    }
}