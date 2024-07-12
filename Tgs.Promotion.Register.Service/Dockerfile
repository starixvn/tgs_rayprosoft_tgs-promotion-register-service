#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Tgs.Promotion.Register.Service/Tgs.Promotion.Register.Service.csproj", "Tgs.Promotion.Register.Service/"]
COPY ["Tgs.Promotion.Register.Core/Tgs.Promotion.Register.Core.csproj", "Tgs.Promotion.Register.Core/"]
COPY ["Tgs.Promotion.Register.Repository/Tgs.Promotion.Register.Repository.csproj", "Tgs.Promotion.Register.Repository/"]
RUN dotnet restore "./Tgs.Promotion.Register.Service/Tgs.Promotion.Register.Service.csproj"
COPY . .
WORKDIR "/src/Tgs.Promotion.Register.Service"
RUN dotnet build "./Tgs.Promotion.Register.Service.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Tgs.Promotion.Register.Service.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Tgs.Promotion.Register.Service.dll"]