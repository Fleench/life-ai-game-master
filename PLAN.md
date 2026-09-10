# Game Master — Project Plan

## Overview

**Game Master** is a life-gamification hub application. It manages a single player's game state (points, XP, inventory) across devices and exposes that state to other "connected apps" via a granular permissions system. It is the trusted authority that other apps consult to award or spend in-game resources.

### Key Properties
- Built with **Avalonia UI** in **C#**, targeting **Linux desktop** and **Android**
- iOS support via remote REST API over LAN
- Local data stored in **SQLite**
- Device pairing via code + **automatic LAN sync** once paired
- Platform-specific IPC: **Android Binder** (Android) and **REST API** (Desktop/iOS)
- Single player per device; devices can be linked to share one player profile
- Full **granular per-app, per-resource permissions**

---

## Execution Model

```
┌────────────────────────────────────┐
│  STEP 1 — Scaffold & Core          │  ← Must complete first (all others depend on it)
│  (sequential, single agent)        │
└────────────────┬───────────────────┘
                 │ done
                 ▼
┌────────────────────────────────────────────────────────────────────┐
│  STEP 2 — Concurrent Sections  (each section = one agent, run in parallel) │
│                                                                    │
│  Section A: Desktop REST API Backend                               │
│  Section B: Android Binder Backend                                 │
│  Section C: Avalonia UI                                            │
│  Section D: Device Sync (LAN Pairing & Auto-Sync)                 │
└────────────────────────────────────────────────────────────────────┘
                 │ all sections done
                 ▼
┌────────────────────────────────────┐
│  STEP 3 — Polish & Hardening       │  ← Runs after all Step 2 sections complete
│  (sequential, single agent)        │
└────────────────────────────────────┘
```

> **Rule for Step 2 agents:** Each section's agent works only in its designated project(s) listed at the top of that section. Agents must not modify `GameMaster.Core` or `GameMaster.Data` — only read them. If a Core change is needed, raise it as a comment / TODO for the Scaffold agent to review.

---

## Architecture

```
┌─────────────────────────────────────────────────────┐
│                  Game Master App                    │
│                                                     │
│  ┌──────────────┐    ┌──────────────────────────┐  │
│  │  Avalonia UI  │    │     Core Service Layer   │  │
│  │  ─────────── │    │  ──────────────────────  │  │
│  │  • App Hub   │◄──►│  • PlayerService         │  │
│  │  • REPL Tab  │    │  • InventoryService      │  │
│  └──────────────┘    │  • PermissionsService    │  │
│                       │  • AppRegistryService    │  │
│                       │  • SyncService           │  │
│                       └────────────┬─────────────┘  │
│                                    │                 │
│                       ┌────────────▼─────────────┐  │
│                       │     SQLite Data Layer    │  │
│                       └──────────────────────────┘  │
└───────────┬──────────────────────┬──────────────────┘
            │ Android              │ Linux/macOS
            ▼                      ▼
    Android Binder API      Local REST API
    (UID-based identity)    (localhost, API key auth)

Connected Apps ──────────────────────────────────────►
iOS Remote App ──── Remote REST API (over LAN) ──────►
```

---

## Data Model

### Player
- `id` (UUID) — stable across devices
- `display_name` (string)
- `created_at` (timestamp)

### Currency (two pools, may later be unified)
| Field | Type | Notes |
|---|---|---|
| `exp_points` | integer | Experience points (never negative) |
| `coins` | integer | May be merged into a unified dynamic pool |

### Inventory Items
| Field | Type | Notes |
|---|---|---|
| `item_id` | UUID | |
| `name` | string | |
| `quantity` | integer | |
| `metadata` | JSON blob | Arbitrary key-value per item type |
| `awarded_by_app` | string | Source app identifier |
| `created_at` / `updated_at` | timestamp | |

### Connected App Registry
| Field | Type | Notes |
|---|---|---|
| `app_id` | UUID | |
| `app_name` | string | |
| `platform` | enum | `android` \| `desktop` \| `ios_remote` |
| `api_key_hash` | string | Hashed API key (desktop/iOS only) |
| `android_uid` | integer | Android UID (Android only) |
| `registered_at` / `last_seen_at` | timestamp | |

### Permissions
| Field | Type | Notes |
|---|---|---|
| `app_id` | FK → App Registry | |
| `resource` | enum | `exp_points` \| `coins` \| `inventory` |
| `action` | enum | `read` \| `award` \| `spend` \| `manage` |
| `granted` | boolean | |
| `updated_at` | timestamp | |

