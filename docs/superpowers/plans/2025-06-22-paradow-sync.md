# paradow-sync Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Parallel waves use `superpowers:dispatching-parallel-agents`. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a lightweight Windows desktop tool that launches and organizes multiple Dofus Unity clients per team profile, with class-icon overlays and focus management.

**Architecture:** Single-process C# / .NET app split into four libraries (Core, Windows, Automation, App). A coordinator agent defines shared interfaces first, then dispatches parallel agents per module. Integration happens in a final sequential wave.

**Tech Stack:** .NET 8, WinUI 3, Win32 P/Invoke, Windows UI Automation, JSON profile storage, embedded PNG class icons.

**Spec:** `docs/superpowers/specs/2025-06-22-paradow-sync-design.md` (§16 — parallel agent workflow)

---

## Parallel Execution Overview

| Wave | Mode | Agents | Gate |
|------|------|--------|------|
| 0 | Sequential | Spike | Rapport go/no-go |
| 1 | Sequential | Coordinator | `dotnet build` OK, interfaces commitées |
| 2 | **Parallel** | Core · Windows · Assets | `dotnet test` par projet |
| 3 | **Parallel** | Overlay · Automation | `dotnet test` par projet |
| 4 | Sequential | App (integration) | App démarre, tray visible |
| 5 | **Parallel** | Tests intégration · Reviewer | Tous critères MVP §15 |

---

## File Structure (locked)

```
paradow-sync/
├── ParadowSync.sln
├── src/
│   ├── ParadowSync.Core/
│   │   ├── Models/Profile.cs, AccountSlot.cs, AppSettings.cs, SessionState.cs
│   │   ├── Services/IProfileStore.cs, ProfileStore.cs
│   │   ├── Services/IOrchestrator.cs
│   │   ├── Catalog/ClassCatalog.cs
│   │   └── ParadowSync.Core.csproj
│   ├── ParadowSync.Windows/
│   │   ├── Native/Win32.cs, MonitorInfo.cs
│   │   ├── Services/IWindowManager.cs, WindowManager.cs
│   │   ├── Services/IFocusTracker.cs, FocusTracker.cs
│   │   ├── Overlay/IOverlayManager.cs, OverlayManager.cs
│   │   ├── Overlay/TeamStripWindow.cs, WindowBadge.cs
│   │   └── ParadowSync.Windows.csproj
│   ├── ParadowSync.Automation/
│   │   ├── Services/ILauncherService.cs, ZaapLauncherService.cs
│   │   ├── Services/ICharacterSelector.cs, UiAutomationCharacterSelector.cs
│   │   ├── Orchestrator.cs
│   │   └── ParadowSync.Automation.csproj
│   └── ParadowSync.App/
│       ├── App.xaml, MainWindow.xaml, TrayService.cs
│       ├── ViewModels/ProfileListViewModel.cs, ProfileEditorViewModel.cs
│       ├── Services/HotkeyService.cs
│       └── ParadowSync.App.csproj
├── assets/icons/          # 19 PNG 32×32 fan-made
└── tests/
    ├── ParadowSync.Core.Tests/
    ├── ParadowSync.Windows.Tests/
    └── ParadowSync.Automation.Tests/
```

---

## Wave 0 — Technical Spike (Agent Spike, sequential)

**Agent prompt scope:** Validate launcher multi-instance, overlay feasibility, focus hooks. No production code.

### Task 0.1: Spike launcher multi-instance

**Files:**
- Create: `docs/spike/2025-06-22-launcher-findings.md`

- [ ] **Step 1:** Install Dofus Unity + Ankama launcher on Windows test machine
- [ ] **Step 2:** Attempt launching 4 clients with accounts pre-registered in Zaap
- [ ] **Step 3:** Document invocation method (CLI args, shortcuts, protocol URI)
- [ ] **Step 4:** Write findings in `docs/spike/2025-06-22-launcher-findings.md`

