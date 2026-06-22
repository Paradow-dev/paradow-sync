# Spike Overlay — Résultats (2025-06-22)

**Objectif :** Valider les fenêtres layered Win32 (click-through) au-dessus de Dofus Unity et la fiabilité du hook de focus `SetWinEventHook`.

**Prototype :** [`spike/OverlaySpike`](../../spike/OverlaySpike/README.md)

**Référence :** [Design spec §12](../../superpowers/specs/2025-06-22-paradow-sync-design.md#12-phase-0--technical-spike-pre-implementation)

---

## Instructions d'exécution OverlaySpike

1. Sur une machine **Windows** avec le SDK .NET 8 :
   ```bash
   dotnet run --project spike/OverlaySpike
   ```
2. Lancer Dofus Unity (fenêtré ou borderless selon le scénario testé)
3. Déplacer la fenêtre overlay spike (petit rectangle semi-transparent) **au-dessus** du client de jeu
4. Activer le click-through avec **Ctrl+Shift+T** (`WS_EX_TRANSPARENT`)
5. Cliquer dans le jeu : les clics doivent **traverser** l'overlay (pas de vol de focus)
6. Alterner le focus entre plusieurs fenêtres (Dofus, navigateur, bureau) et noter les événements dans la console
7. Mesurer la latence entre changement de focus réel et log console (chronomètre ou delta timestamps)
8. Quitter avec `Ctrl+C` ou fermer la console

### Checklist pré-test

- [ ] OverlaySpike compilé et exécuté sur Windows (pas WSL)
- [ ] Au moins 1 client Dofus Unity visible
- [ ] Scénario fenêtré testé
- [ ] Scénario borderless testé
- [ ] Scénario plein écran testé (si applicable)

---

## Visibilité overlay layered sur Dofus Unity

| Scénario | Overlay visible ? | Click-through OK ? | Focus jeu préservé ? | Notes |
|----------|-------------------|--------------------|-----------------------|-------|
| Fenêtré (windowed) | ☐ | ☐ | ☐ | |
| Borderless | ☐ | ☐ | ☐ | |
| Plein écran | ☐ | ☐ | ☐ | |
| Multi-moniteur | ☐ | ☐ | ☐ | |

**Observations visuelles :**

```
(z-order, transparence, clignotement, disparition au focus, etc.)
```

---

## Latence hook focus (`EVENT_SYSTEM_FOREGROUND`)

Objectif spec : **< 50 ms** entre changement de focus et détection.

| # | Fenêtre source → cible | Latence mesurée (ms) | < 50 ms ? | Méthode de mesure |
|---|------------------------|----------------------|-----------|-------------------|
| 1 | | | ☐ | |
| 2 | | | ☐ | |
| 3 | | | ☐ | |
| 4 | Dofus A → Dofus B | | ☐ | |
| 5 | Dofus → autre app | | ☐ | |
| 6 | autre app → Dofus | | ☐ | |

**Statistiques :**

| Métrique | Valeur |
|----------|--------|
| Min (ms) | |
| Max (ms) | |
| Moyenne (ms) | |
| Événements manqués | |

---

## Compatibilité borderless / plein écran

### Borderless

```
(comportement overlay, limites connues)
```

### Plein écran

```
(overlay au-dessus possible ? fallback bandeau bord d'écran ?)
```

### Fallback spec (si click-through échoue sur le jeu)

Bandeau d'équipe **hors** des limites de la fenêtre de jeu (bord d'écran / moniteur adjacent) — faisable ?

| Fallback | Faisable ? | Notes |
|----------|------------|-------|
| Team strip hors fenêtre jeu | ☐ | |
| Badges uniquement sur barre de titre / hors client | ☐ | |

---

## Résultats par critère

| # | Critère | Résultat | Notes |
|---|---------|----------|-------|
| 4 | Overlay layered visible + click-through sur Dofus | ☐ PASS / ☐ FAIL / ☐ PARTIEL | |
| 5 | Focus hook < 50 ms | ☐ PASS / ☐ FAIL | |

---

## Recommandation go / no-go

| Décision | ☐ GO | ☐ NO-GO | ☐ GO avec fallback |
|----------|------|---------|-------------------|
| Critère 4 (overlay in-game) | | | |
| Critère 5 (latence focus) | | | |

**Synthèse :**

```
(1–3 phrases : overlay in-game validé, ou stratégie de fallback retenue)
```

**Impact architecture :**

- [ ] Overlay Manager : fenêtres layered sur HWND jeu
- [ ] Overlay Manager : team strip externe (hors bounds jeu)
- [ ] Ajustement budget perf / z-order documenté

---

_Document rempli par : __________ — Dernière mise à jour : ___________
