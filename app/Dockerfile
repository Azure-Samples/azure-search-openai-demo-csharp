#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 443

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]
COPY ["NuGet.config", "."]
COPY ["backend/", "backend/"]
COPY ["frontend/", "frontend/"]
COPY ["shared/", "shared/"]
COPY ["SharedWebComponents", "SharedWebComponents/"]
RUN dotnet restore "backend/MinimalApi.csproj"

WORKDIR "/src/backend"
FROM build AS publish
RUN dotnet publish "MinimalApi.csproj" -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "MinimalApi.dll"]