**Pass:** 4 concurrent clients running. **Fail:** Document blocker, propose workaround.

### Task 0.2: Spike overlay + focus hook

**Files:**
- Create: `docs/spike/2025-06-22-overlay-findings.md`
- Create: `spike/OverlaySpike/` (throwaway console project, not in solution)

- [ ] **Step 1:** Prototype Win32 layered window over Dofus Unity (click-through)
- [ ] **Step 2:** Prototype `SetWinEventHook(EVENT_SYSTEM_FOREGROUND)` — measure latency
- [ ] **Step 3:** Document if borderless/fullscreen blocks overlay
- [ ] **Step 4:** Write findings in `docs/spike/2025-06-22-overlay-findings.md`

**Pass:** Overlay visible, focus detected < 50ms.

### Task 0.3: Spike UI Automation character select

**Files:**
- Modify: `docs/spike/2025-06-22-launcher-findings.md`

- [ ] **Step 1:** Open Accessibility Insights on Dofus Unity character selection screen
- [ ] **Step 2:** Identify automation tree nodes for character list
- [ ] **Step 3:** Document pass/fail + node names in findings doc

**Pass:** Character clickable via UI Automation. **Fail:** MVP ships with `manual_required` fallback.

---

## Wave 1 — Foundation (Coordinator, sequential)

Defines shared interfaces all parallel agents must respect.

### Task 1.1: Solution scaffolding

**Files:**
- Create: `ParadowSync.sln`, all `.csproj` files, `Directory.Build.props`

- [ ] **Step 1: Create solution**

```bash
cd paradow-sync
dotnet new sln -n ParadowSync
dotnet new classlib -n ParadowSync.Core -o src/ParadowSync.Core -f net8.0
dotnet new classlib -n ParadowSync.Windows -o src/ParadowSync.Windows -f net8.0
dotnet new classlib -n ParadowSync.Automation -o src/ParadowSync.Automation -f net8.0
dotnet new winui -n ParadowSync.App -o src/ParadowSync.App -f net8.0
dotnet new xunit -n ParadowSync.Core.Tests -o tests/ParadowSync.Core.Tests -f net8.0
dotnet new xunit -n ParadowSync.Windows.Tests -o tests/ParadowSync.Windows.Tests -f net8.0
dotnet new xunit -n ParadowSync.Automation.Tests -o tests/ParadowSync.Automation.Tests -f net8.0
dotnet sln add src/ParadowSync.Core src/ParadowSync.Windows src/ParadowSync.Automation src/ParadowSync.App
dotnet sln add tests/ParadowSync.Core.Tests tests/ParadowSync.Windows.Tests tests/ParadowSync.Automation.Tests
```

- [ ] **Step 2: Wire project references**

```
ParadowSync.App        → Core, Windows, Automation
ParadowSync.Automation → Core, Windows
ParadowSync.Windows    → Core
ParadowSync.*.Tests    → respective src project
```

- [ ] **Step 3: Verify build**

```bash
dotnet build
```
Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Commit**

```bash
git add .
git commit -m "chore: scaffold paradow-sync solution"
```

### Task 1.2: Shared domain models (Coordinator)

**Files:**
- Create: `src/ParadowSync.Core/Models/Profile.cs`
- Create: `src/ParadowSync.Core/Models/AccountSlot.cs`
- Create: `src/ParadowSync.Core/Models/AppSettings.cs`
- Create: `src/ParadowSync.Core/Models/SessionState.cs`
- Create: `src/ParadowSync.Core/Models/SlotStatus.cs`

- [ ] **Step 1: Write models**

```csharp
// src/ParadowSync.Core/Models/SlotStatus.cs
namespace ParadowSync.Core.Models;

public enum SlotStatus
{
    Launching,
    SelectingCharacter,
    Ready,
    ManualRequired,
    Error
}
```

