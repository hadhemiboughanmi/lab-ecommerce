---
name: ef-migration
description: Fait évoluer le schéma PostgreSQL de l'ecommerce-app avec EF Core — modifier les entités et le DbContext, générer la migration avec dotnet ef, relire le SQL produit, l'appliquer en local et vérifier dans la base. À utiliser pour ajouter une table, une colonne, un index, une contrainte ou des données de seed. N'applique jamais une migration hors du poste local et ne réécrit jamais une migration déjà committée.
---

# Skill : ef-migration

Objectif : un changement de schéma **versionné, relu et réversible**.

## Pré-requis

- `dotnet ef --version` répond (sinon : `dotnet tool install --global dotnet-ef`).
- La base locale tourne via Aspire (`dotnet run --project src/ECommerce.AppHost`).

## Procédure

1. **Modéliser** — modifier les entités (`Models/`) et `OnModelCreating` (`Data/CatalogDbContext.cs`) :
   longueurs maximales, `IsRequired`, index (dont les clés étrangères), contraintes `CHECK`,
   `DeleteBehavior.Restrict` par défaut. Le seed éventuel passe par `HasData` avec des
   données **synthétiques**.
2. **Compiler** — `dotnet build src/ECommerce.Catalog.Api`.
3. **Générer** — `dotnet ef migrations add <NomExplicite> --project src/ECommerce.Catalog.Api`.
   Le nom décrit l'intention : `AddProductSku`, pas `Update3`.
4. **Relire le SQL, pas le C#** —
   `dotnet ef migrations script <Précédente> <NomExplicite> --project src/ECommerce.Catalog.Api -o migration.sql`.
   Dérouler la [checklist de revue](checklist-revue.md) et la présenter à l'utilisateur.
5. **Appliquer en local** — relancer l'AppHost (les migrations s'appliquent au démarrage en
   Development), ou `dotnet ef database update --connection "$CATALOG_DB_CONNECTION"`.
6. **Vérifier dans la base** — via le serveur MCP `catalogdb` (lecture seule) s'il est
   connecté, sinon `psql` : la table `__EFMigrationsHistory` contient la migration, le
   schéma correspond.
7. **Tester** — `dotnet test` : les tests Testcontainers rejouent toutes les migrations sur
   une base vierge. C'est la preuve que la chaîne complète fonctionne.

## Garde-fous

- **Une migration committée est immuable** : pour corriger, on en **ajoute** une nouvelle
  (le hook `guard-migrations` bloque l'édition de toute façon).
- Toute opération destructive (`DropColumn`, `DropTable`, changement de type) est signalée
  **en tête** de la réponse, avec son impact sur les données existantes.
- Aucune chaîne de connexion avec mot de passe dans le code : la fabrique design-time n'en
  a pas besoin pour `migrations add`.
- Jamais `dotnet ef database update` sur une base partagée, de recette ou de production :
  en dehors du poste, les migrations passent par un pipeline revu.
