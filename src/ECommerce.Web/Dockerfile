# Image du frontend Blazor (.NET 10) : build multi-étapes, runtime chiselé non-root.
#
# Contexte de build = racine du dépôt (les COPY commencent par src/) :
#   docker build -t ecommerce-web:dev -f src/ECommerce.Web/Dockerfile .
#
# Les assets statiques (wwwroot : CSS, Bootstrap, favicon) sont copiés avec le code source et
# publiés par `dotnet publish` dans /app/publish/wwwroot, avec le manifeste des static web
# assets utilisé par MapStaticAssets : rien à copier à part.
# L'adresse de la passerelle (services__gateway__http__0) et ASPNETCORE_ENVIRONMENT sont
# fournies à l'exécution.

# ---- Étape 1 : build (SDK .NET 10) ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Les .csproj d'abord : Docker met le restore en cache tant qu'aucun .csproj ne change.
COPY src/ECommerce.Web/ECommerce.Web.csproj src/ECommerce.Web/
COPY src/ECommerce.ServiceDefaults/ECommerce.ServiceDefaults.csproj src/ECommerce.ServiceDefaults/
RUN dotnet restore src/ECommerce.Web/ECommerce.Web.csproj

# Le code source, wwwroot compris.
COPY src/ECommerce.Web/ src/ECommerce.Web/
COPY src/ECommerce.ServiceDefaults/ src/ECommerce.ServiceDefaults/
RUN dotnet publish src/ECommerce.Web/ECommerce.Web.csproj \
    --configuration Release --no-restore -o /app/publish

# ---- Étape 2 : image finale (runtime chiselé, non-root) ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS final
WORKDIR /app
COPY --from=build /app/publish .

# $APP_UID (1654) est fourni par l'image .NET : le processus ne tourne jamais en root.
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "ECommerce.Web.dll"]
