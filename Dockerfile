FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
<<<<<<< HEAD
EXPOSE 8080
EXPOSE 8081
=======
EXPOSE 5621
>>>>>>> d25676b2f4062118d24d1c88cb0368d5399ae279

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