```csharp
// src/ParadowSync.Core/Models/AccountSlot.cs
namespace ParadowSync.Core.Models;

public sealed class AccountSlot
{
    public required string AccountId { get; init; }
    public required string Character { get; init; }
    public required string Class { get; init; }
    public int Monitor { get; init; }
    public required WindowSlot Slot { get; init; }
}

public sealed class WindowSlot
{
    public int X { get; init; }
    public int Y { get; init; }
    public int W { get; init; }
    public int H { get; init; }
}
```

```csharp
// src/ParadowSync.Core/Models/Profile.cs
namespace ParadowSync.Core.Models;

public sealed class Profile
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required List<AccountSlot> Accounts { get; init; }
    public OverlayConfig Overlay { get; init; } = new();
    public Dictionary<string, string> Hotkeys { get; init; } = new();
}

public sealed class OverlayConfig
{
    public TeamStripConfig TeamStrip { get; init; } = new();
    public WindowBadgeConfig WindowBadges { get; init; } = new();
}

public sealed class TeamStripConfig
{
    public bool Enabled { get; init; } = true;
    public string Position { get; init; } = "top";
    public int Monitor { get; init; }
}

public sealed class WindowBadgeConfig
{
    public bool Enabled { get; init; } = true;
}
```

```csharp
// src/ParadowSync.Core/Models/AppSettings.cs
namespace ParadowSync.Core.Models;

public sealed class AppSettings
{
    public string LauncherPath { get; init; } = @"C:\Program Files\Ankama\Zaap\zaap.exe";
    public int LaunchDelayMs { get; init; } = 3000;
    public int CharacterSelectTimeoutMs { get; init; } = 30000;
    public double OverlayOpacity { get; init; } = 0.85;
}
```

```csharp
// src/ParadowSync.Core/Models/SessionState.cs
namespace ParadowSync.Core.Models;

public sealed class SessionState
{
    public string? ActiveProfileId { get; set; }
    public List<RuntimeSlot> Slots { get; init; } = [];
    public int? FocusedSlotIndex { get; set; }
}

public sealed class RuntimeSlot
{
    public int AccountIndex { get; init; }
    public nint Hwnd { get; set; }
    public required string Character { get; init; }
    public required string Class { get; init; }
    public SlotStatus Status { get; set; }
}
```

- [ ] **Step 2: Commit**

```bash
git add src/ParadowSync.Core/Models/
git commit -m "feat(core): add shared domain models"
```

### Task 1.3: Shared interfaces (Coordinator — CONTRACT for parallel agents)

**Files:**
- Create: `src/ParadowSync.Core/Services/IProfileStore.cs`
- Create: `src/ParadowSync.Windows/Services/IWindowManager.cs`
- Create: `src/ParadowSync.Windows/Services/IFocusTracker.cs`
- Create: `src/ParadowSync.Windows/Overlay/IOverlayManager.cs`
- Create: `src/ParadowSync.Core/Services/IOrchestrator.cs`
- Create: `src/ParadowSync.Automation/Services/ILauncherService.cs`
- Create: `src/ParadowSync.Automation/Services/ICharacterSelector.cs`

- [ ] **Step 1: Write interfaces**

```csharp
// src/ParadowSync.Core/Services/IProfileStore.cs
namespace ParadowSync.Core.Services;

public interface IProfileStore
{
    Task<IReadOnlyList<Models.Profile>> ListAsync(CancellationToken ct = default);
    Task<Models.Profile?> GetAsync(string id, CancellationToken ct = default);
    Task SaveAsync(Models.Profile profile, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
}
```

```csharp
// src/ParadowSync.Windows/Services/IWindowManager.cs
using ParadowSync.Core.Models;

namespace ParadowSync.Windows.Services;

public interface IWindowManager
{
  IReadOnlyList<MonitorInfo> GetMonitors();
  Task<nint> WaitForGameWindowAsync(string processName, TimeSpan timeout, CancellationToken ct);
  void PlaceWindow(nint hwnd, int monitor, WindowSlot slot);
  void FocusWindow(nint hwnd);
  bool IsWindowValid(nint hwnd);
}

public sealed record MonitorInfo(int Index, int X, int Y, int Width, int Height);
```

