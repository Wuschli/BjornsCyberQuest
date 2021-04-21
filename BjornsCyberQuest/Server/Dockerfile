#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build

WORKDIR /app
# Prevent 'Warning: apt-key output should not be parsed (stdout is not a terminal)'
ENV APT_KEY_DONT_WARN_ON_DANGEROUS_USAGE=1

# install NodeJS 13.x
# see https://github.com/nodesource/distributions/blob/master/README.md#deb
RUN apt-get update -yq 
RUN apt-get install curl gnupg -yq 
RUN curl -sL https://deb.nodesource.com/setup_16.x | bash -
RUN apt-get install -y nodejs

WORKDIR /src
COPY ["BjornsCyberQuest/Server/BjornsCyberQuest.Server.csproj", "BjornsCyberQuest/Server/"]
COPY ["BjornsCyberQuest/Shared/BjornsCyberQuest.Shared.csproj", "BjornsCyberQuest/Shared/"]
COPY ["BjornsCyberQuest/Client/BjornsCyberQuest.Client.csproj", "BjornsCyberQuest/Client/"]
COPY ["XTerm/src/Blazor.Extensions.XTerm/Blazor.Extensions.XTerm.csproj", "XTerm/src/Blazor.Extensions.XTerm/"]
COPY ["XTerm/src/Blazor.Extensions.XTerm.JS/Blazor.Extensions.XTerm.JS.csproj", "XTerm/src/Blazor.Extensions.XTerm.JS/"]
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