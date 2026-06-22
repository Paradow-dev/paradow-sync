# Handoff — paradow-sync

**Date :** 2025-06-22  
**Repo :** https://github.com/Paradow-dev/paradow-sync  
**Branche par défaut :** `main` (MVP complet)  
**Statut :** MVP implémenté — validation Windows requise avant release

---

## 1. Contexte produit

**paradow-sync** est un outil Windows pour jouer à **Dofus Unity** en duo/multi-compte. Il permet de :

- Lancer N clients via le **launcher Ankama (Zaap)** sans stocker de mots de passe
- Organiser les fenêtres sur un ou plusieurs écrans (profils d'équipe)
- Afficher des **overlays légers** (bandeau d'équipe + badges) avec **icônes de classe**
- Basculer rapidement entre personnages (`Ctrl+1..8`, clic sur icône, tray)

**Hors scope MVP :** stats in-game (PV/PA), automation combat, sync d'actions, Linux/macOS.

---

## 2. État d'avancement

### Fait (vagues 0–4)

| Vague | Livrable | Commit(s) |
|-------|----------|-----------|
| 0 | Templates spike + prototype `spike/OverlaySpike/` | `4f45e1b` |
| 1 | Solution .NET, modèles, interfaces partagées | `956b258` |
| 2 | ProfileStore, ClassCatalog, IconLoader, WindowManager, FocusTracker, icônes placeholder | `e36a361`, `e8f90fd`, `fc5f544` |
| 3 | OverlayManager (team strip + badges), Orchestrator, Zaap launcher, UI Automation | `b3b05ff`, `2a16998` |
| 4 | App WinUI, tray Win32, hotkeys globaux, éditeur profils, lifecycle session | `1484919` |
| — | README PowerShell + push GitHub public | `94d1d1a` |

### Non fait / en attente (vague 5)

- [ ] **Spike technique validé sur Windows** (`docs/spike/` — templates vides)
- [ ] **Build complet WinUI** sur Windows (`ParadowSync.App` ne compile pas sous Linux/WSL)
- [ ] **Tests Windows/Automation** (13 tests Core passent sur WSL ; reste nécessite `Microsoft.WindowsDesktop.App`)
- [ ] Revue finale spec + tests manuels MVP (`docs/manual-test-checklist.md`)
- [ ] Remplacement icônes placeholder par fan-made Dofus (`assets/icons/`)
- [ ] Ajustement CLI Zaap après spike (`ZaapLauncherService` — args placeholder)
- [ ] Tuning UI Automation sélection personnage (heuristique, à valider in-game)

---

## 3. Architecture

```
ParadowSync.App          → WinUI 3, tray, hotkeys, UI profils
    ├── ParadowSync.Automation → Orchestrator, ZaapLauncher, UiAutomationCharacterSelector
    ├── ParadowSync.Windows    → WindowManager, FocusTracker, OverlayManager
    └── ParadowSync.Core       → ProfileStore, ClassCatalog, IconLoader, modèles
```

### Flux de lancement

```
[Lancer profil]
  → ZaapLauncherService (1 process par compte)
  → WindowManager.WaitForGameWindowAsync("Dofus")
  → UiAutomationCharacterSelector (best-effort → ManualRequired si échec)
  → WindowManager.PlaceWindow (grille + moniteur)
  → OverlayManager.Show (team strip + badges)
  → FocusTracker.Start (WinEventHook, event-driven)
```

### Contraintes performance overlay

- Repaint **uniquement** sur changement de focus / Show / Hide — pas de timer de rendu
- Exception : timer 5s pour validation HWND (crash client) — acceptable

---

## 4. Environnements

| Env | Rôle | Limites connues |
|-----|------|-----------------|
| **WSL2 / Linux** | Dev Core, docs, git | Pas de build WinUI, pas de tests Win32 |
| **Windows natif** | Build, run, spike, tests manuels | Environnement cible obligatoire |

---

## 5. Démarrage rapide (PowerShell Windows)

```powershell
# Prérequis
winget install Microsoft.DotNet.SDK.8
# Visual Studio 2022 + workload "Développement WinUI" (recommandé)

git clone https://github.com/Paradow-dev/paradow-sync.git
cd paradow-sync
dotnet build
dotnet test
dotnet run --project src/ParadowSync.App
```

### Spike (à faire en premier sur Windows)

```powershell
dotnet run --project spike/OverlaySpike
```

Remplir :
- `docs/spike/2025-06-22-launcher-findings.md`
- `docs/spike/2025-06-22-overlay-findings.md`

**Gate MVP :** critères 1, 2, 4, 5 du spike doivent passer. Critère 3 (UI Automation) détermine si l'auto-sélection personnage est fiable ou fallback manuel.

---

## 6. Fichiers clés

| Fichier | Rôle |
|---------|------|
| `src/ParadowSync.App/App.xaml.cs` | DI + wiring session (launch/stop/timer HWND) |
| `src/ParadowSync.App/Services/AppSessionService.cs` | Orchestration lifecycle |
| `src/ParadowSync.Automation/Orchestrator.cs` | Séquence lancement multi-comptes |
| `src/ParadowSync.Automation/Services/ZaapLauncherService.cs` | **⚠ Args Zaap placeholder** |
| `src/ParadowSync.Windows/Overlay/OverlayManager.cs` | Team strip + badges |
| `src/ParadowSync.Core/Services/ProfileStore.cs` | Persistance `%AppData%/paradow-sync/profiles/` |
| `assets/icons/*.png` | Placeholders — à remplacer |

---

## 7. Points d'attention / dette connue

1. **Zaap CLI non validé** — `ZaapLauncherService` utilise `--game dofus-unity --account {id}` (hypothèse). Ajuster après spike.
2. **UI Automation fragile** — l'arbre Dofus Unity peut changer ; fallback `ManualRequired` prévu.
3. **Hotkeys hardcodés** — champs hotkey dans profil JSON informatifs seulement ; pas de rebind dynamique MVP.
4. **Éditeur layout** — champs x/y/w/h texte, pas de grille drag-and-drop visuelle.
5. **StopAllAsync** — efface l'état session ; ne tue pas forcément les processus Dofus.
6. **Icônes** — placeholders générés (`scripts/generate-placeholder-icons.py`), pas du fan-art Dofus.

---

## 8. Prochaines tâches recommandées (ordre)

1. **Spike Windows** → remplir `docs/spike/*.md`, go/no-go
2. **`dotnet build` + `dotnet test`** sur Windows — corriger erreurs WinUI/XAML
3. **Test manuel** → `docs/manual-test-checklist.md`
4. Ajuster `ZaapLauncherService` selon findings spike
5. Tuner `UiAutomationCharacterSelector` sur écran sélection perso réel
6. Remplacer icônes placeholder
7. Vague 5 : revue spec + perf CPU (< 0,1 % avec 4 clients)

---

## 9. Documentation de référence

| Doc | Chemin |
|-----|--------|
| Spec design | `docs/superpowers/specs/2025-06-22-paradow-sync-design.md` |
| Plan implémentation | `docs/superpowers/plans/2025-06-22-paradow-sync.md` |
| Workflow agents parallèles | Spec §16 |
| Checklist tests manuels | `docs/manual-test-checklist.md` |
| Procédure spike | `docs/spike/README.md` |

---

## 10. Workflow dev (agents parallèles)

Le projet a été développé en **Subagent-Driven** avec vagues parallèles :

- **Vague 1** séquentielle : interfaces partagées (gate)
- **Vagues 2–3** parallèles : modules indépendants (Core / Windows / Assets / Overlay / Automation)
- **Vague 4** séquentielle : intégration App

Skills utilisés : `brainstorming` → `writing-plans` → `subagent-driven-development` + `dispatching-parallel-agents`.

Pour continuer : reprendre **Vague 5** (tests intégration + reviewer) après validation Windows.

---

## 11. Commandes utiles

```powershell
# Build app uniquement
dotnet build src/ParadowSync.App

# Tests par projet
dotnet test tests/ParadowSync.Core.Tests
dotnet test tests/ParadowSync.Windows.Tests
dotnet test tests/ParadowSync.Automation.Tests

# Regénérer icônes placeholder
python scripts/generate-placeholder-icons.py

# Profils utilisateur (runtime)
explorer $env:APPDATA\paradow-sync\profiles
```

---

## 12. Contact / reprise session

**Pour reprendre dans Cursor ou avec un agent :**

> « Reprends paradow-sync depuis `docs/HANDOFF.md`. Spike Windows [fait/à faire]. Priorité : [build / spike / tests manuels / tuning Zaap]. »

**Repo :** https://github.com/Paradow-dev/paradow-sync  
**Branche active dev :** `main` (= `feature/mvp` mergée)
