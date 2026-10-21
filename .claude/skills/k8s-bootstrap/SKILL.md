---
name: k8s-bootstrap
description: Crée le cluster k3d local de l'ecommerce-app (ecom-dev), y importe les images, vérifie le Secret de base créé par l'humain, puis applique les manifestes (PostgreSQL, quatre services, ingress) et vérifie le rollout. À utiliser pour monter, remonter ou redéployer l'application sur Kubernetes en local. Montre chaque commande avant de l'exécuter et ne supprime jamais un cluster, un namespace ou un volume.
---

# Skill : k8s-bootstrap

Objectif : un cluster **reproductible** où l'application tourne avec sa base, sans secret dans
les fichiers.

## Procédure

1. **Cluster** — `k3d cluster list` ; s'il n'existe pas : `k3d cluster create --config k8s/k3d-cluster.yaml`.
   Vérifier `kubectl config current-context` = `k3d-ecom-dev` **avant** toute autre commande.
2. **Images** — vérifier qu'elles existent (`docker images | grep ecommerce-`), puis
   `k3d image import ecommerce-{catalog,ordering,gateway,web}:dev -c ecom-dev`.
3. **Namespace** — `kubectl apply -f k8s/namespace.yaml`.
4. **Secret** — `kubectl -n ecommerce get secret catalog-db`. S'il est absent : **s'arrêter** et
   donner à l'humain la commande à lancer lui-même :
   `kubectl -n ecommerce create secret generic catalog-db --from-literal=password="$(openssl rand -hex 24)"`.
   Ne jamais générer, afficher ni lire la valeur.
5. **Base** — `kubectl apply -f k8s/postgres.yaml`, puis `kubectl -n ecommerce rollout status statefulset/postgres`.
6. **Services** — `kubectl apply -f k8s/` puis `rollout status` de chaque Deployment.
7. **Vérifier** — `curl -s http://ecommerce.localhost:8081/products` et la readiness de Catalog.
8. **Rendre compte** — tableau ressource | état | preuve (commande et sortie).

## Garde-fous

- Toujours vérifier le **contexte kubectl** avant d'agir : jamais un cluster partagé.
- Jamais de Secret dans un fichier, un prompt ou une sortie : `kubectl get secret -o yaml` est interdit.
- `k3d cluster delete`, `kubectl delete ns`, `kubectl delete pvc` : bloqués par le hook
  `guard-destructive` — les proposer, ne pas les lancer.
- En cas d'échec d'un rollout : déléguer le diagnostic au sous-agent `k8s-debug-pod`.
