
    FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
    WORKDIR /source
    
    # Copy solution and project files first for layer caching
    # Assuming HetroClothingClient is a standalone project or part of a solution
    # If part of a larger solution, copy the .sln and all relevant .csproj files
    # For simplicity, let's assume it's built as a standalone project for this Dockerfile
    COPY HetroClothingClient.csproj .
    # If you have other library projects referenced by HetroClothingClient, copy their .csproj too
    # COPY ../PathToLibrary/Library.csproj ../PathToLibrary/
    
    # Restore dependencies for HetroClothingClient
    RUN dotnet restore HetroClothingClient.csproj
    
    # Copy the rest of the HetroClothingClient source code
    COPY . .
    
    # Build and publish the application for release
    # The WORKDIR should be where the HetroClothingClient.csproj is
    WORKDIR /source
    RUN dotnet publish HetroClothingClient.csproj -c Release -o /app/publish --no-restore
    
    # ---- Runtime Stage ----
    FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
    WORKDIR /app
    
    # Optional: Create a non-root user for security
    # RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
    # USER appuser
    
    # Copy published output from the build stage
    COPY --from=build /app/publish .
    
    # SQLite database file will be created inside the container in /app if Data Source is relative like "hetro_client.db"
    # If you want to persist it, you'll need to mount a volume for it in docker-compose.
    
    # Expose the port the app listens on (HTTP, as TLS is typically handled by a reverse proxy)
    EXPOSE 8080
    
    # Entry point to run the application DLL
    ENTRYPOINT ["dotnet", "HetroClothingClient.dll"]