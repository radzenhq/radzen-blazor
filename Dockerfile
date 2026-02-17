# =============================
# BUILD STAGE
# =============================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first for better caching
COPY Radzen.Blazor/*.csproj Radzen.Blazor/
COPY RadzenBlazorDemos/*.csproj RadzenBlazorDemos/
COPY RadzenBlazorDemos.Host/*.csproj RadzenBlazorDemos.Host/
COPY RadzenBlazorDemos.Tools/*.csproj RadzenBlazorDemos.Tools/

# Radzen.DocFX usually has no csproj → copy full folder
COPY Radzen.DocFX/ Radzen.DocFX/

# Restore dependencies (Host + Tools for llms.txt generation)
RUN dotnet restore RadzenBlazorDemos.Host/RadzenBlazorDemos.Host.csproj \
 && dotnet restore RadzenBlazorDemos.Tools/RadzenBlazorDemos.Tools.csproj

# Copy full source after restore layer
COPY . .

# Install docfx (build stage only)
RUN dotnet tool install -g docfx
ENV PATH="$PATH:/root/.dotnet/tools"

# Build shared project (keep net8.0 if required)
RUN dotnet build -c Release Radzen.Blazor/Radzen.Blazor.csproj -f net8.0

# Generate documentation
RUN docfx Radzen.DocFX/docfx.json

# Publish the Blazor host app
WORKDIR /src/RadzenBlazorDemos.Host
RUN dotnet publish -c Release -o /app/out


# =============================
# RUNTIME STAGE
# =============================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Copy only published output
COPY --from=build /app/out ./

# Set runtime URL
ENV ASPNETCORE_URLS=http://+:5000

ENTRYPOINT ["dotnet", "RadzenBlazorDemos.Host.dll"]