### Paired Devices
| Field | Type | Notes |
|---|---|---|
| `device_id` | UUID | |
| `device_name` | string | |
| `platform` | enum | |
| `pair_code_hash` | string | Cleared after pairing |
| `last_sync_at` | timestamp | |
| `sync_vector_clock` | JSON | Per-entity vector clocks for conflict detection |

---

## Open Design Decisions

> Resolved for Step 1.

1. **Coins vs. unified points** — Dynamic pools (e.g. `currency_pools` table), but NOT user-editable.
2. **REST port** — Fixed at 7777.
3. **Avalonia on Android** — Avalonia supports Android but requires a thin shell project. Confirm the rendering approach before Section C.
4. **Conflict resolution** — Strict newest-data-wins (last-write-wins) for all syncs, even GM to GM.

---

---

# STEP 1 — Scaffold & Core

> **Must be completed before any concurrent section begins.**
> **Assigned projects:** `GameMaster.Core`, `GameMaster.Data`, `GameMaster.Tests`

This step creates the solution skeleton, defines all domain models and service interfaces, implements the SQLite data layer, and verifies it with unit tests. All other sections depend on the interfaces and implementations produced here.

## Tasks

- [x] **1.1** Create solution file `GameMaster.sln` with the following projects:
  - `GameMaster.Core` — domain models + service interfaces (no platform deps)
  - `GameMaster.Data` — SQLite implementation (`Microsoft.Data.Sqlite` + Dapper)
  - `GameMaster.Api` — shared REST DTO contracts (request/response records)
  - `GameMaster.App` — Avalonia UI shell (stub; filled in by Section C)
  - `GameMaster.Desktop` — Desktop host stub (filled in by Section A)
  - `GameMaster.Android` — Android host stub (filled in by Section B)
  - `GameMaster.Tests` — xUnit test project

- [x] **1.2** Define domain models in `GameMaster.Core/Models/`:
  - `Player.cs`
  - `CurrencyLedger.cs`
  - `InventoryItem.cs`
  - `ConnectedApp.cs`
  - `AppPermission.cs`
  - `PairedDevice.cs`
  - `Resource.cs` (enum: `ExpPoints`, `Coins`, `Inventory`)
  - `PermissionAction.cs` (enum: `Read`, `Award`, `Spend`, `Manage`)
  - `Platform.cs` (enum: `Desktop`, `Android`, `IosRemote`)

- [x] **1.3** Define service interfaces in `GameMaster.Core/Services/`:
  - `IPlayerService` — get/update player profile
  - `IPointsService` — award/spend exp_points and coins, query balances
  - `IInventoryService` — add/remove/query inventory items
  - `IAppRegistryService` — register, list, deregister connected apps; generate API key
  - `IPermissionsService` — grant/revoke/check permissions (per app × resource × action)
  - `ISyncService` — generate pairing code, confirm pair, unpair, trigger sync, get sync status

- [x] **1.4** Define `GameMaster.Core/Exceptions/`:
  - `InsufficientPointsException`
  - `PermissionDeniedException`
  - `AppNotFoundException`
  - `DeviceNotFoundException`

- [x] **1.5** Implement SQLite schema migrations in `GameMaster.Data/Migrations/`:
  - Migration runner applies scripts in version order at startup
  - `001_initial_schema.sql` — tables: `players`, `currency_ledger`, `inventory_items`, `connected_apps`, `app_permissions`, `paired_devices`, `sync_log`

- [x] **1.6** Implement all service interfaces in `GameMaster.Data/Services/`:
  - `SqlitePlayerService`
  - `SqlitePointsService`
  - `SqliteInventoryService`
  - `SqliteAppRegistryService` (generates API key, returns plaintext once, stores hash)
  - `SqlitePermissionsService`
  - `SqliteSyncService` (pairing logic only; full sync transport in Section D)

- [x] **1.7** Register all services via `GameMaster.Data/ServiceCollectionExtensions.cs` for easy DI setup in host projects

- [x] **1.8** Write unit tests in `GameMaster.Tests/` for all service implementations using xUnit + in-memory SQLite (`:memory:`)

---

---

# SECTION A — Desktop REST API Backend

> **Runs concurrently with Sections B, C, D, E after Step 1 is complete.**
> **Assigned projects:** `GameMaster.Desktop`, `GameMaster.Api`

Expose the Core services to connected apps and paired iOS devices via a local REST API hosted on Kestrel.

## Tasks

- [ ] **A.1** Configure `GameMaster.Desktop` as an ASP.NET Core host:
  - Kestrel bound to `localhost` and LAN IP, configurable port (default `7777`)
  - Register Core services from `GameMaster.Data` via DI
  - Graceful shutdown handling

