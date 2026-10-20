---
description: Relit le diff courant au regard des règles du projet (CLAUDE.md)
argument-hint: "[ref de base, main par défaut]"
allowed-tools: Bash(git diff:*), Bash(git status:*), Bash(git log:*), Bash(git branch:*), Bash(git rev-parse:*), Read, Grep, Glob
---

Relis les changements de la branche courante et signale les écarts aux règles du projet. Ne modifie aucun fichier.

## 1. Récupérer le diff

- Base : `$ARGUMENTS` si fourni, sinon `main`.
- Diff à relire : `git diff <base>...HEAD`, plus les changements non commités (`git diff HEAD`) et les fichiers non suivis listés par `git status --short`.
- Si la base n'existe pas ou que le dépôt n'a aucun commit, dis-le et arrête-toi.
- Lis le contexte autour des lignes modifiées quand le diff seul ne suffit pas pour juger.

## 2. Vérifier

Les règles font foi dans `CLAUDE.md` (racine) et dans le `CLAUDE.md` du projet concerné (ex. `src/ECommerce.Web/CLAUDE.md`). Relis-les avant de commencer. Points à contrôler en priorité :

**Interdits**
- Donnée client (nom, e-mail) dans un log, une trace, un message d'exception.
- Secret dans le code ou un `appsettings*.json`.
- Test modifié ou supprimé pour faire passer un build.
- Migration EF Core déjà committée modifiée.

**Conventions**
- URL ou port en dur pour appeler un service, au lieu de `https+http://<nom>`.
- Nouveau service ou nouvelle dépendance absent de `AppHost.cs` (`WithReference`, `WaitFor`), ou nom de ressource incohérent entre `AppHost.cs`, `Program.cs` et la config YARP.
- Web qui appelle un service sans passer par la gateway.
- Endpoint ajouté sans test d'intégration `WebApplicationFactory` dans `tests/`, ou projet de test absent de `ECommerce.slnx`.
- Modèle d'API modifié sans mise à jour des DTO dupliqués chez ses consommateurs.
- Nom de branche hors `feat/`, `fix/`, `chore/`, `test/`, `ci/`, ou travail sur `main`. Messages de commit hors Conventional Commits en anglais à l'impératif (`git log <base>..HEAD`).

**Front (si `src/ECommerce.Web/` est touché)**
- Information portée par la couleur seule.
- Bouton sans nom accessible.
- Nouvelle dépendance front (NuGet, JS/CSS, CDN).

Signale aussi les bugs évidents que tu remarques en passant, mais ne fais pas de revue de style.

## 3. Rendre compte

Pour chaque écart, une ligne : gravité (**Bloquant** pour un interdit, **À corriger** pour une convention, **Remarque** sinon), lien `fichier:ligne`, règle concernée, et ce qu'il faut changer. Classe du plus grave au moins grave. Si rien n'est à signaler, dis-le en une phrase et précise la base comparée.
