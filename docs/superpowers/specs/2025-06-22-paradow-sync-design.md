# paradow-sync — Design Specification

**Date:** 2025-06-22  
**Status:** Draft — pending user review  
**Platform:** Windows natif  
**Game:** Dofus Unity

## 1. Problem Statement

Playing Dofus Unity with multiple accounts on a single Windows machine requires manually launching clients, arranging windows, remembering which character is in which window, and switching focus between clients. This is tedious and error-prone during duo or team play.

**paradow-sync** is a lightweight desktop tool that orchestrates multi-account sessions via the Ankama launcher, organizes game windows across monitors, and provides visual overlays so the player always knows which character is active.

## 2. Goals (MVP)

| Goal | Description |
|------|-------------|
| Profile-based launch | Launch a variable number of accounts per team profile |
| Window organization | Auto-layout, multi-monitor support, position persistence |
| Hotkeys | Quick focus switching between clients |
| Launcher integration | Orchestrate via Ankama launcher without storing passwords |
| Character selection | Auto-select configured character when technically feasible |
| Team overlay | Class icons, active character indicator, per-window badges |
| Lightweight | No measurable impact on game performance |

## 3. Non-Goals (Post-MVP)

- Real-time in-game stats (HP, AP, MP)
- Combat automation or action synchronization
- In-game 3D overlays or memory reading
- Password storage or credential management
- Linux/macOS support
- Automatic class detection from the game client

## 4. Architecture

Single-process C# / .NET desktop application with four internal modules:

```
┌──────────────────────────────────────────────────┐
│  UI Layer (WinUI 3 or WPF)                       │
│  Profile editor · Layout grid · Overlay config   │
│  Hotkey configuration                            │
├──────────────────────────────────────────────────┤
│  Tray Service                                    │
│  Quick launch/stop · Profile menu · Hotkeys      │
├──────────────────────────────────────────────────┤
│  Orchestrator                                    │
│  Ankama launcher integration                     │
│  UI Automation for character selection           │
├──────────────────────────────────────────────────┤
│  Window Manager (Win32 P/Invoke)                 │
│  Placement · Multi-monitor · HWND tracking       │
│  Position persistence                            │
├──────────────────────────────────────────────────┤
│  Overlay Manager                                 │
│  Team strip · Window badges · Focus detection    │
│  WinEventHook (event-driven, zero polling)       │
└──────────────────────────────────────────────────┘
```

### 4.1 Technology Stack

| Component | Choice | Rationale |
|-----------|--------|-----------|
| Runtime | .NET 8+ | Modern, well-supported on Windows |
| UI framework | WinUI 3 (preferred) or WPF | Native Windows, tray support, good Win32 interop |
| Window management | Win32 P/Invoke | Direct, lightweight monitor/window APIs |
| Focus detection | `SetWinEventHook(EVENT_SYSTEM_FOREGROUND)` | Zero CPU polling |
| Overlay rendering | Win32 layered windows (`WS_EX_LAYERED`, `WS_EX_TRANSPARENT`, `WS_EX_NOACTIVATE`) | Lightest option, no game injection |
| Character automation | Windows UI Automation | Non-invasive, no memory reading |
| Profile storage | JSON files in `%APPDATA%/paradow-sync/` | Simple, human-readable, no database needed |
| Icons | Embedded PNG 32×32 fan-made Dofus-style class icons | Loaded once at startup (~200 KB total) |

### 4.2 Performance Budget

| Metric | Target |
|--------|--------|
| CPU (in-game) | < 0.1% — event-driven only, no render loops |
| Memory (overlay module) | < 10 MB |
| Disk I/O (in-game) | 0 — all assets cached in RAM at launch |
| Game injection | None — no hooks into Dofus process |
| GPU capture | None — no DXGI/Desktop Duplication |

## 5. Data Model

### 5.1 Profile

