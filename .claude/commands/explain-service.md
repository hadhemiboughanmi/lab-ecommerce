---
description: Explique le rôle, l'API et les dépendances d'un service du projet
argument-hint: "<service> (catalog, ordering, gateway, web…)"
allowed-tools: Read, Grep, Glob
---

Explique le service `$ARGUMENTS` à un développeur qui découvre le projet. Ne modifie aucun fichier.

## 1. Trouver le service

- `$ARGUMENTS` est un nom de ressource Aspire (`catalog`, `ordering`, `gateway`, `web`…) ou un nom de projet (`ECommerce.Catalog.Api`…). Retrouve la ressource dans `src/ECommerce.AppHost/AppHost.cs` et le projet dans `src/`.
- Si `$ARGUMENTS` est vide ou ne correspond à aucun service, liste les ressources déclarées dans `AppHost.cs` et arrête-toi.

## 2. Lire

Lis ce qui existe parmi ces fichiers du projet :
- `Program.cs` : services enregistrés, clients typés, configuration.
- `Endpoints/*Endpoints.cs` : routes, verbes, entrées et sorties.
- `Data/` ou le `DbContext` : modèle et données de seed.
- `Services/` : clients vers les autres services.
- `appsettings.json` (pour la gateway : les routes et clusters YARP).
- le `README.md` du projet, en français.

Lis aussi, dans le reste du dépôt, qui appelle ce service : `WithReference` dans `AppHost.cs`, `https+http://<nom>` dans les `Program.cs`, routes YARP de la gateway.

## 3. Expliquer

Réponds avec ces sections, en citant les fichiers sous forme de liens `fichier:ligne` :

1. **Rôle** : ce que fait le service, en deux ou trois phrases.
2. **Place dans l'architecture** : qui l'appelle et par quel chemin (via la gateway ou en direct), qui il appelle, son ordre de démarrage (`WaitFor`).
3. **API** : un tableau verbe, route, rôle, codes de retour. Indique la route telle qu'elle est vue depuis la gateway (`/catalog/api/...`) quand elle existe. Pour Web, remplace ce tableau par les pages et les clients API.
4. **Données** : modèle, stockage (InMemory, rien n'est persisté), données de seed.
5. **Points d'attention** : comportements surprenants ou limites visibles dans le code (ex. stock non vérifié, DTO dupliqués à tenir à jour, retries sur POST).

Décris uniquement ce que tu as lu dans le code. Ne présente pas comme existant ce qui est seulement prévu.
