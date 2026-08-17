namespace HighMetro.Models;

public class PreviewImageMessage(string filePath)
{
    public string FilePath { get; private set; } = filePath;
}