```json
{
  "id": "uuid",
  "name": "Team Donjon",
  "accounts": [
    {
      "accountId": "ankama-account-ref",
      "character": "Iopette",
      "class": "Iop",
      "monitor": 0,
      "slot": { "x": 0, "y": 0, "w": 960, "h": 540 }
    },
    {
      "accountId": "ankama-account-ref-2",
      "character": "Eniripsa",
      "class": "Eniripsa",
      "monitor": 0,
      "slot": { "x": 960, "y": 0, "w": 960, "h": 540 }
    }
  ],
  "overlay": {
    "teamStrip": {
      "enabled": true,
      "position": "top",
      "monitor": 0
    },
    "windowBadges": {
      "enabled": true
    }
  },
  "hotkeys": {
    "focusSlot1": "Ctrl+1",
    "focusSlot2": "Ctrl+2",
    "focusSlot3": "Ctrl+3",
    "focusSlot4": "Ctrl+4",
    "toggleOverlay": "Ctrl+Shift+O",
    "stopAll": "Ctrl+Shift+Q"
  }
}
```

### 5.2 Field Definitions

| Field | Required | Description |
|-------|----------|-------------|
| `accountId` | Yes | Reference to an Ankama account registered in the launcher (not a password) |
| `character` | Yes | Character name to auto-select on login screen |
| `class` | Yes | Dofus class name — resolves icon and accent color |
| `monitor` | Yes | Monitor index (0-based) for window placement |
| `slot` | Yes | Window position and size `{x, y, w, h}` relative to target monitor |
| `class` values | — | One of the 19 Dofus Unity classes (see §7.4 color table) |

### 5.3 App Settings

```json
{
  "launcherPath": "C:\\Program Files\\Ankama\\Zaap\\zaap.exe",
  "launchDelayMs": 3000,
  "characterSelectTimeoutMs": 30000,
  "overlayOpacity": 0.85
}
```

### 5.4 Runtime State (in-memory, not persisted)

```json
{
  "activeProfileId": "uuid",
  "slots": [
    {
      "accountIndex": 0,
      "hwnd": 12345678,
      "character": "Iopette",
      "class": "Iop",
      "status": "ready"
    }
  ],
  "focusedSlotIndex": 0
}
```

Slot `status` values: `launching` | `selecting_character` | `ready` | `manual_required` | `error`

## 6. Core Flows

### 6.1 Launch Profile

```
User clicks "Launch" (UI or tray)
  → Orchestrator reads profile accounts sequentially
  → For each account:
      1. Trigger Ankama launcher for accountId
      2. Wait for Dofus Unity window (HWND detection, timeout 60s)
      3. Attempt UI Automation character selection
         → Success: status = ready
         → Failure: status = manual_required, notify user
      4. Register HWND ↔ slot mapping
      5. Wait launchDelayMs before next account
  → Window Manager places all ready windows per profile layout
  → Overlay Manager creates team strip + window badges
  → WinEventHook starts listening for focus changes
  → First ready window receives focus
```

### 6.2 Focus Switch

```
Trigger: hotkey (Ctrl+N) OR click on team strip icon
  → Lookup slot N in runtime state
  → Win32 SetForegroundWindow(hwnd)
  → Overlay Manager updates active indicator (single repaint)
```

### 6.3 Stop Profile

```
User clicks "Stop All" (UI, tray, or hotkey)
  → Overlay Manager destroys all overlay windows
  → Unhook WinEventHook
  → Close all registered game HWNDs (or prompt user)
  → Clear runtime state
```

### 6.4 Window Focus Change (automatic)

```
WinEventHook fires EVENT_SYSTEM_FOREGROUND
  → Match HWND to slot in runtime state
  → Overlay Manager:
      - Highlight matching icon in team strip
      - Apply class-color border to active window
      - Remove border from previously active window
  → Single repaint, no continuous loop
```

## 7. Overlay Design

### 7.1 Team Strip

A thin horizontal bar fixed at the top or bottom of a configured monitor.

- **Per slot:** fan-made class icon (32×32 inactive, 36×36 active)
- **Active state:** class-color border glow around icon
- **Tooltip:** character name + class name on hover
- **Click:** focuses the corresponding game window (only interactive overlay element)
- **Toggle:** `Ctrl+Shift+O` or tray menu

