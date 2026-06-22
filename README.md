# paradow-sync

Outil Windows léger pour lancer et organiser plusieurs clients **Dofus Unity** en duo/multi-compte : profils d'équipe, disposition multi-écrans, overlays avec icônes de classe, raccourcis clavier.

## Prérequis (Windows)

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Ankama Launcher (Zaap)](https://go.ankama.com/launcher) avec comptes Dofus Unity enregistrés
- Visual Studio 2022 ou **Build Tools** avec workload **WinUI** (pour compiler l'app)

## Installation (PowerShell)

```powershell
git clone https://github.com/Paradow-dev/paradow-sync.git
cd paradow-sync
dotnet build
dotnet test
dotnet run --project src/ParadowSync.App
```

## Spike technique (optionnel, avant premier usage)

Valider le launcher multi-instance et les overlays :

```powershell
dotnet run --project spike/OverlaySpike
```

Remplir les rapports dans `docs/spike/`.

## Structure

| Projet | Rôle |
|--------|------|
| `ParadowSync.Core` | Profils, settings, catalogue de classes |
| `ParadowSync.Windows` | Gestion fenêtres Win32, overlays |
| `ParadowSync.Automation` | Launcher Zaap, sélection personnage |
| `ParadowSync.App` | Interface WinUI + tray + hotkeys |

## Docs

- [Spec](docs/superpowers/specs/2025-06-22-paradow-sync-design.md)
- [Plan d'implémentation](docs/superpowers/plans/2025-06-22-paradow-sync.md)
- [Checklist tests manuels](docs/manual-test-checklist.md)

## Licence

Usage personnel. Les icônes placeholder dans `assets/icons/` sont à remplacer par des visuels fan-made ; ne pas redistribuer les assets officiels Ankama.