```csharp
// src/ParadowSync.Windows/Services/IFocusTracker.cs
namespace ParadowSync.Windows.Services;

public interface IFocusTracker : IDisposable
{
    event EventHandler<nint>? ForegroundChanged;
    void Start();
    void Stop();
}
```

```csharp
// src/ParadowSync.Windows/Overlay/IOverlayManager.cs
using ParadowSync.Core.Models;

namespace ParadowSync.Windows.Overlay;

public interface IOverlayManager : IDisposable
{
    void Show(SessionState session, Profile profile);
    void UpdateFocusedSlot(int slotIndex);
    void Hide();
    void SetVisible(bool visible);
    event EventHandler<int>? SlotClicked;
}
```

```csharp
// src/ParadowSync.Core/Services/IOrchestrator.cs
namespace ParadowSync.Core.Services;

public interface IOrchestrator
{
    SessionState? CurrentSession { get; }
    Task LaunchProfileAsync(Models.Profile profile, CancellationToken ct = default);
    Task StopAllAsync(CancellationToken ct = default);
    Task FocusSlotAsync(int index, CancellationToken ct = default);
}
```

```csharp
// src/ParadowSync.Automation/Services/ILauncherService.cs
namespace ParadowSync.Automation.Services;

public interface ILauncherService
{
    Task LaunchAccountAsync(string accountId, CancellationToken ct = default);
}
```

```csharp
// src/ParadowSync.Automation/Services/ICharacterSelector.cs
namespace ParadowSync.Automation.Services;

public interface ICharacterSelector
{
    Task<bool> SelectCharacterAsync(nint gameHwnd, string characterName, TimeSpan timeout, CancellationToken ct);
}
```

- [ ] **Step 2: Verify build**

```bash
dotnet build
```

- [ ] **Step 3: Commit — GATE for Wave 2**

```bash
git add .
git commit -m "feat: define shared interfaces for parallel agent work"
```

---

## Wave 2 — Parallel (3 agents)

> **Dispatch:** Launch Agents Core, Windows, Assets **in parallel** via `dispatching-parallel-agents`. Each agent receives §16.4 prompt template with its file scope.

---

### Agent Core — Task 2A: ProfileStore + ClassCatalog

**Files:**
- Create: `src/ParadowSync.Core/Services/ProfileStore.cs`
- Create: `src/ParadowSync.Core/Catalog/ClassCatalog.cs`
- Create: `tests/ParadowSync.Core.Tests/ProfileStoreTests.cs`
- Create: `tests/ParadowSync.Core.Tests/ClassCatalogTests.cs`

**Do NOT touch:** `ParadowSync.Windows/`, `ParadowSync.Automation/`, `ParadowSync.App/`

- [ ] **Step 1: Write failing ProfileStore test**

```csharp
// tests/ParadowSync.Core.Tests/ProfileStoreTests.cs
using ParadowSync.Core.Models;
using ParadowSync.Core.Services;

public class ProfileStoreTests : IDisposable
{
    private readonly string _dir;
    private readonly ProfileStore _store;

    public ProfileStoreTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "paradow-sync-test-" + Guid.NewGuid());
        Directory.CreateDirectory(_dir);
        _store = new ProfileStore(_dir);
    }

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Fact]
    public async Task SaveAndGet_roundtrips_profile()
    {
        var profile = new Profile
        {
            Id = "test-1",
            Name = "Team Test",
            Accounts = [new AccountSlot
            {
                AccountId = "acc-1", Character = "Iopette", Class = "Iop",
                Monitor = 0, Slot = new WindowSlot { X = 0, Y = 0, W = 960, H = 540 }
            }]
        };
        await _store.SaveAsync(profile);
        var loaded = await _store.GetAsync("test-1");
        Assert.NotNull(loaded);
        Assert.Equal("Iopette", loaded.Accounts[0].Character);
    }
}
```

