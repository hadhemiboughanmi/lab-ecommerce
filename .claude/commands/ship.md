---
description: Prépare une livraison — relecture du diff, tests, commit conventionnel et Pull Request, avec validation à chaque étape
argument-hint: "[contexte de la livraison]"
---

Prépare la livraison de la branche courante. Contexte fourni par l'utilisateur : $ARGUMENTS

1. Vérifie la branche courante : si c'est `main`, ou si son préfixe n'est pas `feat/`, `fix/`,
   `chore/`, `test/` ou `ci/`, **arrête-toi** et propose une branche conforme (règles de `CLAUDE.md`).
2. Affiche `git status` et `git diff --stat`, puis résume les changements en trois lignes.
   Signale les fichiers sans rapport avec le contexte de la livraison : ils ne seront pas
   committés sans mon accord explicite.
3. Relis le diff sous l'angle sécurité et conformité : secret, chaîne de connexion avec mot
   de passe, donnée personnelle dans un log, endpoint de diagnostic exposé. Signale tout
   écart avant d'aller plus loin.
4. Si le diff touche `Migrations/`, `.github/`, `k8s/` ou `helm/`, liste explicitement ce qui
   change pour l'exploitation (schéma, droits, déploiement).
5. Lance `dotnet build ECommerce.slnx` puis `dotnet test ECommerce.slnx`. Si un test échoue,
   **arrête-toi** et explique pourquoi. S'il n'existe aucun projet de test, dis-le clairement
   au lieu de présenter la livraison comme testée.
6. Propose un message au format Conventional Commits — en anglais, à l'impératif, avec un
   scope parmi `catalog`, `ordering`, `gateway`, `web`, `db`, `ci`, et un corps qui explique
   le *pourquoi*.
7. **Attends ma validation** avant de committer.
8. Après validation : ajoute uniquement les fichiers de la livraison (pas de `git add .`),
   committe, puis **redemande** avant le push, puis ouvre la Pull Request vers `main`
   (titre, contexte, changements, tests, risques, checklist de revue).

## Interdits

- Ne merge **jamais**. Ne force **jamais** un push. N'utilise jamais `--no-verify`.
- Ne committe pas si l'étape 3 a relevé un secret : signale-le et arrête-toi.
