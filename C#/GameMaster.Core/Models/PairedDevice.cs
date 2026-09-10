using System;
namespace GameMaster.Core.Models;
public class PairedDevice {
    public Guid DeviceId { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public Platform Platform { get; set; }
    public string PairCodeHash { get; set; } = string.Empty;
    public DateTime LastSyncAt { get; set; }
    public string SyncVectorClock { get; set; } = string.Empty;
}
