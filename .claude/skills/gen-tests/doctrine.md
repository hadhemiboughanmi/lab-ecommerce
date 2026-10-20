# Doctrine de test — ecommerce-app

Fichier d'appui du skill `gen-tests`, chargé seulement quand le skill en a besoin.

## Quel type de test pour quel besoin

| Ce qu'on veut prouver | Outil | Exemple dans le dépôt |
|---|---|---|
| Un endpoint répond correctement (codes HTTP, JSON) | `WebApplicationFactory<Program>` | `tests/ECommerce.Catalog.Tests` |
| Un service se comporte bien quand son voisin répond X | faux `HttpMessageHandler` injecté dans le client typé | `OrderingApiFactory` + `FakeCatalogHandler` |
| Une règle portée par la base (CHECK, index unique) | Testcontainers `postgres:17-alpine` | `CatalogApiFactory` (lab 04) |
| Le contrat JSON attendu par un consommateur | lecture du JSON brut (`JsonDocument`) | `CatalogContractTests` (lab 11) |
| Une métrique métier est émise | `MeterListener` | `OrderingEndpointsTests` (lab 10) |

## Pièges connus

- **`Program` invisible** : les *top-level statements* génèrent une classe interne. Ajouter
  `public partial class Program { }` à la fin de `Program.cs`.
- **Provider in-memory** : il ignore les contraintes, les index uniques et les transactions.
  Un test vert en in-memory ne prouve rien sur PostgreSQL.
- **Client typé + service discovery** : en test, fournir `services:<nom>:http:0` via
  `UseSetting`, sinon la résolution du nom échoue avant même d'atteindre le faux handler.
- **Tests qui partagent une base** : chaque test crée ses propres données (SKU unique,
  client unique) ; aucun test ne suppose l'ordre d'exécution.

## Ce qu'on ne teste pas

- Le framework (que `MapGet` route bien) : c'est le travail de Microsoft.
- Les accesseurs triviaux.
- Le pourcentage de couverture comme objectif : un chemin critique non testé compte plus
  que dix lignes triviales couvertes.
