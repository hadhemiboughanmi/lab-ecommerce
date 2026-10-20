# Checklist de revue d'une migration EF Core

À dérouler sur le **SQL généré** (`dotnet ef migrations script ... -o migration.sql`), pas sur le C#.

## Bloquant — à justifier avant d'aller plus loin

- [ ] `DROP TABLE` / `DROP COLUMN` : quelles données disparaissent ? existe-t-il une sauvegarde ?
- [ ] `ALTER COLUMN ... TYPE` : la conversion est-elle sans perte (ex. `varchar(200)` → `varchar(100)`) ?
- [ ] `NOT NULL` ajouté sur une table non vide : quelle valeur pour les lignes existantes ?
- [ ] Renommage détecté comme `DROP` + `ADD` : EF a-t-il perdu les données de la colonne ?

## À vérifier

- [ ] Chaque clé étrangère a un index.
- [ ] Les contraintes métier sont en base (`CHECK`, index uniques), pas seulement dans le code.
- [ ] `ON DELETE` : `RESTRICT` sauf justification écrite pour `CASCADE`.
- [ ] Le seed (`InsertData`) ne contient que des données synthétiques.
- [ ] Les séquences d'identité sont réalignées après un seed à identifiants explicites
      (Npgsql génère les `setval` lui-même : vérifier qu'ils sont présents).
- [ ] Un index sur une grosse table : prévoir `CREATE INDEX CONCURRENTLY` hors migration.

## Exploitation

- [ ] La migration est idempotente en script (`--idempotent`) pour le pipeline.
- [ ] Le temps d'exécution est acceptable (verrou sur une table volumineuse ?).
- [ ] Le retour arrière est décrit (migration inverse ou procédure).
