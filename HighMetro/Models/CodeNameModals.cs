namespace HighMetro.Models;

public class CodeNameModals(int value, string displayName)
{
    public int Value { get; } = value;
    public string DisplayName { get; } = displayName;
    public override bool Equals(object? obj)
    {
        return obj is CodeNameModals item && item.Value == this.Value;
    }
    public override int GetHashCode()
    {
        return Value;
    }
}