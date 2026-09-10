using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameMaster.Core.Models;
namespace GameMaster.Core.Services;
public interface ISyncService {
    Task<string> GeneratePairingCodeAsync();
    Task ConfirmPairAsync(string code, Guid remoteDeviceId, string remotePublicKey);
    Task UnpairAsync(Guid deviceId);
    Task TriggerSyncAsync(Guid deviceId);
    Task<IEnumerable<PairedDevice>> GetSyncStatusAsync();
    Task PushStateAsync(string delta);
    Task<string> GetStateSnapshotAsync();
}
