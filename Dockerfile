# Stage 1: Restore .NET dependencies
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props nuget.config global.json ./
COPY src/Domain/Domain.csproj src/Domain/
COPY src/Application/Application.csproj src/Application/
COPY src/Infrastructure/Infrastructure.csproj src/Infrastructure/
COPY src/Web/Web.csproj src/Web/

RUN dotnet restore src/Web/Web.csproj

# Stage 2: Build .NET backend
FROM restore AS build
WORKDIR /src

COPY src/ src/
RUN dotnet build src/Web/Web.csproj -c Release --no-restore

# Stage 3: Build Angular frontend
FROM node:24-alpine AS frontend
WORKDIR /app

COPY src/Web/ClientApp/package.json src/Web/ClientApp/package-lock.json ./
RUN npm ci

COPY src/Web/ClientApp/ ./
RUN npm run build -- --configuration production

# Stage 4: Publish .NET app (skip SPA publish — frontend built in separate stage)
FROM build AS publish
WORKDIR /src

RUN dotnet publish src/Web/Web.csproj -c Release --no-build /p:SkipSpaPublish=true -o /app/publish

# Copy Angular dist into wwwroot
COPY --from=frontend /app/dist/browser/ /app/publish/wwwroot/

# Stage 5: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Create uploads directory
RUN mkdir -p /app/uploads

COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SimpleChat.Web.dll"]
