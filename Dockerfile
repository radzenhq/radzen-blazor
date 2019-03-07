FROM microsoft/dotnet:3.0-sdk
COPY . /app
WORKDIR /app/RazorComponentsApp1

RUN ["dotnet", "clean"]
RUN ["dotnet", "restore"]
RUN ["dotnet", "build"]

ENV ASPNETCORE_URLS http://*:5000
 
ENTRYPOINT ["dotnet", "run"]
