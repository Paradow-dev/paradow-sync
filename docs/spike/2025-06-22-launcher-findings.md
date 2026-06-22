# Spike Launcher — Résultats (2025-06-22)

**Objectif :** Valider le lancement multi-instance du launcher Ankama (Zaap) et l'accessibilité de l'écran de sélection de personnage via UI Automation.

**Référence :** [Design spec §12](../../superpowers/specs/2025-06-22-paradow-sync-design.md#12-phase-0--technical-spike-pre-implementation)

---

## Environnement de test

| Élément | Valeur |
|---------|--------|
| Date du test | _à compléter_ |
| Testeur | _à compléter_ |
| OS | Windows _version_ |
| Launcher Ankama (Zaap) | _version_ |
| Dofus Unity | _version_ |
| Nombre de comptes Zaap disponibles | _≥ 4_ |
| Résolution / moniteurs | _à compléter_ |
| Outil UI Automation | Accessibility Insights / Inspect.exe |

### Checklist pré-test

- [ ] Au moins 4 comptes Ankama enregistrés dans Zaap (sans stocker de mots de passe dans ce document)
- [ ] Dofus Unity installé et à jour via le launcher
- [ ] Aucune instance Dofus en cours d'exécution
- [ ] Accessibility Insights installé
- [ ] Journal des captures d'écran / logs prêt

---

## Procédure — Lancer 4 comptes Zaap

1. _Décrire la méthode retenue (manuelle, script, raccourci, URI protocol, etc.)_
2. Lancer le compte 1 → attendre l'écran de sélection de personnage
3. Répéter pour les comptes 2, 3 et 4 sans fermer les instances précédentes
4. Vérifier que 4 processus Dofus Unity sont actifs (Gestionnaire des tâches)
5. Noter le délai entre chaque lancement et tout échec ou blocage

**Notes d'exécution :**

```
(espace pour observations libres)
```

---

## Résultats par critère

| # | Critère | Méthode | Résultat | Notes |
|---|---------|---------|----------|-------|
| 1 | 4 clients Dofus Unity concurrents | Test manuel | ☐ PASS / ☐ FAIL | |
| 2 | Invocation scriptable par compte | CLI / API / raccourcis | ☐ PASS / ☐ FAIL | |
| 3 | Écran sélection perso accessible (UI Automation) | Accessibility Insights | ☐ PASS / ☐ FAIL / ☐ PARTIEL | |

---

## Méthode d'invocation

Documenter chaque piste explorée. Ne pas inclure de credentials.

### Arguments CLI

```text
(commande testée, ex. zaap.exe --help ou arguments découverts)
Résultat :
```

### URI protocol (zaap://, ankama://, etc.)

```text
URI testée :
Résultat :
```

### Raccourcis / fichiers .lnk

| Compte (alias) | Chemin raccourci | Arguments extraits | Fonctionne ? |
|----------------|------------------|--------------------|--------------|
| compte-1 | | | ☐ |
| compte-2 | | | ☐ |
| compte-3 | | | ☐ |
| compte-4 | | | ☐ |

### API / fichiers de configuration Zaap

```text
(Fichiers inspectés, endpoints, comportement observé)
```

---

## UI Automation — Écran de sélection de personnage

### Arbre d'accessibilité

| Élément UI | ControlType | Name / AutomationId | Localisable ? | Action possible |
|------------|-------------|---------------------|---------------|-----------------|
| _ex. liste persos_ | | | ☐ | |
| _ex. bouton Jouer_ | | | ☐ | |

### Observations

- **Fenêtre racine :** _titre HWND, classe_
- **Stabilité :** _l'arbre est-il stable après chargement ?_ 
- **Délai avant disponibilité :** _ms_
- **Sélection par nom de personnage :** _faisable via Name property ?_
- **Clic « Jouer » :** _faisable ?_

### Captures / exports

_Lier captures Accessibility Insights ou coller extraits d'arbre (sans données sensibles)._

---

## Recommandation go / no-go

| Décision | ☐ GO | ☐ NO-GO | ☐ GO avec réserves |
|----------|------|---------|-------------------|
| Critères 1 & 2 (multi-instance + scriptable) | | | |
| Critère 3 (auto-sélection perso) | | | |

**Synthèse :**

```
(1–3 phrases : impact sur le scope MVP — auto-sélection incluse ou fallback manuel)
```

**Actions si NO-GO ou réserves :**

- [ ] _à définir_

---

_Document rempli par : __________ — Dernière mise à jour : ___________
