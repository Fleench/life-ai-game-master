# GameMaster Backend & Client Audit

## `ReplViewModel.cs` Analysis
The following commands are wired up and functioning within the REPL view model:
- `player`
- `points`
- `inventory`

The following commands are completely stubbed out in the REPL (returning `"Command execution pending."`):
- `apps`
- `perms`
- `sync`

## `IGameMasterClient.cs` Interface Analysis
Even if the REPL were wired up, the `IGameMasterClient` interface lacks the necessary methods to support the pending commands:
- **`apps`**: Missing methods for listing and revoking apps (`ListAppsAsync`, `RevokeAppAsync`). Currently, only `RegisterAppAsync` exists.
- **`perms`**: Missing methods for modifying permissions (`GrantPermissionAsync`, `RevokePermissionAsync`). Currently, only `GetMyPermissionsAsync` exists.
- **`sync`**: Missing all sync-related methods (`GetSyncStatusAsync`, `PairDeviceAsync`, `UnpairDeviceAsync`).

## Client Implementations Analysis
- **`HttpGameMasterClient.cs`**: Implements all currently defined methods in `IGameMasterClient` properly via HTTP calls.
- **`AndroidBinderGameMasterClient.cs`**: Every single method is stubbed out and throws a `NotImplementedException("IPC implementation pending for ...")`. This implementation is currently entirely non-functional.
