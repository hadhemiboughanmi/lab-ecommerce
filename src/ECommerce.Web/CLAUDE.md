# CLAUDE.md — ECommerce.Web

Règles propres au front Blazor. Elles s'ajoutent à celles du CLAUDE.md racine.

## Règles front

- **Aucune information portée par la couleur seule.** Un statut, une erreur ou un état (commande, stock, validation) doit aussi être donné par un texte, une icône avec libellé ou un motif. Exemple : un badge de statut affiche `Expédiée`, pas seulement un fond vert.
- **Chaque bouton a un nom accessible.** Il vient du texte visible du bouton ou, pour un bouton qui n'a qu'une icône, d'un `aria-label`. Une icône seule (`<button><i class="bi bi-cart"></i></button>`) est interdite.
- **Pas de nouvelle dépendance front sans accord.** Il faut un accord explicite avant d'ajouter un package NuGet au projet Web, une bibliothèque JS/CSS dans `wwwroot/` ou un appel à un CDN.
