---
name: gen-tests
description: Écrit ou complète les tests automatisés de l'ecommerce-app (.NET 10) selon la doctrine d'équipe — tests d'intégration WebApplicationFactory, faux services HTTP, Testcontainers PostgreSQL. À utiliser quand on demande d'ajouter des tests, de tester un endpoint, de couvrir un cas d'erreur ou de reproduire un bug par un test. N'invente aucun comportement et ne modifie jamais un test pour le faire passer.
---

# Skill : gen-tests

Objectif : des tests qui **protègent un comportement**, pas des tests qui font du chiffre.

## Procédure

1. **Comprendre avant d'écrire** — lire l'endpoint ou la classe ciblée et lister les
   comportements observables : entrées, sorties, codes HTTP, cas d'erreur, effets en base.
   Si un comportement attendu est ambigu : **demander**, ne pas deviner.
2. **Choisir le bon niveau** — voir [doctrine.md](doctrine.md) :
   - endpoint HTTP → test d'intégration `WebApplicationFactory<Program>` ;
   - appel à un autre service → faux `HttpMessageHandler` (pas d'appel réseau réel) ;
   - comportement qui dépend du moteur SQL (contrainte, index unique, séquence) →
     **Testcontainers PostgreSQL**, jamais le provider in-memory.
3. **Écrire** — un test = un comportement, nommé `Methode_Condition_ResultatAttendu`,
   structure *Arrange / Act / Assert*. Couvrir le nominal **et** au moins un cas d'erreur
   et un cas limite.
4. **Exécuter** — `dotnet test ECommerce.slnx`. Un nouveau test doit d'abord **échouer**
   quand il reproduit un bug (sinon il ne prouve rien), puis passer après correction.
5. **Rendre compte** — tableau `Comportement | Test | Résultat`, puis les cas volontairement
   non couverts et pourquoi.

## Garde-fous

- **Ne jamais modifier un test existant pour faire passer un build** : on corrige le code.
  Si le contrat a réellement changé, le dire explicitement et demander validation.
- Aucune donnée réelle : noms, e-mails et montants **fictifs**.
- Aucun secret dans un test : les conteneurs Testcontainers génèrent leurs identifiants.
- Pas de `Thread.Sleep` ni de dépendance à l'ordre d'exécution des tests.
