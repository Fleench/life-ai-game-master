using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameMaster.Core.Models;
using GameMaster.Core.Services;
using Dapper;

namespace GameMaster.Data.Services;

public class SqliteSyncService : ISyncService {
    private readonly IDbConnectionFactory _db;

    public SqliteSyncService(IDbConnectionFactory db) {
        _db = db;
    }

    public async Task<string> GeneratePairingCodeAsync() {
        var code = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
        // Insert into paired_devices with dummy device to hold code temporarily
        return code;
    }

    public async Task ConfirmPairAsync(string code, Guid remoteDeviceId, string remotePublicKey) {
        // Validation and ECDH exchange logic here
    }

    public async Task UnpairAsync(Guid deviceId) {
        using var conn = await _db.CreateConnectionAsync();
        await conn.ExecuteAsync("DELETE FROM paired_devices WHERE device_id = @DeviceId", new { DeviceId = deviceId.ToString() });
    }

    public async Task TriggerSyncAsync(Guid deviceId) {
        // Auto-sync logic
    }

    public async Task<IEnumerable<PairedDevice>> GetSyncStatusAsync() {
        using var conn = await _db.CreateConnectionAsync();
        return await conn.QueryAsync<PairedDevice>("SELECT * FROM paired_devices");
    }

    public async Task PushStateAsync(string delta) {
        // Push state delta
    }

    public async Task<string> GetStateSnapshotAsync() {
        // Return snapshot
        return "{}";
    }
}
