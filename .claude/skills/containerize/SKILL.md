---
name: containerize
description: Écrit ou optimise les Dockerfiles des microservices de l'ecommerce-app (build multi-étapes, runtime chiselé non-root) et le docker-compose de développement avec PostgreSQL. À utiliser pour conteneuriser un service, réduire une image, corriger un build Docker ou lancer la pile en conteneurs. Scanne l'image, ne met aucun secret dans une couche et demande confirmation avant toute suppression.
---

# Skill : containerize

Objectif : des images **petites, non-root, sans secret et scannées**.

## Procédure

1. **Identifier** le service et ses dépendances de projet (toujours `ECommerce.ServiceDefaults`).
2. **Écrire** `src/<Service>/Dockerfile` :
   - étape `build` sur `mcr.microsoft.com/dotnet/sdk:10.0` ; les `.csproj` copiés **avant**
     le code (cache du `restore`) ;
   - étape finale sur `mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled` : ni shell, ni
     gestionnaire de paquets ;
   - `USER $APP_UID`, `EXPOSE 8080`.
3. **Construire depuis la racine du dépôt** — `docker build -t ecommerce-<svc>:dev -f src/<Service>/Dockerfile .`
   (un `.dockerignore` exclut `bin/` et `obj/`, sinon le `restore` du conteneur casse).
4. **Vérifier non-root** — `docker inspect --format '{{.Config.User}}' <image>` : un UID
   numérique, jamais vide ni `0`. (L'image chiselée n'a pas de commande `id`.)
5. **Scanner** — `trivy image --severity HIGH,CRITICAL <image>`, puis qualifier chaque
   résultat : exploitable **ici** ou théorique ?
6. **Compose** — PostgreSQL avec `healthcheck`, Catalog avec `depends_on: condition: service_healthy`,
   mot de passe lu dans `.env` (non versionné), seul `web` publie un port.

## Garde-fous

- **Aucun secret** dans un `Dockerfile` : ni `ENV`, ni `ARG`, ni fichier copié. La chaîne de
  connexion est injectée au démarrage.
- Image finale **runtime uniquement** (jamais le SDK) et **non-root**.
- `docker system prune`, `docker volume rm`, `docker compose down -v` : bloqués par le hook
  `guard-destructive` — les proposer, ne pas les lancer.
- Images de base **épinglées** (tag explicite, idéalement digest en environnement bancaire).
