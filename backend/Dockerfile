# Stage 1: build
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src
COPY *.slnx .
COPY src/MinimalAPI.Domain/*.csproj src/MinimalAPI.Domain/
COPY src/MinimalAPI.Application/*.csproj src/MinimalAPI.Application/
COPY src/MinimalAPI.Infrastructure/*.csproj src/MinimalAPI.Infrastructure/
COPY src/MinimalAPI.Api/*.csproj src/MinimalAPI.Api/
RUN dotnet restore
COPY . .
RUN dotnet publish src/MinimalAPI.Api -c Release -o /app/publish

# Stage 2: runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1
USER $APP_UID
ENTRYPOINT ["dotnet", "MinimalAPI.Api.dll"]
