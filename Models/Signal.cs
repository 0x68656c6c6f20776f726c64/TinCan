namespace TinCan.Models;

public enum SignalType
{
    Buy,
    Sell,
    Hold
}

public class Signal
{
    public SignalType Type { get; set; }
    public string Reason { get; set; } = "";
    public double Confidence { get; set; } // 0.0 to 1.0
}
