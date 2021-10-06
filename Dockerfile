# syntax=docker/dockerfile:1
FROM mono:latest

ENV DOCFX_VER 2.58.4

RUN apt-get update && apt-get install unzip wget git -y && wget -q -P /tmp https://github.com/dotnet/docfx/releases/download/v${DOCFX_VER}/docfx.zip && \
    mkdir -p /opt/docfx && \
    unzip /tmp/docfx.zip -d /opt/docfx && \
    echo '#!/bin/bash\nmono /opt/docfx/docfx.exe $@' > /usr/bin/docfx && \
    chmod +x /usr/bin/docfx && \
    rm -rf /tmp/*

COPY Radzen.Blazor /app/Radzen.Blazor
COPY Radzen.DocFX /app/Radzen.DocFX
COPY RadzenBlazorDemos /app/RadzenBlazorDemos
WORKDIR /app
RUN docfx Radzen.DocFX/docfx.json

FROM mcr.microsoft.com/dotnet/sdk:5.0-focal

COPY --from=0 /app/RadzenBlazorDemos /app
WORKDIR /app
RUN dotnet publish -c Release -o out
COPY RadzenBlazorDemos/northwind.db /app/out
COPY RadzenBlazorDemos/northwind.sql /app/out

ENV ASPNETCORE_URLS http://*:5000
WORKDIR /app/out

ENTRYPOINT ["dotnet", "RadzenBlazorDemos.dll"]
