# Manual Test Checklist — paradow-sync MVP

## Prérequis

- [ ] Windows 10/11 x64 avec .NET 8 runtime
- [ ] Ankama Zaap installé (`C:\Program Files\Ankama\Zaap\zaap.exe`)
- [ ] Au moins 2 comptes Dofus Unity enregistrés dans Zaap
- [ ] Build réussi : `dotnet build src/ParadowSync.App`

## Démarrage application

- [ ] L'application démarre sans crash
- [ ] La fenêtre principale s'affiche avec la liste des profils
- [ ] L'icône tray apparaît dans la zone de notification
- [ ] Clic droit sur l'icône tray → menu visible (Profils, Arrêter tout, Basculer overlay, Quitter)

## Gestion des profils

- [ ] **Nouveau** : créer un profil avec 1+ comptes (account ID, personnage, classe, écran, position)
- [ ] **Enregistrer** : le profil apparaît dans la liste après rechargement
- [ ] **Modifier** : ouvrir un profil existant, changer le nom, enregistrer
- [ ] **Supprimer** : confirmer la suppression, le profil disparaît de la liste
- [ ] Fichiers JSON créés dans `%AppData%\paradow-sync\profiles\`

## Lancement de session

- [ ] Sélectionner un profil → **Lancer**
- [ ] Zaap lance les clients (un par compte du profil)
- [ ] Les fenêtres Dofus sont positionnées selon le layout configuré
- [ ] Le bandeau d'équipe (team strip) apparaît si activé
- [ ] Les badges de classe apparaissent sur les fenêtres si activés
- [ ] Statut affiché dans la barre de statut de la fenêtre principale

## Focus et hotkeys

- [ ] `Ctrl+1` à `Ctrl+8` : focus sur le slot correspondant (fenêtre Dofus au premier plan)
- [ ] Clic sur une icône du bandeau : focus sur le slot correspondant
- [ ] `Ctrl+Shift+O` : overlay masqué / réaffiché
- [ ] `Ctrl+Shift+Q` : arrêt de la session (overlay disparu)

## Tray

- [ ] Menu **Profils** → liste des profils, clic lance le profil
- [ ] **Arrêter tout** depuis le tray arrête la session active
- [ ] **Basculer l'overlay** depuis le tray fonctionne
- [ ] **Quitter** ferme l'application proprement
- [ ] Double-clic sur l'icône tray → fenêtre principale au premier plan

## Arrêt

- [ ] Bouton **Arrêter** dans la fenêtre principale arrête la session
- [ ] Overlay et focus tracker s'arrêtent (pas de bandeau/badge résiduel)

## Robustesse

- [ ] Fermer manuellement une fenêtre Dofus → après ~5 s le slot passe en erreur (validation HWND)
- [ ] Relancer un profil pendant une session active arrête d'abord la session précédente
- [ ] Aucun champ mot de passe dans l'UI ni dans les fichiers JSON de profil

## Non couvert (MVP)

- [ ] Raccourcis personnalisés par profil (champs texte informatifs seulement)
- [ ] Éditeur visuel de grille de placement
- [ ] Tests automatisés UI
