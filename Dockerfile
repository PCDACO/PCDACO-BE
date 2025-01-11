# Use the official Microsoft ASP.NET Core runtime as a base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Use the official Microsoft .NET SDK as a build image
FROM mcr.microsoft.com/dotnet/sdk:8.0.404-alpine3.20-amd64 AS build
WORKDIR /src

# # Copy project files and restore dependencies (optimize caching)
# COPY ["src/API/API.csproj", "API/"]
# COPY ["src/Domain/Domain.csproj", "Domain/"]
# COPY ["src/Infrastructure/Infrastructure.csproj", "Infrastructure/"]
# COPY ["src/UseCases/UseCases.csproj", "UseCases/"]
# COPY ["src/Persistance/Persistance.csproj", "Persistance/"]

# # # Restore dependencies
# RUN dotnet restore "/src/API/API.csproj"

# Copy the rest of the source files
COPY /src /src

WORKDIR /src
# Build the project
RUN dotnet build "API/API.csproj" -c Release

# Publish the project
RUN dotnet publish "API/API.csproj" -c Release --no-restore -o /app/publish

# Use the runtime image for the final stage
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

# Set the entry point for the container
ENTRYPOINT ["dotnet", "API.dll"]
