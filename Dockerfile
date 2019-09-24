FROM mcr.microsoft.com/dotnet/core/sdk:3.0.100
COPY . /app
WORKDIR /app
RUN dotnet publish -c Release -o out
COPY ./northwind.db /app/out

ENV ASPNETCORE_URLS http://*:5000
WORKDIR /app/out

ENTRYPOINT ["dotnet", "LatestBlazor.dll"]
