# StrategyRuleService API Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5184

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Api/Api.csproj", "Api/"]
COPY ["Application/Application.csproj", "Application/"]
COPY ["Infrastructure/Infrastructure.csproj", "Infrastructure/"]
COPY ["Persistance/Persistance.csproj", "Persistance/"]
COPY ["Domain/Domain.csproj", "Domain/"]
COPY ["Core/Core.csproj", "Core/"]
RUN dotnet restore "Api/Api.csproj"
COPY Api/ Api/
COPY Application/ Application/
COPY Infrastructure/ Infrastructure/
COPY Persistance/ Persistance/
COPY Domain/ Domain/
COPY Core/ Core/
WORKDIR "/src/Api"
RUN dotnet build "Api.csproj" -c Release -o /app/build
RUN dotnet publish "Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Api.dll"]

