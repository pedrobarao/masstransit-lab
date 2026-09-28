FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Contracts/Contracts.csproj Contracts/
COPY Publisher/Publisher.csproj Publisher/
RUN dotnet restore Publisher/Publisher.csproj
COPY Contracts/ Contracts/
COPY Publisher/ Publisher/
RUN dotnet publish Publisher/Publisher.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Publisher.dll"]
