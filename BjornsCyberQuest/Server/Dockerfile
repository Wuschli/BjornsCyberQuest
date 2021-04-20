#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["BjornsCyberQuest/Server/BjornsCyberQuest.Server.csproj", "BjornsCyberQuest/Server/"]
COPY ["BjornsCyberQuest/Shared/BjornsCyberQuest.Shared.csproj", "BjornsCyberQuest/Shared/"]
COPY ["BjornsCyberQuest/Client/BjornsCyberQuest.Client.csproj", "BjornsCyberQuest/Client/"]
RUN dotnet restore "BjornsCyberQuest/Server/BjornsCyberQuest.Server.csproj"
COPY . .
WORKDIR "/src/BjornsCyberQuest/Server"
RUN dotnet build "BjornsCyberQuest.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BjornsCyberQuest.Server.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BjornsCyberQuest.Server.dll"]
