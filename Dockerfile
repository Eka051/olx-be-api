FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["olx-be-api.csproj", "."]
RUN dotnet restore "olx-be-api.csproj"

COPY . .
WORKDIR "/src"
RUN dotnet build "olx-be-api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "olx-be-api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "olx-be-api.dll"]