```
┌──────────────────────────────────────────────────────────┐
│  [Iop]   [Eni]   [Panda]   [Sacri]                     │
│   ██      ░░      ░░░       ░░░    ← active = highlighted│
└──────────────────────────────────────────────────────────┘
```

### 7.2 Window Badge

A small label in the top-left corner of each game window.

- **Content:** class icon (16×16) + character name
- **Background:** semi-transparent dark panel
- **Click-through:** yes (`WS_EX_TRANSPARENT`) — does not block game input
- **Active window:** 2–3 px class-color border around entire game window

### 7.3 Class Icons

- Fan-made Dofus-style icons, one per class (19 classes)
- Embedded as PNG resources in the application
- Loaded into memory at startup, never re-read from disk during play
- Mapping: `class` field in profile → icon resource key (e.g. `"Iop"` → `icons/iop.png`)
- Fallback: generic question-mark icon for unknown classes

### 7.4 Class Colors

Each class has a fixed accent color used for borders and active highlights. Colors are hardcoded constants, not read from the game.

| Class | Color (hex) |
|-------|-------------|
| Feca | `#1565C0` |
| Osamodas | `#6A1B9A` |
| Enutrof | `#F9A825` |
| Sram | `#4A148C` |
| Xelor | `#00838F` |
| Ecaflip | `#AD1457` |
| Eniripsa | `#2E7D32` |
| Iop | `#C62828` |
| Cra | `#33691E` |
| Sadida | `#558B2F` |
| Sacrieur | `#B71C1C` |
| Pandawa | `#0277BD` |
| Roublard | `#4E342E` |
| Zobal | `#880E4F` |
| Steamer | `#455A64` |
| Eliotrope | `#283593` |
| Huppermage | `#E65100` |
| Ouginak | `#BF360C` |
| Forgelance | `#37474F` |

## 8. Window Management

### 8.1 Layout

- Slots define `{x, y, w, h}` relative to the target monitor's work area
- On launch: windows are resized and positioned to match slot definitions
- On profile stop + relaunch: positions restored from profile (not from last drag)

### 8.2 Multi-Monitor

- Monitor index is 0-based, queried via `EnumDisplayMonitors`
- Profile editor shows available monitors for slot assignment
- Team strip monitor is independently configurable

### 8.3 Persistence

- Window positions are defined in the profile (user-configured via layout editor)
- Runtime HWND mappings are ephemeral (rebuilt each launch)
- No automatic "remember last dragged position" in MVP

## 9. UI Design

### 9.1 Main Window

- **Profile list:** create, edit, duplicate, delete profiles
- **Profile editor:**
  - Account slots (add/remove, configure account + character + class)
  - Visual layout grid with drag-and-drop slot positioning
  - Monitor selector per slot
  - Overlay settings (team strip position, badge toggle)
  - Hotkey configuration
- **Launch / Stop buttons**

### 9.2 Tray Icon

- Right-click menu: profile list with Launch/Stop, toggle overlay, settings, quit
- Left-click: show/hide main window
- Status indicator: green = running, grey = idle

## 10. Error Handling

| Scenario | Behavior |
|----------|----------|
| Launcher not found | Block launch, show settings prompt to set `launcherPath` |
| Game window timeout (60s) | Mark slot `error`, continue with remaining accounts, notify user |
| Character auto-select fails | Mark slot `manual_required`, show toast notification, user selects manually |
| HWND lost (game crash) | Detect via periodic HWND validity check (every 5s, low cost), mark slot `error`, update overlay |
| Focus hook fails | Log warning, overlay still works via hotkey/manual click only |
| Profile JSON corrupt | Skip profile, show error in profile list |

## 11. Security & Compliance

- **No password storage.** Accounts are referenced by Ankama launcher identity only.
- **No game memory reading.** Class and character info come from user configuration.
- **No game process injection.** All interaction is external (Win32 + UI Automation).
- **UI Automation scope:** limited to launcher login screen and character selection screen only. No in-game automation.
- **CGU awareness:** window management and external overlays are generally tolerated. Character auto-selection via UI Automation is low-risk but should be documented as a best-effort feature with manual fallback.

