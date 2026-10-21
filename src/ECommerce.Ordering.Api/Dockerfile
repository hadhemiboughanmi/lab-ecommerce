# Image du service Ordering (.NET 10) : build multi-étapes, runtime chiselé non-root.
#
# Contexte de build = racine du dépôt (les COPY commencent par src/) :
#   docker build -t ecommerce-ordering:dev -f src/ECommerce.Ordering.Api/Dockerfile .
#
# Aucun secret ni environnement figé dans l'image : l'adresse de Catalog
# (services__catalog__http__0) et ASPNETCORE_ENVIRONMENT sont fournis à l'exécution.

# ---- Étape 1 : build (SDK .NET 10) ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Les .csproj d'abord : Docker met le restore en cache tant qu'aucun .csproj ne change.
COPY src/ECommerce.Ordering.Api/ECommerce.Ordering.Api.csproj src/ECommerce.Ordering.Api/
COPY src/ECommerce.ServiceDefaults/ECommerce.ServiceDefaults.csproj src/ECommerce.ServiceDefaults/
RUN dotnet restore src/ECommerce.Ordering.Api/ECommerce.Ordering.Api.csproj

COPY src/ECommerce.Ordering.Api/ src/ECommerce.Ordering.Api/
COPY src/ECommerce.ServiceDefaults/ src/ECommerce.ServiceDefaults/
RUN dotnet publish src/ECommerce.Ordering.Api/ECommerce.Ordering.Api.csproj \
    --configuration Release --no-restore -o /app/publish

# ---- Étape 2 : image finale (runtime chiselé, non-root) ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS final
WORKDIR /app
COPY --from=build /app/publish .

# $APP_UID (1654) est fourni par l'image .NET : le processus ne tourne jamais en root.
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "ECommerce.Ordering.Api.dll"]
