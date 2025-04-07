namespace Common.Data.Models;

public class OrderBookSnapshotApiResp
{
    public string timestamp { get; set; }
    public string microtimestamp { get; set; }
    public List<List<string>> bids { get; set; }
    public List<List<string>> asks { get; set; }
}