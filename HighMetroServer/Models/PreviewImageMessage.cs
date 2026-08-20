namespace HighMetroServer.Models;

public class PreviewImageMessage(string filePath,string fileType)
{
    public string FilePath { get; private set; } = filePath;
    public string FileType { get; private set; } = fileType;
}