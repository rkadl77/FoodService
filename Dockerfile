FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5621

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["hitsApplication.sln", "./"]
COPY ["hitsApplication/hitsApplication.csproj", "hitsApplication/"]
RUN dotnet restore "hitsApplication.sln"
COPY . .
WORKDIR "/src/hitsApplication"
RUN dotnet build "hitsApplication.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "hitsApplication.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "hitsApplication.dll"]
