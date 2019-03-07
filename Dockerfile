FROM microsoft/dotnet:3.0.100-preview3-sdk
COPY . /app
WORKDIR /app/RazorComponentsApp1

ENV ASPNETCORE_URLS http://*:5000

ENTRYPOINT ["dotnet", "run"]
