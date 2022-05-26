#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
ARG BuildConfiguration=Release
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["Tgs.Promotion.Register.Service/Tgs.Promotion.Register.Service.csproj", "Tgs.Promotion.Register.Service/"]
COPY ["Tgs.Promotion.Register.Core/Tgs.Promotion.Register.Core.csproj", "Tgs.Promotion.Register.Core/"]
COPY ["Tgs.Promotion.Register.Repository/Tgs.Promotion.Register.Repository.csproj", "Tgs.Promotion.Register.Repository/"]
RUN dotnet restore "Tgs.Promotion.Register.Service/Tgs.Promotion.Register.Service.csproj"
COPY . .
WORKDIR "/src/Tgs.Promotion.Register.Service"
RUN dotnet build "Tgs.Promotion.Register.Service.csproj" -c $BuildConfiguration -o /app/build

FROM build AS publish
ARG BuildConfiguration
RUN dotnet publish "Tgs.Promotion.Register.Service.csproj" -c $BuildConfiguration -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Tgs.Promotion.Register.Service.dll"]