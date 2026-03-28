namespace TinCan.Models;

public class OpenClawResponse
{
    public string Suggestion { get; set; } = "hold";
    public double Confidence { get; set; } = 0.0;
    public string Reason { get; set; } = "";
}
