FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/CVAnalyzer.API/CVAnalyzer.API.csproj", "src/CVAnalyzer.API/"]
COPY ["src/CVAnalyzer.Application/CVAnalyzer.Application.csproj", "src/CVAnalyzer.Application/"]
COPY ["src/CVAnalyzer.Domain/CVAnalyzer.Domain.csproj", "src/CVAnalyzer.Domain/"]
COPY ["src/CVAnalyzer.Infrastructure/CVAnalyzer.Infrastructure.csproj", "src/CVAnalyzer.Infrastructure/"]
COPY ["src/CVAnalyzer.AgentService/CVAnalyzer.AgentService.csproj", "src/CVAnalyzer.AgentService/"]
RUN dotnet restore "src/CVAnalyzer.API/CVAnalyzer.API.csproj"

COPY . .
WORKDIR "/src/src/CVAnalyzer.API"
RUN dotnet build "CVAnalyzer.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "CVAnalyzer.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

ENTRYPOINT ["dotnet", "CVAnalyzer.API.dll"]
