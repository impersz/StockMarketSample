# Use the official .NET SDK image to build and publish the project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env

# Install curl using apt
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*


WORKDIR /app

# Copy the csproj and restore as distinct layers
COPY *.csproj ./
RUN dotnet restore

# Copy the entire project and build the release
COPY . ./
RUN dotnet publish -c Release -o out

# Build a runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build-env /app/out .

# Expose the port for the API
EXPOSE 80

# Set the entry point for the container
ENTRYPOINT ["dotnet", "OrderService.dll"]
