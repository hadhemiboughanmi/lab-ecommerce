---
name: ci-pipeline
description: Génère ou fait évoluer les workflows GitHub Actions de l'ecommerce-app — CI (build, tests avec Testcontainers, images, scans), CD avec gate manuel, et revue assistée par Claude en mode headless. À utiliser pour créer, corriger ou durcir un pipeline. N'active jamais de déploiement en production sans approbation humaine.
---

# Skill : ci-pipeline

Objectif : des pipelines **lisibles, au moindre privilège, et qui bloquent ce qui doit l'être**.

## Procédure

1. **Inventorier** — solution (`ECommerce.slnx`), projets de test, Dockerfiles, migrations.
2. **CI** (`.github/workflows/ci.yml`) — déclenchée sur `push` et `pull_request` vers `main` :
   `checkout` → `setup-dotnet` (10.0.x) → `restore` → `build` (Release) → `test` (les tests
   Testcontainers fonctionnent sur `ubuntu-latest`, Docker y est présent) → build des images
   → scan Trivy **bloquant** sur CRITICAL.
3. **Revue des migrations** — si `**/Migrations/**` change : générer le script SQL
   (`dotnet ef migrations script --idempotent`) et le publier en artefact.
4. **Revue assistée** (optionnel) — `claude -p` sur le diff, `--allowedTools "Read,Grep,Glob"`,
   résultat publié en commentaire de PR. L'agent **commente**, il ne décide pas.
5. **CD** (`.github/workflows/cd.yml`) — `staging` automatique après une CI verte sur `main`,
   `production` derrière un `environment` protégé (approbation requise).
6. **Commenter** chaque étape en français pour l'équipe.

## Garde-fous

- `permissions:` explicites et minimales dans **chaque** workflow.
- Aucun secret en clair : `secrets.*` uniquement ; jamais de `echo` d'un secret.
- Actions **épinglées** (au minimum sur une version majeure, idéalement sur un SHA).
- Jamais de déploiement en production sans gate humain (environnement protégé côté GitHub).
- Le job de revue Claude ne reçoit **aucun** outil d'écriture.