## 12. Phase 0 — Technical Spike (Pre-Implementation)

Before building the full application, validate these unknowns in 1–2 days:

| Question | Method | Pass Criteria |
|----------|--------|---------------|
| Can Ankama launcher launch N Dofus Unity instances? | Manual test + script | 4 concurrent clients running |
| Is launcher invocation scriptable? | CLI args, zaap API, shortcut inspection | Programmatic launch per account |
| Is character selection screen accessible via UI Automation? | Inspect with Accessibility Insights | Tree nodes identifiable for click |
| Do layered overlay windows work over Dofus Unity (fullscreen/borderless)? | Win32 prototype | Overlay visible, click-through functional |
| Focus hook reliability | `SetWinEventHook` prototype | Focus changes detected < 50ms |

**Gate:** MVP implementation starts only after spike passes criteria 1, 2, 4, and 5. Criterion 3 determines whether character auto-select ships in MVP or launches with manual fallback only. Overlays (criterion 4) are required for MVP — if click-through fails over the game window, fallback is a screen-edge team strip outside the game window bounds.

## 13. Testing Strategy

| Layer | Approach |
|-------|----------|
| Profile serialization | Unit tests for JSON read/write/validation |
| Window Manager | Unit tests with mocked Win32 (where possible) |
| Overlay Manager | Unit tests for slot↔HWND mapping and focus state |
| Orchestrator | Integration tests with mock launcher (no real game in CI) |
| UI Automation | Manual test checklist against real Dofus Unity login screen |
| Performance | Manual: verify CPU < 0.1% with 4 clients running via Task Manager |
| End-to-end | Manual test script: launch profile → verify windows, overlay, hotkeys |

## 14. Project Structure (Proposed)

```
paradow-sync/
├── src/
│   ├── ParadowSync.App/          # WinUI/WPF entry, tray, main window
│   ├── ParadowSync.Core/         # Profiles, settings, orchestration logic
│   ├── ParadowSync.Windows/      # Win32 window manager, overlay, hooks
│   └── ParadowSync.Automation/   # UI Automation for launcher/character select
├── assets/
│   └── icons/                    # Fan-made class icons (PNG 32×32)
├── docs/
│   └── superpowers/specs/        # This document
└── tests/
    ├── ParadowSync.Core.Tests/
    └── ParadowSync.Windows.Tests/
```

## 15. MVP Acceptance Criteria

- [ ] Create and save a profile with 2–8 accounts, each with account ref, character, class, monitor, and slot position
- [ ] Launch profile: all clients start via Ankama launcher
- [ ] Character auto-selection works for at least 1 account (with manual fallback for others)
- [ ] Windows placed according to profile layout on correct monitors
- [ ] Team strip shows class icons for all active accounts
- [ ] Active account highlighted in team strip and with class-color window border
- [ ] Window badges show class icon + character name on each client
- [ ] Click team strip icon → focuses corresponding window
- [ ] Hotkeys `Ctrl+1..N` focus corresponding windows
- [ ] `Ctrl+Shift+O` toggles overlay visibility
- [ ] Stop all closes clients and removes overlays
- [ ] Tray icon provides quick launch/stop
- [ ] CPU usage < 0.1% during normal play with 4 clients
- [ ] No passwords stored anywhere in config or logs

## 16. Development Workflow — Parallel Agent Team

Le développement du MVP suit une approche **multi-agents parallèles** orchestrée par un agent coordinateur (session principale). Chaque agent travaille dans un domaine isolé avec un contexte auto-suffisant — il n'hérite pas de l'historique de chat.

### 16.1 Rôles