- [ ] **Step 2: Run test — expect FAIL**

```bash
dotnet test tests/ParadowSync.Core.Tests --filter SaveAndGet_roundtrips_profile
```

- [ ] **Step 3: Implement ProfileStore**

```csharp
// src/ParadowSync.Core/Services/ProfileStore.cs
using System.Text.Json;
using ParadowSync.Core.Models;

namespace ParadowSync.Core.Services;

public sealed class ProfileStore : IProfileStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _directory;

    public ProfileStore(string? directory = null)
    {
        _directory = directory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "paradow-sync", "profiles");
        Directory.CreateDirectory(_directory);
    }

    public async Task<IReadOnlyList<Profile>> ListAsync(CancellationToken ct = default)
    {
        var profiles = new List<Profile>();
        foreach (var file in Directory.EnumerateFiles(_directory, "*.json"))
        {
            var json = await File.ReadAllTextAsync(file, ct);
            var profile = JsonSerializer.Deserialize<Profile>(json, JsonOptions);
            if (profile is not null) profiles.Add(profile);
        }
        return profiles;
    }

    public async Task<Profile?> GetAsync(string id, CancellationToken ct = default)
    {
        var path = Path.Combine(_directory, $"{id}.json");
        if (!File.Exists(path)) return null;
        var json = await File.ReadAllTextAsync(path, ct);
        return JsonSerializer.Deserialize<Profile>(json, JsonOptions);
    }

    public async Task SaveAsync(Profile profile, CancellationToken ct = default)
    {
        var path = Path.Combine(_directory, $"{profile.Id}.json");
        var json = JsonSerializer.Serialize(profile, JsonOptions);
        await File.WriteAllTextAsync(path, json, ct);
    }

    public Task DeleteAsync(string id, CancellationToken ct = default)
    {
        var path = Path.Combine(_directory, $"{id}.json");
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }
}
```

- [ ] **Step 4: Implement ClassCatalog with 19 classes + colors from spec §7.4**

```csharp
// src/ParadowSync.Core/Catalog/ClassCatalog.cs
namespace ParadowSync.Core.Catalog;

public static class ClassCatalog
{
    private static readonly Dictionary<string, ClassInfo> Classes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Iop"] = new("Iop", "#C62828", "icons/iop.png"),
        ["Eniripsa"] = new("Eniripsa", "#2E7D32", "icons/eniripsa.png"),
        // ... all 19 classes from spec §7.4
    };

    public static ClassInfo? Get(string className) =>
        Classes.TryGetValue(className, out var info) ? info : null;

    public static IReadOnlyList<ClassInfo> All => Classes.Values.ToList();
}

public sealed record ClassInfo(string Name, string ColorHex, string IconPath);
```

- [ ] **Step 5: Run all Core tests — expect PASS**

```bash
dotnet test tests/ParadowSync.Core.Tests
```

- [ ] **Step 6: Commit**

```bash
git add src/ParadowSync.Core/ tests/ParadowSync.Core.Tests/
git commit -m "feat(core): add ProfileStore and ClassCatalog"
```

---

### Agent Windows — Task 2B: WindowManager + FocusTracker

**Files:**
- Create: `src/ParadowSync.Windows/Native/Win32.cs`
- Create: `src/ParadowSync.Windows/Services/WindowManager.cs`
- Create: `src/ParadowSync.Windows/Services/FocusTracker.cs`
- Create: `tests/ParadowSync.Windows.Tests/WindowManagerTests.cs`

**Do NOT touch:** `ParadowSync.Core/` (except already-defined interfaces), `Overlay/`, `ParadowSync.App/`

- [ ] **Step 1: Implement Win32 P/Invoke declarations**

