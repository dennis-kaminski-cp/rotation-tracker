FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/RotationTracker/RotationTracker.csproj src/RotationTracker/
RUN dotnet restore src/RotationTracker/RotationTracker.csproj
COPY src/RotationTracker/ src/RotationTracker/
# Restore again: the SDK adds the Blazor framework assets (blazor.web.js) only once it sees the .razor sources.
RUN dotnet publish src/RotationTracker/RotationTracker.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "RotationTracker.dll"]
