# Image du service Catalog (.NET 10) : build multi-étapes, runtime chiselé non-root.
#
# Contexte de build = racine du dépôt (les COPY commencent par src/) :
#   docker build -t ecommerce-catalog:dev -f src/ECommerce.Catalog.Api/Dockerfile .
#
# Aucun secret dans l'image : la chaîne de connexion (ConnectionStrings__catalogdb) est injectée
# au démarrage, par compose ou par un Secret Kubernetes.
# L'environnement n'est pas figé ici (Production par défaut) : il se choisit à l'exécution
# (ASPNETCORE_ENVIRONMENT), pour qu'une même image serve du poste à la production.

# ---- Étape 1 : build (SDK .NET 10) ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Les .csproj d'abord (Catalog et sa seule dépendance, ServiceDefaults) : Docker met le restore
# en cache tant qu'aucun .csproj ne change.
COPY src/ECommerce.Catalog.Api/ECommerce.Catalog.Api.csproj src/ECommerce.Catalog.Api/
COPY src/ECommerce.ServiceDefaults/ECommerce.ServiceDefaults.csproj src/ECommerce.ServiceDefaults/
RUN dotnet restore src/ECommerce.Catalog.Api/ECommerce.Catalog.Api.csproj

# Puis le code, et la publication en Release.
COPY src/ECommerce.Catalog.Api/ src/ECommerce.Catalog.Api/
COPY src/ECommerce.ServiceDefaults/ src/ECommerce.ServiceDefaults/
RUN dotnet publish src/ECommerce.Catalog.Api/ECommerce.Catalog.Api.csproj \
    --configuration Release --no-restore -o /app/publish

# ---- Étape 2 : image finale (runtime chiselé, non-root) ----
# « Chiseled » : ni shell ni gestionnaire de paquets. Plus petite, et presque rien à exploiter.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS final
WORKDIR /app
COPY --from=build /app/publish .

# $APP_UID (1654) est fourni par l'image .NET : le processus ne tourne jamais en root.
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "ECommerce.Catalog.Api.dll"]