```csharp
// src/ParadowSync.Windows/Native/Win32.cs
using System.Runtime.InteropServices;

namespace ParadowSync.Windows.Native;

internal static class Win32
{
    internal const int GWL_EXSTYLE = -20;
    internal const int WS_EX_LAYERED = 0x80000;
    internal const int WS_EX_TRANSPARENT = 0x20;
    internal const int WS_EX_NOACTIVATE = 0x8000000;
    internal const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
    internal const uint WINEVENT_OUTOFCONTEXT = 0x0000;

    [DllImport("user32.dll")] internal static extern bool SetWindowPos(nint hWnd, nint hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    [DllImport("user32.dll")] internal static extern bool SetForegroundWindow(nint hWnd);
    [DllImport("user32.dll")] internal static extern bool IsWindow(nint hWnd);
    [DllImport("user32.dll")] internal static extern nint SetWinEventHook(uint eventMin, uint eventMax, nint hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);
    [DllImport("user32.dll")] internal static extern bool UnhookWinEvent(nint hWinEventHook);
    [DllImport("user32.dll")] internal static extern nint GetForegroundWindow();

    internal delegate void WinEventDelegate(nint hWinEventHook, uint eventType, nint hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);
}
```

- [ ] **Step 2: Implement WindowManager**

Key methods: `GetMonitors()` via `EnumDisplayMonitors`, `PlaceWindow()` via `SetWindowPos`, `WaitForGameWindowAsync()` polling process windows, `FocusWindow()` via `SetForegroundWindow`.

- [ ] **Step 3: Implement FocusTracker**

Wraps `SetWinEventHook(EVENT_SYSTEM_FOREGROUND)`, raises `ForegroundChanged` with HWND. `IDisposable` calls `UnhookWinEvent`.

- [ ] **Step 4: Write unit tests** (mock-free where possible: `IsWindowValid` with invalid HWND returns false)

- [ ] **Step 5: Run tests**

```bash
dotnet test tests/ParadowSync.Windows.Tests
```

- [ ] **Step 6: Commit**

```bash
git add src/ParadowSync.Windows/ tests/ParadowSync.Windows.Tests/
git commit -m "feat(windows): add WindowManager and FocusTracker"
```

---

### Agent Assets — Task 2C: Class icon pack

**Files:**
- Create: `assets/icons/*.png` (19 fan-made icons, 32×32)
- Modify: `src/ParadowSync.Core/Catalog/ClassCatalog.cs` (complete all 19 entries)
- Create: `src/ParadowSync.Core/Catalog/IconLoader.cs`

**Do NOT touch:** Windows, Automation, App code.

- [ ] **Step 1:** Source or create 19 fan-made Dofus-style class icons (32×32 PNG)
- [ ] **Step 2:** Place in `assets/icons/` with lowercase names (`iop.png`, `eniripsa.png`, …)
- [ ] **Step 3:** Implement `IconLoader` that loads PNG into `byte[]` cache at startup
- [ ] **Step 4:** Complete `ClassCatalog` with all 19 classes + colors from spec §7.4
- [ ] **Step 5: Commit**

```bash
git add assets/ src/ParadowSync.Core/Catalog/
git commit -m "feat(assets): add fan-made class icons and IconLoader"
```

---

### Wave 2 Integration (Coordinator)

- [ ] **Step 1:** Merge all agent branches if using separate worktrees
- [ ] **Step 2:** `dotnet build && dotnet test`
- [ ] **Step 3:** Dispatch spec reviewer on Core + Windows + Assets

---

## Wave 3 — Parallel (2 agents)

---

### Agent Overlay — Task 3A: OverlayManager

**Depends on:** `IWindowManager`, `IFocusTracker`, `ClassCatalog`, `IconLoader`

**Files:**
- Create: `src/ParadowSync.Windows/Overlay/OverlayManager.cs`
- Create: `src/ParadowSync.Windows/Overlay/TeamStripWindow.cs`
- Create: `src/ParadowSync.Windows/Overlay/WindowBadge.cs`
- Create: `tests/ParadowSync.Windows.Tests/OverlayManagerTests.cs`

