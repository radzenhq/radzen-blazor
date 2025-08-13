# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:9.0

COPY Radzen.Blazor /app/Radzen.Blazor
COPY Radzen.DocFX /app/Radzen.DocFX
COPY RadzenBlazorDemos /app/RadzenBlazorDemos
COPY RadzenBlazorDemos.Host /app/RadzenBlazorDemos.Host

WORKDIR /app

RUN dotnet tool install -g docfx
ENV PATH="$PATH:/root/.dotnet/tools"
RUN wget https://dot.net/v1/dotnet-install.sh \
    && bash dotnet-install.sh --channel 8.0 --runtime dotnet --install-dir /usr/share/dotnet
RUN dotnet build -c Release Radzen.Blazor/Radzen.Blazor.csproj -f net8.0
RUN docfx Radzen.DocFX/docfx.json

WORKDIR /app/RadzenBlazorDemos.Host
RUN dotnet publish -c Release -o out

ENV ASPNETCORE_URLS=http://*:5000
WORKDIR /app/RadzenBlazorDemos.Host/out

ENTRYPOINT ["dotnet", "RadzenBlazorDemos.Host.dll"]
