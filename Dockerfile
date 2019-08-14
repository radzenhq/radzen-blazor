FROM mcr.microsoft.com/dotnet/core/sdk:3.0.100-preview8
COPY . /app
WORKDIR /app

ENV ASPNETCORE_URLS http://*:5000
 
ENTRYPOINT ["dotnet", "run"]
