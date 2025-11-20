FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 5005

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

# Copiar archivos de proyecto
COPY ["UserFeed.Api/UserFeed.Api.csproj", "UserFeed.Api/"]
COPY ["UserFeed.Application/UserFeed.Application.csproj", "UserFeed.Application/"]
COPY ["UserFeed.Domain/UserFeed.Domain.csproj", "UserFeed.Domain/"]
COPY ["UserFeed.Infrastructure/UserFeed.Infrastructure.csproj", "UserFeed.Infrastructure/"]

# Restaurar dependencias
RUN dotnet restore "UserFeed.Api/UserFeed.Api.csproj"

# Copiar todo el código
COPY . .

# Build
WORKDIR "/src/UserFeed.Api"
RUN dotnet build "UserFeed.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "UserFeed.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://+:5005
ENTRYPOINT ["dotnet", "UserFeed.Api.dll"]
