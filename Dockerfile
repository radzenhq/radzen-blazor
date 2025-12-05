# ============= BUILD STAGE =============
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first (better layer caching)
COPY Radzen.Blazor/*.csproj Radzen.Blazor/
COPY Radzen.DocFX/*.csproj Radzen.DocFX/  # if any
COPY RadzenBlazorDemos/*.csproj RadzenBlazorDemos/
COPY RadzenBlazorDemos.Host/*.csproj RadzenBlazorDemos.Host/

# Restore using the host app as the entry point
RUN dotnet restore RadzenBlazorDemos.Host/RadzenBlazorDemos.Host.csproj

# Now copy the rest of the source
COPY . .

# Install docfx in the *build* stage only
RUN dotnet tool install -g docfx
ENV PATH="$PATH:/root/.dotnet/tools"

# Build Radzen.Blazor if you need it as a separate step
# You can keep targeting net8.0 (or change to net10.0 if your TFM is net10.0)
RUN dotnet build -c Release Radzen.Blazor/Radzen.Blazor.csproj -f net8.0

# Generate docs (if needed in the image)
RUN docfx Radzen.DocFX/docfx.json

# Publish the host app
WORKDIR /src/RadzenBlazorDemos.Host
RUN dotnet publish -c Release -o /app/out


# ============= RUNTIME STAGE =============
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Copy published output from build stage
COPY --from=build /app/out ./

ENV ASPNETCORE_URLS=http://+:5000

ENTRYPOINT ["dotnet", "RadzenBlazorDemos.Host.dll"]
