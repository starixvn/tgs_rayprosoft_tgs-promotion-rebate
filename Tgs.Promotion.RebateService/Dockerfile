#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
ARG BuildConfiguration=Release
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["Tgs.Promotion.RebateService/Tgs.Promotion.RebateService.csproj", "Tgs.Promotion.RebateService/"]
COPY ["RebateService.Core/RebateService.Core.csproj", "RebateService.Core/"]
COPY ["RebateService.Repositories/RebateService.Repositories.csproj", "RebateService.Repositories/"]
RUN dotnet restore "Tgs.Promotion.RebateService/Tgs.Promotion.RebateService.csproj"
COPY . .
WORKDIR "/src/Tgs.Promotion.RebateService"
RUN dotnet build "Tgs.Promotion.RebateService.csproj" -c $BuildConfiguration -o /app/build

FROM build AS publish
ARG BuildConfiguration
RUN dotnet publish "Tgs.Promotion.RebateService.csproj" -c $BuildConfiguration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Tgs.Promotion.RebateService.dll"]
