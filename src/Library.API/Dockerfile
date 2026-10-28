FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copia os arquivos de projeto para cache eficiente do dotnet restore
COPY ["Library.API/Library.API.csproj", "Library.API/"]
COPY ["Library.Application/Library.Application.csproj", "Library.Application/"]
COPY ["Library.Domain/Library.Domain.csproj", "Library.Domain/"]
COPY ["Library.Infra.EF/Library.Infra.EF.csproj", "Library.Infra.EF/"]
COPY ["Library.Infra.Redis/Library.Infra.Redis.csproj", "Library.Infra.Redis/"]

RUN dotnet restore "Library.API/Library.API.csproj"

# Copia todo o código fonte e compila
COPY . .
WORKDIR "/src/Library.API"
RUN dotnet build "Library.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Library.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Library.API.dll"]
