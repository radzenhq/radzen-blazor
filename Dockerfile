# syntax=docker/dockerfile:1
FROM mono:latest

ENV DOCFX_VER 2.58.4

COPY Radzen.Blazor /app/Radzen.Blazor
COPY Radzen.DocFX /app/DocFX
COPY RadzenBlazorDemos /app/RadzenBlazorDemos
COPY RadzenBlazorDemos.Host /app/RadzenBlazorDemos.Host
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0

COPY --from=0 /app/RadzenBlazorDemos.Host /app/RadzenBlazorDemos.Host
COPY --from=0 /app/RadzenBlazorDemos /app/RadzenBlazorDemos

WORKDIR /app/RadzenBlazorDemos.Host
RUN dotnet publish -c Release -o out

ENV ASPNETCORE_URLS http://*:5000
WORKDIR /app/RadzenBlazorDemos.Host/out

ENTRYPOINT ["dotnet", "RadzenBlazorDemos.Host.dll"]
