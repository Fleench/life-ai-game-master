namespace GameMaster.Sdk.Models;

public class CoinTransactionResult
{
    public bool Success { get; set; }
    public int NewBalance { get; set; }
    public string Message { get; set; } = string.Empty;
}