| Rôle | Responsabilité |
|------|----------------|
| **Coordinateur** | Décompose le plan, définit les interfaces partagées, lance les agents en parallèle, intègre les livrables, résout les conflits |
| **Agent Spike** | Phase 0 — valide faisabilité launcher, overlays, UI Automation (séquentiel, bloquant) |
| **Agent Core** | Modèles, sérialisation JSON, settings, interfaces publiques |
| **Agent Windows** | Win32 window manager, focus hooks, enum moniteurs |
| **Agent Overlay** | Team strip, badges, rendu layered windows, icônes de classe |
| **Agent Automation** | Intégration launcher Ankama, UI Automation sélection perso |
| **Agent App** | WinUI/WPF, tray, éditeur de profils, hotkeys, câblage DI |
| **Agent Tests** | Tests unitaires par module (peut tourner en parallèle une fois les interfaces figées) |
| **Reviewer** | Revue spec + qualité après chaque vague (subagent-driven-development) |

### 16.2 Vagues d'exécution parallèle

```
Vague 0 (séquentielle, gate)
  └── Agent Spike → rapport go/no-go

Vague 1 (séquentielle, fondation)
  └── Coordinateur → solution .NET, projets, interfaces partagées (IWindowManager, IOverlayManager, IOrchestrator, Profile models)

Vague 2 (parallèle — 3 agents)
  ├── Agent Core      → ParadowSync.Core + tests
  ├── Agent Windows   → ParadowSync.Windows (sans overlay) + tests
  └── Agent Assets    → Pack icônes fan-made + ClassCatalog (couleurs, mapping)

Vague 3 (parallèle — 2 agents, dépend de Vague 2)
  ├── Agent Overlay     → Overlay Manager (consomme IWindowManager + ClassCatalog)
  └── Agent Automation  → Launcher + UI Automation (consomme Profile models)

Vague 4 (séquentielle, intégration)
  └── Agent App → UI, tray, orchestration, branchement modules

Vague 5 (parallèle — 2 agents)
  ├── Agent Tests     → tests d'intégration, mocks
  └── Reviewer        → revue finale spec + perf
```

### 16.3 Règles de parallélisation

**Peut tourner en parallèle quand :**
- Domaines indépendants (Core ≠ Windows ≠ Assets)
- Interfaces partagées déjà définies et commitées (Vague 1)
- Pas de modification des mêmes fichiers entre agents

**Doit rester séquentiel quand :**
- Spike (résultat conditionne le scope automation)
- Scaffolding + interfaces (prérequis de toutes les vagues)
- Intégration App (touche tous les modules)
- Un agent dépend du livrable d'un autre (Overlay → Windows, App → tout)

### 16.4 Contrat agent (prompt type)

Chaque agent reçoit un prompt auto-suffisant :

```markdown
## Mission
[Objectif précis — un seul module]

## Contexte
- Spec: docs/superpowers/specs/2025-06-22-paradow-sync-design.md (sections X, Y)
- Plan: docs/superpowers/plans/2025-06-22-paradow-sync.md (Tâches N–M)

## Interfaces à respecter
[Copie exacte des interfaces C# définies en Vague 1]

## Fichiers autorisés
- Create/Modify: [liste explicite]
- Do NOT touch: [liste explicite]

## Livrable attendu
- Code + tests passants pour ce module
- Résumé: ce qui est fait, ce qui est bloqué, écarts vs spec

## Contraintes
- Pas de stockage MDP
- Overlay event-driven, pas de polling
- Windows uniquement
```

### 16.5 Intégration post-parallèle

Après chaque vague parallèle, le coordinateur :

1. Lit le résumé de chaque agent
2. Vérifie les conflits git (mêmes fichiers modifiés)
3. Lance `dotnet build` + `dotnet test` sur la solution complète
4. Dispatch un reviewer (spec compliance → code quality)
5. Corrige les conflits avant la vague suivante

### 16.6 Skills associés

| Phase | Skill |
|-------|-------|
| Planification | `superpowers:writing-plans` |
| Exécution par tâche | `superpowers:subagent-driven-development` |
| Dispatch parallèle | `superpowers:dispatching-parallel-agents` |
| Fin de branche | `superpowers:finishing-a-development-branch` |
