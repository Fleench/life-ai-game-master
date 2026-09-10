using System;
namespace GameMaster.Core.Models;
public class CurrencyPool {
    public string CurrencyId { get; set; } = string.Empty;
    public int Balance { get; set; }
    public DateTime UpdatedAt { get; set; }
}
