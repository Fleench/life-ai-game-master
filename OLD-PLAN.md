# Game Master — Project Plan

## Overview

**Game Master** is a life-gamification hub application. It manages a single player's game state (points, XP, inventory) across devices and exposes that state to other "connected apps" via a granular permissions system. It is not a game itself — it is the trusted authority that other apps consult to award or spend in-game resources.

### Key Properties
- Built with **Avalonia UI** in **C#**, targeting **Linux desktop** and **Android**
- iOS support via remote REST API
- Local data stored in **SQLite**
- Device pairing via code + **automatic LAN sync** once paired
- Two platform-specific IPC backends: **Android Binder** and **Desktop REST API**
- Single player per device; devices can be linked to share one player profile
- Full **granular per-app, per-resource permissions**

---

## Architecture Diagram

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
  (3rd-party apps award/spend resources with permission)

iOS Remote App ──── Remote REST API (over LAN) ──────►
```

---

## Data Model

### Player
- `id` (UUID) — stable across devices
- `display_name` (string)
- `created_at` (timestamp)

### Currency
| Field | Type | Notes |
|---|---|---|
| `exp_points` | integer | Experience points (never negative) |
| `coins` | integer | Optional; may be merged into a unified "points" pool |

> **Design note:** The current plan keeps `exp_points` and `coins` as two separate pools. If coins are removed, they become a second point pool type defined dynamically.

### Inventory
- `item_id` (UUID)
- `name` (string)
- `quantity` (integer)
- `metadata` (JSON blob — arbitrary key-value data per item type)
- `awarded_by_app` (string — source app identifier)
- `created_at` / `updated_at` (timestamps)

### Connected App Registry
- `app_id` (UUID)
- `app_name` (string)
- `platform` (enum: `android` | `desktop` | `ios_remote`)
- `api_key_hash` (string — hashed API key, desktop/iOS only)
- `android_uid` (integer — Android UID, Android only)
- `registered_at` (timestamp)
- `last_seen_at` (timestamp)

### Permissions
- `app_id` (foreign key → App Registry)
- `resource` (enum: `exp_points` | `coins` | `inventory`)
- `action` (enum: `read` | `award` | `spend` | `manage`)
- `granted` (boolean)
- `updated_at` (timestamp)

### Sync / Paired Devices
- `device_id` (UUID)
- `device_name` (string)
- `platform` (enum)
- `pair_code_hash` (string — used during pairing; cleared after)
- `last_sync_at` (timestamp)
- `sync_vector_clock` (JSON — for conflict resolution)

---

## Phase 1 — Project Scaffold & Core Abstractions

> **Goal:** Set up the solution structure, define all interfaces, and wire up the SQLite data layer with no platform-specific code.

### Tasks

- [ ] **1.1** Create solution file `GameMaster.sln` with the following projects:
  - `GameMaster.Core` — domain models, service interfaces, no platform deps
  - `GameMaster.Data` — SQLite implementation via `Microsoft.Data.Sqlite` + Dapper
  - `GameMaster.Api` — shared REST API contracts (DTOs, request/response records)
  - `GameMaster.App` — Avalonia UI app (references Core + Data)
  - `GameMaster.Android` — Android-specific host project (Binder service)
  - `GameMaster.Desktop` — Desktop-specific host project (REST API host)

- [ ] **1.2** Define domain models in `GameMaster.Core/Models/`:
  - `Player.cs`
  - `CurrencyLedger.cs` (holds exp_points, coins)
  - `InventoryItem.cs`
  - `ConnectedApp.cs`
  - `AppPermission.cs`
  - `PairedDevice.cs`

- [ ] **1.3** Define service interfaces in `GameMaster.Core/Services/`:
  - `IPlayerService` — get/set player profile
  - `IPointsService` — award/spend exp_points and coins, get balances
  - `IInventoryService` — add/remove/query inventory items
  - `IAppRegistryService` — register apps, list apps, deregister apps
  - `IPermissionsService` — grant/revoke/check permissions per app+resource+action
  - `ISyncService` — pair device, unpair, trigger sync, resolve conflicts

- [ ] **1.4** Implement SQLite schema migrations in `GameMaster.Data/`:
  - Write SQL migration scripts (versioned, applied at startup)
  - Tables: `players`, `currency_ledger`, `inventory_items`, `connected_apps`, `app_permissions`, `paired_devices`, `sync_log`

- [ ] **1.5** Implement all service interfaces in `GameMaster.Data/Services/`:
  - `SqlitePlayerService`
  - `SqlitePointsService`
  - `SqliteInventoryService`
  - `SqliteAppRegistryService`
  - `SqlitePermissionsService`
  - `SqliteSyncService` (stub — pairing logic only; full sync in Phase 4)

- [ ] **1.6** Write unit tests for all service implementations (`GameMaster.Tests/` project using xUnit + in-memory SQLite)

---

## Phase 2 — Platform-Specific Backends

> **Goal:** Expose the core services to connected apps via platform-appropriate IPC.

### 2A — Desktop REST API (Linux / macOS)

- [ ] **2A.1** Create `GameMaster.Desktop` host using `Microsoft.AspNetCore` (Kestrel on `localhost:<port>`)
- [ ] **2A.2** Define REST endpoints in `GameMaster.Api`:
  - `POST /apps/register` — register a new connected app, returns API key
  - `GET /player` — get player profile
  - `GET /points` — get current balances (requires `read` on `exp_points` or `coins`)
  - `POST /points/award` — award points (requires `award` permission)
  - `POST /points/spend` — spend points (requires `spend` permission)
  - `GET /inventory` — list inventory items (requires `read` on `inventory`)
  - `POST /inventory/add` — add item (requires `award` on `inventory`)
  - `POST /inventory/remove` — remove item (requires `spend` on `inventory`)
  - `GET /permissions` — get this app's permissions
- [ ] **2A.3** Implement API key authentication middleware:
  - Connected apps include `X-Api-Key: <key>` header
  - Middleware resolves `ConnectedApp` from key, attaches to request context
- [ ] **2A.4** Implement permission guard middleware:
  - Checks `IPermissionsService` before each action endpoint
  - Returns `403 Forbidden` with a human-readable reason if denied
- [ ] **2A.5** Integration tests for all endpoints using `WebApplicationFactory`

### 2B — Android Binder Service

- [ ] **2B.1** Create `GameMaster.Android` MAUI/Android project
- [ ] **2B.2** Define AIDL interface `IGameMasterService.aidl` (or equivalent C# Binder subclass)
  - Methods mirror the REST API surface
- [ ] **2B.3** Implement `GameMasterBinderService` — wraps Core services, enforces UID-based identity (caller UID from `Binder.getCallingUid()`)
- [ ] **2B.4** Implement Android permission enforcement using `IPermissionsService` (same logic as REST, identity comes from UID instead of API key)
- [ ] **2B.5** Declare service in `AndroidManifest.xml` with appropriate `android:permission` protection level

---

## Phase 3 — Avalonia UI

> **Goal:** Build the two-tab Avalonia UI: App Hub and REPL Terminal.

- [ ] **3.1** Set up Avalonia project targeting Linux desktop (`GameMaster.App`)
  - Use MVVM pattern (CommunityToolkit.Mvvm)
  - Configure dependency injection (Microsoft.Extensions.DependencyInjection)

- [ ] **3.2** Implement **App Hub tab** (`AppHubView` + `AppHubViewModel`):
  - List all registered connected apps with their status (online/offline, last seen)
  - Per-app permission editor: toggle read/award/spend/manage per resource
  - "Launch App" button (Linux: `Process.Start`; Android: `Intent`)
  - "Revoke" button to deregister an app

- [ ] **3.3** Implement **REPL Terminal tab** (`ReplView` + `ReplViewModel`):
  - Text input + output display (monospace, scrollable)
  - Command parser for admin commands:
    - `player` — show player profile
    - `points` — show balances
    - `points award <exp|coins> <amount>` — directly award points
    - `points spend <exp|coins> <amount>` — directly spend points
    - `inventory list` — list all items
    - `inventory add <name> [qty]` — add item
    - `inventory remove <name> [qty]` — remove item
    - `apps list` — list registered apps
    - `apps revoke <app_id>` — revoke app
    - `perms <app_id>` — show permissions for app
    - `perms <app_id> grant <resource> <action>` — grant permission
    - `perms <app_id> revoke <resource> <action>` — revoke permission
    - `sync status` — show paired devices and sync state
    - `help` — list all commands

- [ ] **3.4** Implement shared UI elements:
  - Player stats header (name, EXP, coins, item count)
  - Navigation bar / tab strip

- [ ] **3.5** Android UI: verify Avalonia renders correctly on Android or use Android-specific shell if needed

---

## Phase 4 — Device Sync (LAN Pairing & Auto-Sync)

> **Goal:** Allow two devices to pair via a code and then automatically sync game state when on the same LAN.

- [ ] **4.1** LAN discovery: implement mDNS/UDP broadcast so devices announce themselves on the local network
  - Service name: `_gamemaster._tcp.local`
  - Broadcast: device ID, device name, IP, port

- [ ] **4.2** Pairing flow:
  - Device A generates a 6-digit pairing code (short-lived, expires in 5 min)
  - Device B enters the code; both exchange device IDs and public keys
  - After pairing, devices store each other's `device_id` + IP hint in `paired_devices`

- [ ] **4.3** Sync protocol:
  - Use vector clocks per entity (player, currency, each inventory item) for conflict detection
  - Conflict resolution strategy: **last-write-wins** by timestamp, with REPL override for manual resolution
  - Sync trigger: when a paired device is discovered on LAN, initiate sync automatically
  - Sync endpoint: add `POST /sync/push` and `GET /sync/state` to REST API (authenticated by device ID + shared secret from pairing)

- [ ] **4.4** Android sync: trigger sync via a background `WorkManager` job when on WiFi

- [ ] **4.5** Sync status visible in REPL (`sync status`) and in UI (last synced timestamp per device)

---

## Phase 5 — Polish & Hardening

- [ ] **5.1** Logging: structured logging throughout using `Microsoft.Extensions.Logging` + file sink
- [ ] **5.2** Error handling: all API endpoints return consistent `ProblemDetails` JSON errors
- [ ] **5.3** API versioning: prefix all routes with `/v1/`
- [ ] **5.4** App auto-start on boot:
  - Linux: systemd user service for the REST API host
  - Android: `BOOT_COMPLETED` broadcast receiver to start Binder service
- [ ] **5.5** Security hardening:
  - API keys stored as bcrypt hashes in SQLite
  - Rate limiting on REST endpoints to prevent abuse
  - Reject requests with invalid/expired API keys with `401`
- [ ] **5.6** Documentation:
  - `README.md` — setup, pairing, building
  - `API.md` — full REST API reference
  - `ARCHITECTURE.md` — component diagram and decisions

---

## Technology Stack Summary

| Concern | Technology |
|---|---|
| UI Framework | Avalonia UI (C#) |
| Target Platforms | Linux desktop, Android |
| Remote Client Platform | iOS (REST over LAN) |
| Language | C# (.NET 8+) |
| Local Database | SQLite via `Microsoft.Data.Sqlite` + Dapper |
| Desktop IPC | ASP.NET Core / Kestrel (localhost REST) |
| Android IPC | Android Binder (AIDL) |
| Auth (desktop/iOS) | API key (hashed) in `X-Api-Key` header |
| Auth (Android) | Binder UID identity |
| LAN Discovery | mDNS / UDP broadcast |
| Sync | Vector clock + last-write-wins |
| MVVM | CommunityToolkit.Mvvm |
| DI | Microsoft.Extensions.DependencyInjection |
| Testing | xUnit + in-memory SQLite |

---

## Open Design Decisions

1. **Coins vs. unified points** — Currently kept separate. Decide before implementing `SqlitePointsService` whether to unify into a dynamic pool table or keep as fixed columns.
2. **REST port** — Pick a fixed port (e.g., `7777`) or make it configurable. Should be consistent across devices for sync discovery.
3. **Avalonia on Android** — Avalonia supports Android but may need a thin Android shell project. Confirm rendering approach before Phase 3.5.
4. **Conflict resolution** — Last-write-wins is simple but could cause silent data loss. Consider surfacing conflicts in the REPL for manual resolution in a later iteration.