- [ ] **Step 1: Write failing test for focus state mapping**

```csharp
[Fact]
public void UpdateFocusedSlot_changes_active_index()
{
    var overlay = new OverlayManager(/* mocked deps */);
    overlay.UpdateFocusedSlot(2);
    Assert.Equal(2, overlay.ActiveSlotIndex);
}
```

- [ ] **Step 2: Implement TeamStripWindow**
  - Win32 layered window, `WS_EX_NOACTIVATE`
  - Renders class icons from `IconLoader` cache
  - Active slot: class-color border, 36×36 icon
  - Click handler on icons → raises `SlotClicked`
  - Repaint **only** on `UpdateFocusedSlot` or `Show`

- [ ] **Step 3: Implement WindowBadge**
  - Small layered window anchored to game HWND top-left
  - 16×16 icon + character name, click-through
  - Active game window: 2px class-color border via separate border window

- [ ] **Step 4: Implement OverlayManager**
  - `Show()`: create strip + badges for all ready slots
  - `UpdateFocusedSlot()`: update strip highlight + window borders (single repaint)
  - `Hide()` / `Dispose()`: destroy all overlay HWNDs
  - Subscribe to `IFocusTracker.ForegroundChanged` → map HWND to slot index

- [ ] **Step 5: Run tests + commit**

```bash
dotnet test tests/ParadowSync.Windows.Tests
git commit -m "feat(overlay): add team strip, badges, and focus indicators"
```

---

### Agent Automation — Task 3B: Launcher + CharacterSelector + Orchestrator

**Depends on:** `IWindowManager`, spike findings from Wave 0

**Files:**
- Create: `src/ParadowSync.Automation/Services/ZaapLauncherService.cs`
- Create: `src/ParadowSync.Automation/Services/UiAutomationCharacterSelector.cs`
- Create: `src/ParadowSync.Automation/Orchestrator.cs`
- Create: `tests/ParadowSync.Automation.Tests/OrchestratorTests.cs`

- [ ] **Step 1: Implement ZaapLauncherService** based on spike findings (process start with account ref)
- [ ] **Step 2: Implement UiAutomationCharacterSelector** using `System.Windows.Automation`
- [ ] **Step 3: Implement Orchestrator**

```csharp
// Launch flow per account:
// 1. launcher.LaunchAccountAsync(accountId)
// 2. hwnd = windowManager.WaitForGameWindowAsync("Dofus", timeout)
// 3. success = characterSelector.SelectCharacterAsync(hwnd, character, timeout)
// 4. slot.Status = success ? Ready : ManualRequired
// 5. windowManager.PlaceWindow(hwnd, monitor, slot)
// 6. delay launchDelayMs before next account
```

- [ ] **Step 4: Write Orchestrator tests with mocked ILauncherService, IWindowManager, ICharacterSelector
- [ ] **Step 5: Run tests + commit**

```bash
dotnet test tests/ParadowSync.Automation.Tests
git commit -m "feat(automation): add launcher, character selector, orchestrator"
```

---

### Wave 3 Integration (Coordinator)

- [ ] `dotnet build && dotnet test`
- [ ] Spec reviewer on Overlay + Automation
- [ ] Verify Overlay only repaints on events (no timer loops in code review)

---

## Wave 4 — Integration (Agent App, sequential)

### Task 4.1: Tray + HotkeyService

**Files:**
- Create: `src/ParadowSync.App/Services/TrayService.cs`
- Create: `src/ParadowSync.App/Services/HotkeyService.cs`

- [ ] **Step 1:** Tray icon with menu: profiles, launch/stop, toggle overlay, quit
- [ ] **Step 2:** Global hotkeys: `Ctrl+1..8` focus slots, `Ctrl+Shift+O` toggle overlay, `Ctrl+Shift+Q` stop all
- [ ] **Step 3:** Wire to `IOrchestrator` and `IOverlayManager`

