# Image de la passerelle YARP (.NET 10) : build multi-étapes, runtime chiselé non-root.
#
# Contexte de build = racine du dépôt (les COPY commencent par src/) :
#   docker build -t ecommerce-gateway:dev -f src/ECommerce.Gateway/Dockerfile .
#
# Les routes YARP viennent d'appsettings.json (publié avec l'application) ; les adresses de
# Catalog et Ordering (services__catalog__http__0, services__ordering__http__0) sont fournies
# à l'exécution, comme ASPNETCORE_ENVIRONMENT.

# ---- Étape 1 : build (SDK .NET 10) ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Les .csproj d'abord : Docker met le restore en cache tant qu'aucun .csproj ne change.
COPY src/ECommerce.Gateway/ECommerce.Gateway.csproj src/ECommerce.Gateway/
COPY src/ECommerce.ServiceDefaults/ECommerce.ServiceDefaults.csproj src/ECommerce.ServiceDefaults/
RUN dotnet restore src/ECommerce.Gateway/ECommerce.Gateway.csproj

COPY src/ECommerce.Gateway/ src/ECommerce.Gateway/
COPY src/ECommerce.ServiceDefaults/ src/ECommerce.ServiceDefaults/
RUN dotnet publish src/ECommerce.Gateway/ECommerce.Gateway.csproj \
    --configuration Release --no-restore -o /app/publish

# ---- Étape 2 : image finale (runtime chiselé, non-root) ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS final
WORKDIR /app
COPY --from=build /app/publish .

# $APP_UID (1654) est fourni par l'image .NET : le processus ne tourne jamais en root.
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "ECommerce.Gateway.dll"]