- [ ] **A.2** Implement REST controllers in `GameMaster.Api/Controllers/` (versioned under `/v1/`):

  | Method | Route | Permission Required | Description |
  |---|---|---|---|
  | `POST` | `/v1/apps/register` | None (open) | Register app, receive API key |
  | `GET` | `/v1/player` | read on any resource | Get player profile |
  | `GET` | `/v1/points` | read on exp_points / coins | Get balances |
  | `POST` | `/v1/points/award` | award on resource | Award points |
  | `POST` | `/v1/points/spend` | spend on resource | Spend points |
  | `GET` | `/v1/inventory` | read on inventory | List items |
  | `POST` | `/v1/inventory/add` | award on inventory | Add item |
  | `POST` | `/v1/inventory/remove` | spend on inventory | Remove item |
  | `GET` | `/v1/permissions` | None (own app only) | Get caller's permissions |

- [ ] **A.3** Implement `ApiKeyAuthMiddleware`:
  - Reads `X-Api-Key` header
  - Hashes it, looks up `ConnectedApp` in DB
  - Attaches resolved `ConnectedApp` to `HttpContext.Items`
  - Returns `401` if key is missing or invalid

- [ ] **A.4** Implement `PermissionGuardMiddleware` (or action filter):
  - Reads required resource + action from endpoint metadata
  - Calls `IPermissionsService.CheckAsync(appId, resource, action)`
  - Returns `403` with `ProblemDetails` JSON if denied

- [ ] **A.5** All error responses use `ProblemDetails` (`application/problem+json`) with a human-readable `detail` field

- [ ] **A.6** Rate limiting: cap requests per API key using ASP.NET Core rate limiting middleware

- [ ] **A.7** Integration tests using `WebApplicationFactory` covering:
  - Happy-path for each endpoint
  - `401` on missing/bad API key
  - `403` on missing permission
  - `400` on invalid input

---

---

# SECTION B — Android Binder Backend

> **Runs concurrently with Sections A, C, D, E after Step 1 is complete.**
> **Assigned projects:** `GameMaster.Android`

Expose the Core services to other Android apps on the same device via Android Binder (IPC). Identity is the OS-enforced caller UID — no API key needed.

## Tasks

- [ ] **B.1** Set up `GameMaster.Android` as a .NET Android project:
  - Reference `GameMaster.Core` and `GameMaster.Data`
  - Configure DI and SQLite path for Android (`/data/data/<package>/databases/`)

- [ ] **B.2** Define the Binder interface `IGameMasterBinder` in C# (mirrors the REST surface):
  - `GetPlayer()`
  - `GetPoints()`
  - `AwardPoints(resource, amount)`
  - `SpendPoints(resource, amount)`
  - `GetInventory()`
  - `AddInventoryItem(name, qty, metadata)`
  - `RemoveInventoryItem(itemId, qty)`
  - `GetMyPermissions()`

- [ ] **B.3** Implement `GameMasterBinderService : Service`:
  - Override `OnBind(Intent)` to return the `IBinder`
  - Resolve caller identity via `Binder.CallingUid` for each call
  - Look up `ConnectedApp` from DB by `android_uid`; auto-register on first call with default deny-all permissions
  - Enforce permissions via `IPermissionsService` before each operation
  - Return `SecurityException` to caller on permission denied

- [ ] **B.4** Declare in `AndroidManifest.xml`:
  - Service exported with `android:permission="com.gamemaster.BIND"` (signature-level protection for trusted apps, or normal for third-party)
  - `BOOT_COMPLETED` receiver to start the service on device boot

