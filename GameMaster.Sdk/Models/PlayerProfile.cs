namespace GameMaster.Sdk.Models;

public class PlayerProfile
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int Points { get; set; }
    public int Coins { get; set; }
}