### Task 4.2: Profile list + editor UI

**Files:**
- Create: `src/ParadowSync.App/MainWindow.xaml`
- Create: `src/ParadowSync.App/ViewModels/ProfileListViewModel.cs`
- Create: `src/ParadowSync.App/ViewModels/ProfileEditorViewModel.cs`

- [ ] **Step 1:** Profile list view (create, edit, delete, launch)
- [ ] **Step 2:** Profile editor: account slots, class dropdown (from ClassCatalog), monitor selector
- [ ] **Step 3:** Layout grid editor (visual slot positioning)
- [ ] **Step 4:** Overlay config (strip position, badge toggle)
- [ ] **Step 5:** Hotkey config per profile

### Task 4.3: DI wiring + session lifecycle

**Files:**
- Modify: `src/ParadowSync.App/App.xaml.cs`

- [ ] **Step 1:** Register all services (DI container or manual wiring)
- [ ] **Step 2:** Launch profile → orchestrator → overlay show → focus tracker start
- [ ] **Step 3:** Stop all → overlay hide → focus tracker stop → close HWNDs
- [ ] **Step 4:** HWND validity check every 5s for crashed clients
- [ ] **Step 5: Commit**

```bash
git commit -m "feat(app): add UI, tray, hotkeys, and session lifecycle"
```

---

## Wave 5 — Parallel (Tests + Reviewer)

### Agent Tests — Task 5A: Integration smoke tests

- [ ] Profile roundtrip: create → save → load → launch (mocked)
- [ ] Focus switching: hotkey → correct HWND focused
- [ ] Overlay toggle: hidden → no HWNDs, visible → HWNDs created
- [ ] Manual test checklist document: `docs/manual-test-checklist.md`

### Agent Reviewer — Task 5B: Final review

- [ ] Verify all MVP acceptance criteria (spec §15)
- [ ] Verify performance rules: no polling loops, no game injection
- [ ] Verify no passwords in config/logs
- [ ] Performance manual test: CPU < 0.1% with 4 clients

---

## Agent Dispatch Cheat Sheet

```markdown
# Wave 2 — dispatch simultaneously:

Agent "Core":
  Mission: Task 2A — ProfileStore + ClassCatalog + tests
  Allowed: src/ParadowSync.Core/, tests/ParadowSync.Core.Tests/
  Forbidden: everything else

Agent "Windows":
  Mission: Task 2B — WindowManager + FocusTracker + tests
  Allowed: src/ParadowSync.Windows/ (except Overlay/), tests/ParadowSync.Windows.Tests/
  Forbidden: everything else

Agent "Assets":
  Mission: Task 2C — icon pack + ClassCatalog completion
  Allowed: assets/, src/ParadowSync.Core/Catalog/
  Forbidden: everything else

# Wave 3 — dispatch simultaneously:

Agent "Overlay":
  Mission: Task 3A — OverlayManager + TeamStrip + Badges
  Allowed: src/ParadowSync.Windows/Overlay/, related tests
  Forbidden: Automation/, App/

Agent "Automation":
  Mission: Task 3B — Launcher + CharacterSelector + Orchestrator
  Allowed: src/ParadowSync.Automation/, related tests
  Forbidden: Overlay/, App/
```

---

## Spec Coverage Checklist

| Spec § | Task |
|--------|------|
| §5 Data model | Task 1.2, 2A |
| §6 Launch flow | Task 3B, 4.3 |
| §6 Focus switch | Task 2B, 3A, 4.1 |
| §7 Overlay + icons | Task 2C, 3A |
| §8 Window management | Task 2B |
| §9 UI | Task 4.2 |
| §10 Error handling | Task 3B, 4.3 |
| §12 Spike | Wave 0 |
| §15 Acceptance criteria | Wave 5 |
| §16 Parallel agents | This plan |
