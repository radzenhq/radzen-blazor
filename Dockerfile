# =============================
# BUILD STAGE
# =============================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# The SDK image sets NUGET_XMLDOC_MODE=skip; the API page generator needs Radzen.Blazor.xml from the
# restored package, so re-enable XML doc extraction during restore.
ENV NUGET_XMLDOC_MODE=none

# Copy project files first for better caching
COPY Radzen.Blazor/*.csproj Radzen.Blazor/
COPY Radzen.Blazor.Api/*.csproj Radzen.Blazor.Api/
COPY Radzen.Blazor.Api.Generator/*.csproj Radzen.Blazor.Api.Generator/
COPY RadzenBlazorDemos/*.csproj RadzenBlazorDemos/
COPY RadzenBlazorDemos.Host/*.csproj RadzenBlazorDemos.Host/
COPY RadzenBlazorDemos.Tools/*.csproj RadzenBlazorDemos.Tools/

# Restore for Release so the Radzen.Blazor NuGet package (referenced only in Release) is downloaded
# for both the publish below and the API page generator.
RUN dotnet restore RadzenBlazorDemos.Host/RadzenBlazorDemos.Host.csproj -p:Configuration=Release \
 && dotnet restore RadzenBlazorDemos.Tools/RadzenBlazorDemos.Tools.csproj \
 && dotnet restore Radzen.Blazor.Api.Generator/Radzen.Blazor.Api.Generator.csproj

# Copy full source after restore layer
COPY . .

# Pre-generate API reference pages from the published Radzen.Blazor package - the deployed site uses that
# same package, so there is no need to compile Radzen.Blazor (and its Sass/JS assets) from source.
# Pages must exist on disk before publish evaluates the Api project's Razor globs.
RUN RADZEN_DLL=$(find /root/.nuget/packages/radzen.blazor -path '*/lib/net10.0/Radzen.Blazor.dll' | sort -V | tail -1) \
 && test -n "$RADZEN_DLL" \
 && dotnet run --project Radzen.Blazor.Api.Generator -- \
      "$RADZEN_DLL" "${RADZEN_DLL%.dll}.xml" \
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
