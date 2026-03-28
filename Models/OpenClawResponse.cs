namespace TinCan.Models;

public class OpenClawResponse
{
    public string? Suggestion { get; set; }
    public double Confidence { get; set; }
    public string? Reason { get; set; }
}
