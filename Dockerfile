# =============================
# BUILD STAGE
# =============================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first for better caching
COPY Radzen.Blazor/*.csproj Radzen.Blazor/
COPY Radzen.Blazor.Api/*.csproj Radzen.Blazor.Api/
COPY Radzen.Blazor.Api.Generator/*.csproj Radzen.Blazor.Api.Generator/
COPY RadzenBlazorDemos/*.csproj RadzenBlazorDemos/
COPY RadzenBlazorDemos.Host/*.csproj RadzenBlazorDemos.Host/
COPY RadzenBlazorDemos.Tools/*.csproj RadzenBlazorDemos.Tools/

# Restore dependencies (Host + Tools + API page generator)
RUN dotnet restore RadzenBlazorDemos.Host/RadzenBlazorDemos.Host.csproj \
 && dotnet restore RadzenBlazorDemos.Tools/RadzenBlazorDemos.Tools.csproj \
 && dotnet restore Radzen.Blazor.Api.Generator/Radzen.Blazor.Api.Generator.csproj

# Copy full source after restore layer
COPY . .

# Pre-generate API reference pages (must exist on disk before publish evaluates globs)
RUN dotnet build Radzen.Blazor/Radzen.Blazor.csproj -c Release \
 && dotnet run --project Radzen.Blazor.Api.Generator -- \
      Radzen.Blazor/bin/Release/net10.0/Radzen.Blazor.dll \
      Radzen.Blazor/bin/Release/net10.0/Radzen.Blazor.xml \
      Radzen.Blazor.Api/Generated/Pages

# Publish the Blazor host app (generated pages are now on disk for the SDK to discover)
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
