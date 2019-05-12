FROM mcr.microsoft.com/dotnet/core/sdk:3.0.100-preview5
COPY . /app
WORKDIR /app

ENV ASPNETCORE_URLS https://*:5001
 
ENTRYPOINT ["dotnet", "run"]
