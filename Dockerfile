FROM microsoft/dotnet:3.0-sdk
COPY . /app
WORKDIR /app/WebApplication1/WebApplication1.Server

ENV ASPNETCORE_URLS http://*:5000
 
ENTRYPOINT ["dotnet", "run"]
