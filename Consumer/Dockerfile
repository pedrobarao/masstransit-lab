FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Contracts/Contracts.csproj Contracts/
COPY Consumer/Consumer.csproj Consumer/
RUN dotnet restore Consumer/Consumer.csproj
COPY Contracts/ Contracts/
COPY Consumer/ Consumer/
RUN dotnet publish Consumer/Consumer.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Consumer.dll"]
