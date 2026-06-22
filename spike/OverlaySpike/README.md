# OverlaySpike

Prototype Win32 minimal pour la Phase 0 de paradow-sync. Valide :

- Fenêtre layered semi-transparente, click-through (`WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_NOACTIVATE`)
- Hook `SetWinEventHook(EVENT_SYSTEM_FOREGROUND)` avec logs horodatés

**Prérequis :** Windows, [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Build

Depuis la racine du dépôt (ou du worktree) :

```bash
dotnet build spike/OverlaySpike
```

## Run

```bash
dotnet run --project spike/OverlaySpike
```

## Procédure de test

1. Lancer Dofus Unity (fenêtré, puis borderless / plein écran).
2. Exécuter OverlaySpike — une petite fenêtre cyan semi-transparente apparaît.
3. Glisser cette fenêtre **au-dessus** du client de jeu (mode draggable par défaut).
4. Appuyer sur **Ctrl+Shift+T** pour activer le click-through (`WS_EX_TRANSPARENT`).
5. Cliquer dans le jeu : les clics doivent passer à travers l'overlay lorsque le click-through est actif.
6. Changer de focus (Alt+Tab, clic autre fenêtre, second client Dofus) et observer les logs `[FOCUS]` dans la console.
7. Mesurer le délai entre le changement de focus perçu et le timestamp log (objectif spec : **< 50 ms**).
8. Documenter les résultats dans [`docs/spike/2025-06-22-overlay-findings.md`](../../docs/spike/2025-06-22-overlay-findings.md).

## Arrêt

`Ctrl+C` dans la console, ou fermer la fenêtre console.

## Notes

- Ne s'exécute pas sous Linux/WSL (API Win32 uniquement).
- Aucune dépendance NuGet — P/Invoke seulement.
- Ce code n'est **pas** du code production ; il ne doit pas être copié tel quel dans `src/`.
