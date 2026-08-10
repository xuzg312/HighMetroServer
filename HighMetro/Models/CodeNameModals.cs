namespace HighMetro.Models;

public class CodeNameModals
{
    public int Value { get;}
    public string DisplayName { get;}

    public CodeNameModals(int value, string displayName)
    {
        Value = value;
        DisplayName = displayName;
    }
    public override bool Equals(object? obj)
    {
        return obj is CodeNameModals item && item.Value == this.Value;
    }

    public override int GetHashCode()
    {
        return Value;
    }
}