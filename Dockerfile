# Multi-stage Dockerfile for InventoryHub

# Stage 1: Build ServerApp
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-server
WORKDIR /src

# Copy and restore ServerApp dependencies
COPY FullStackApp/ServerApp/ServerApp.csproj FullStackApp/ServerApp/
COPY FullStackApp/SharedModels/SharedModels.csproj FullStackApp/SharedModels/
RUN dotnet restore FullStackApp/ServerApp/ServerApp.csproj

# Copy source and build
COPY FullStackApp/ServerApp/ FullStackApp/ServerApp/
COPY FullStackApp/SharedModels/ FullStackApp/SharedModels/
RUN dotnet build FullStackApp/ServerApp/ServerApp.csproj -c Release -o /app/build
RUN dotnet publish FullStackApp/ServerApp/ServerApp.csproj -c Release -o /app/publish

# Stage 2: Build ClientApp
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-client
WORKDIR /src

# Copy and restore ClientApp dependencies
COPY FullStackApp/ClientApp/ClientApp.csproj FullStackApp/ClientApp/
COPY FullStackApp/SharedModels/SharedModels.csproj FullStackApp/SharedModels/
RUN dotnet restore FullStackApp/ClientApp/ClientApp.csproj

# Copy source and build
COPY FullStackApp/ClientApp/ FullStackApp/ClientApp/
COPY FullStackApp/SharedModels/ FullStackApp/SharedModels/
RUN dotnet build FullStackApp/ClientApp/ClientApp.csproj -c Release -o /app/build
RUN dotnet publish FullStackApp/ClientApp/ClientApp.csproj -c Release -o /app/publish

# Stage 3: Runtime for ServerApp
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime-server
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Copy published ServerApp
COPY --from=build-server /app/publish .

# Create logs directory
RUN mkdir -p /app/logs

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "ServerApp.dll"]

# Stage 4: Runtime for ClientApp (Nginx)
FROM nginx:alpine AS runtime-client
WORKDIR /usr/share/nginx/html

# Copy published ClientApp
COPY --from=build-client /app/publish/wwwroot .

# Copy nginx configuration
COPY nginx.conf /etc/nginx/nginx.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
