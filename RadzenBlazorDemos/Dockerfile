FROM mcr.microsoft.com/dotnet/sdk:5.0
COPY . /app
WORKDIR /app
RUN dotnet publish -c Release -o out
COPY ./northwind.db /app/out
COPY ./northwind.sql /app/out

ENV ASPNETCORE_URLS http://*:5000
WORKDIR /app/out

ENTRYPOINT ["dotnet", "RadzenBlazorDemos.dll"]
