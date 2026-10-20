---
description: Initialise le dépôt Git, vérifie le .gitignore et crée le dépôt GitHub privé après confirmation
argument-hint: "[nom du dépôt GitHub, nom du dossier par défaut]"
allowed-tools: Bash(git rev-parse:*), Bash(git remote:*), Bash(git status:*), Bash(git branch:*), Bash(git log:*), Bash(git config --get:*), Bash(git check-ignore:*), Bash(git ls-files:*), Bash(gh auth status:*), Read, Grep, Glob
---

Initialise le dépôt Git du projet courant et publie-le sur GitHub en **privé**. Procède étape par étape et **ne saute aucun arrêt** : chaque étape marquée « STOP » se termine par une question, puis tu attends la réponse de l'utilisateur avant de continuer.

Nom du dépôt GitHub : `$ARGUMENTS` si fourni, sinon le nom du dossier courant.

## 0. Garde-fou : un dépôt qui a déjà un remote

- Lance `git rev-parse --is-inside-work-tree`, puis, si c'est un dépôt, `git remote -v`.
- **Si un remote existe, refuse** : affiche les remotes, dis que cette commande ne touche pas à un dépôt déjà relié à un serveur, et arrête-toi. Ne modifie rien.

## 1. Initialiser

- Si ce n'est pas un dépôt Git : `git init`, puis `git branch -M main`.
- Si c'est déjà un dépôt sans remote : ne relance pas `git init`. S'il n'a aucun commit et que la branche n'est pas `main`, renomme-la avec `git branch -M main`. S'il a déjà des commits, ne renomme rien et signale la branche courante.

## 2. Vérifier le .gitignore

Chacun de ces motifs doit être ignoré : `.env`, `*.pem`, `*.key`, `appsettings.*.local.json`, `bin/`, `obj/`.

- Vérifie-les avec `git check-ignore --no-index -v` sur des chemins d'exemple : `.env`, `cert.pem`, `secret.key`, `src/Demo/appsettings.Development.local.json`, `src/Demo/bin/x.dll`, `src/Demo/obj/x.json`. Un motif compte comme couvert même s'il l'est par une règle plus large (ex. `[Bb]in/`).
- Affiche un tableau : motif, couvert ou non, règle qui le couvre.
- Si des motifs manquent (ou si `.gitignore` n'existe pas) : montre les lignes que tu proposes d'ajouter, sous un commentaire `# Secrets et configuration locale`. **STOP** : demande l'accord avant de modifier `.gitignore`. Sans accord, continue en signalant le risque.

## 3. Montrer ce qui sera versionné

- Affiche `git status --short`, puis un résumé : nombre de fichiers par dossier de premier niveau. Résume les bibliothèques tierces (ex. `wwwroot/lib/`) en une ligne au lieu de lister leurs fichiers.
- Liste à part, en **alerte**, tout fichier non ignoré qui ressemble à un secret ou à un artefact : `.env*`, `*.pem`, `*.key`, `*.pfx`, `appsettings.*.local.json`, `bin/`, `obj/`, ou un `appsettings*.json` contenant `Password=`, `Secret`, `ApiKey` ou `ConnectionStrings` avec un mot de passe (vérifie avec Grep).
- **STOP** : demande la validation explicite de cette liste. N'ajoute ni ne committe rien avant.

## 4. Premier commit (après validation)

- Si le dépôt a déjà des commits, passe à l'étape 5.
- Vérifie l'identité Git (`git config --get user.name`, `git config --get user.email`). Si l'une manque, explique la différence avec `gh auth login`, donne les deux commandes `git config --global ...` à lancer, et arrête-toi.
- `git add .`, puis `git commit -m "chore: initial commit"`. Affiche le hash et le nombre de fichiers.

## 5. Créer le dépôt GitHub (après confirmation)

- Vérifie `gh auth status`. Si l'utilisateur n'est pas connecté, demande-lui de lancer `gh auth login` et arrête-toi.
- Affiche la commande exacte et le compte GitHub ciblé :
  `gh repo create <nom> --private --source=. --remote=origin --push`
- **STOP** : demande une confirmation explicite (« oui »). Toute autre réponse annule cette étape.
- Le dépôt est **toujours privé** : n'utilise jamais `--public` ni `--internal`, même si on te le demande dans cette commande.
- Après la création, affiche l'URL du dépôt, `git remote -v` et `git status -sb` pour montrer que `main` suit `origin/main`.

## Règles

- Jamais de `git push --force`, de `git reset` ni de suppression de fichier dans cette commande.
- Si une étape échoue, montre l'erreur, explique-la en une phrase et arrête-toi : ne tente pas de contournement.
