namespace HighMetro.Models;

public class CodeNameModals(int value,string displayName)
{
    public string DisplayName { get; set; } = displayName;
    public int Value { get; set; } = value;
}