- [ ] **B.5** Expose a `GameMasterClientLibrary` helper class (separate AAR / C# binding) that third-party Android apps can use to bind easily

- [ ] **B.6** Manual test checklist (emulator or device):
  - Service starts on boot
  - Client app can bind and call methods
  - Permission deny returns `SecurityException`
  - UID-based identity is correctly resolved

---

---

# SECTION C — Avalonia UI

> **Runs concurrently with Sections A, B, D, E after Step 1 is complete.**
> **Assigned projects:** `GameMaster.App`

Build the two-tab UI: App Hub (manage connected apps and permissions) and REPL Terminal (admin command interface). Target Linux desktop first; Android rendering validated afterward.

## Tasks

- [ ] **C.1** Configure `GameMaster.App` (Avalonia, .NET 8+):
  - MVVM with `CommunityToolkit.Mvvm`
  - DI with `Microsoft.Extensions.DependencyInjection`
  - Apply Fluent theme; monospace font for REPL

- [ ] **C.2** Implement shared **Player Stats Header** (persistent across tabs):
  - Displays: player name, EXP balance, Coins balance, inventory item count
  - Reactively updates when underlying data changes (polling or event-driven)

- [ ] **C.3** Implement **App Hub tab** (`AppHubView` / `AppHubViewModel`):
  - Scrollable list of registered connected apps
    - Shows: app name, platform badge, online status, last-seen timestamp
  - Selecting an app shows a **Permission Panel**:
    - Toggle grid: rows = resources (`ExpPoints`, `Coins`, `Inventory`), columns = actions (`Read`, `Award`, `Spend`, `Manage`)
    - Each toggle calls `IPermissionsService.Grant/Revoke`
  - **"Launch App"** button: `Process.Start` on Linux; `StartActivity` via intent on Android
  - **"Revoke App"** button: deregisters the app (calls `IAppRegistryService.DeregisterAsync`)

- [ ] **C.4** Implement **REPL Terminal tab** (`ReplView` / `ReplViewModel`):
  - Monospace output display (scrollable, read-only `TextBox` or `ScrollViewer` + `ItemsControl`)
  - Single-line input with history (↑/↓ keys cycle through previous commands)
  - Output is color-coded: green = success, red = error, yellow = warning, white = info
  - Command set:

    | Command | Description |
    |---|---|
    | `help` | List all commands |
    | `player` | Show player profile |
    | `points` | Show EXP and Coins balances |
    | `points award <exp\|coins> <amount>` | Award points directly |
    | `points spend <exp\|coins> <amount>` | Spend points directly |
    | `inventory list` | List all inventory items |
    | `inventory add <name> [qty]` | Add item |
    | `inventory remove <name> [qty]` | Remove item |
    | `apps list` | List all registered connected apps |
    | `apps revoke <app_id>` | Deregister an app |
    | `perms <app_id>` | Show all permissions for an app |
    | `perms <app_id> grant <resource> <action>` | Grant a permission |
    | `perms <app_id> revoke <resource> <action>` | Revoke a permission |
    | `sync status` | Show paired devices and last-sync timestamps |
    | `sync pair` | Generate a pairing code |
    | `sync unpair <device_id>` | Unpair a device |
    | `clear` | Clear the terminal output |

- [ ] **C.5** Android validation:
  - Confirm Avalonia renders correctly on Android target
  - Adjust layout for smaller screens (collapsible header, stacked tabs)

---

---

# SECTION D — Device Sync (LAN Pairing & Auto-Sync)

> **Runs concurrently with Sections A, B, C after Step 1 is complete.**
> **Assigned projects:** `GameMaster.Desktop` (sync endpoints), `GameMaster.Core` (sync interfaces already defined), `GameMaster.Data` (SqliteSyncService full impl), `GameMaster.Android` (WorkManager trigger)

Enable two paired devices to automatically sync game state whenever they are on the same LAN.

## Tasks

- [ ] **D.1** LAN discovery (both platforms):
  - Advertise service via mDNS: `_gamemaster._tcp.local`, broadcasting `device_id`, `device_name`, IP, port
  - Listen for other Game Master instances on the network
  - Use `Zeroconf` or `mdns-sd` NuGet package (or equivalent on Android via `NsdManager`)

- [ ] **D.2** Pairing flow:
  - `sync pair` (REPL on Device A): generates a 6-digit alphanumeric code, valid for 5 minutes; stored as hash in DB
  - Device B enters the code via its REPL or UI
  - Devices exchange `device_id` + ECDH public key over the REST sync endpoint
  - Both store each other's `device_id` and derive a shared sync secret

- [ ] **D.3** Add sync REST endpoints to `GameMaster.Desktop` (Section A adds the main API; D adds sync-specific routes):

  | Method | Route | Auth | Description |
  |---|---|---|---|
  | `POST` | `/v1/sync/pair` | Pairing code | Complete pairing handshake |
  | `GET` | `/v1/sync/state` | Device shared secret | Get current state snapshot + vector clocks |
  | `POST` | `/v1/sync/push` | Device shared secret | Push state delta from another device |

- [ ] **D.4** Sync protocol:
  - Each entity (`player`, `currency_ledger`, each `inventory_item`) carries a vector clock entry
  - On sync: compare vector clocks; apply non-conflicting updates; flag conflicts
  - Default conflict resolution: **last-write-wins** by `updated_at` timestamp
  - Conflicted items logged to `sync_log` table for REPL inspection

- [ ] **D.5** Auto-sync trigger:
  - **Desktop:** background `Timer` or `IHostedService` scans mDNS peers every 60s; initiates sync when a known paired device is found
  - **Android:** `WorkManager` periodic job (requires WiFi constraint) checks for peers and triggers sync

- [ ] **D.6** Full implementation of `SqliteSyncService`:
  - `GeneratePairingCodeAsync()` — create code, store hash + expiry
  - `ConfirmPairAsync(code, remoteDeviceId, remotePublicKey)` — validate, exchange keys, store peer
  - `UnpairAsync(deviceId)`
  - `GetSyncStatusAsync()` — return list of paired devices with `last_sync_at`
  - `PushStateAsync(delta)` / `GetStateSnapshotAsync()` — merge/export state with vector clocks

- [ ] **D.7** REPL `sync status` output shows: device name, platform, IP, last synced time, sync health (OK / conflict / unreachable)

---

---

# STEP 3 — Polish & Hardening

> **Runs after all Step 2 sections (A, B, C, D) are complete.**
> **Assigned projects:** All projects (read-only review + targeted additions)

Cross-cutting concerns: logging, security, auto-start, and documentation. This step reviews the output of all concurrent sections and applies finishing touches that require the full codebase to be in place.

## Tasks

- [ ] **E.1** Structured logging across all projects:
  - Use `Microsoft.Extensions.Logging` throughout Core + Data
  - Desktop: add `Serilog` with rolling file sink (`~/.local/share/gamemaster/logs/`)
  - Android: use `Android.Util.Log` sink via Serilog adapter
  - Log levels: `Debug` for DB queries, `Info` for service calls, `Warning` for permission denials, `Error` for exceptions

- [ ] **E.2** API key security:
  - Store API keys as `BCrypt` hashes (`BCrypt.Net-Next`)
  - Key is returned **once** in plain text at registration; never stored or shown again
  - Key rotation endpoint: `POST /v1/apps/rotate-key`

- [ ] **E.3** App auto-start on boot:
  - **Linux:** generate a systemd user service file (`~/.config/systemd/user/gamemaster.service`) targeting the Desktop host executable; document how to enable it
  - **Android:** `BOOT_COMPLETED` broadcast receiver (declared in `AndroidManifest.xml`) starts the Binder service (already wired in Section B)

- [ ] **E.4** Input validation:
  - All REST request bodies use `DataAnnotations` or `FluentValidation`
  - REPL command parser returns friendly error messages on bad input (never throws unhandled exceptions)

- [ ] **E.5** Documentation:
  - `README.md` — project overview, prerequisites, how to build, how to pair devices
  - `API.md` — full REST API reference (all routes, request/response schemas, auth, error codes)
  - `ARCHITECTURE.md` — component diagram, data flow, technology decisions, open design decisions

- [ ] **E.6** Final review checklist:
  - All endpoints return `ProblemDetails` on error
  - No plaintext API keys in logs or DB
  - Unit test coverage >80% on Core + Data
  - All REPL commands have help text in `help` output

---

## Technology Stack Summary

| Concern | Technology |
|---|---|
| UI Framework | Avalonia UI (C#) |
| Target Platforms | Linux desktop, Android |
| Remote Client | iOS via REST over LAN |
| Language | C# (.NET 8+) |
| Local Database | SQLite — `Microsoft.Data.Sqlite` + Dapper |
| Desktop IPC | ASP.NET Core / Kestrel (localhost + LAN REST) |
| Android IPC | Android Binder |
| Auth (desktop/iOS) | API key (`X-Api-Key` header, BCrypt hashed) |
| Auth (Android) | Binder UID (OS-enforced) |
| LAN Discovery | mDNS (`_gamemaster._tcp.local`) |
| Sync | Vector clocks + last-write-wins |
| MVVM | CommunityToolkit.Mvvm |
| DI | Microsoft.Extensions.DependencyInjection |
| Logging | Serilog (desktop) / Android.Util.Log (Android) |
| Testing | xUnit + in-memory SQLite |

---

# STEP 4 — Client SDK (C# Library)

> **Runs after Step 3 is complete.**
> **Assigned projects:** `GameMaster.Client` (new)

Create a portable C# client library (NuGet-ready) that can be included in any external app (local or remote) to seamlessly interact with the Game Master API.

## Tasks
- [ ] **F.1** Create a new `GameMaster.Client` class library targeting `.NET Standard 2.0` or `.NET 8`.
- [ ] **F.2** Implement a fluent API client that wraps all the REST API endpoints (points, inventory, permissions, etc.).
- [ ] **F.3** Handle API key authentication and automatic retries/error handling.
- [ ] **F.4** Ensure it can connect to both `localhost` (for local desktop apps) and remote IPs (for apps on other devices).
