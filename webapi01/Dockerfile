FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

VOLUME /etc/certificates
COPY ../Certificates /etc/certificates/

ENV ASPNETCORE_URLS=http://+:80;http://+:8080;https://+:443;https://+:8081
ENV ASPNETCORE_Kestrel__Certificates__Default__Password=123
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/etc/certificates/aspnetapp.pfx

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["webapi01/webapi01.csproj", "webapi01/"]
COPY ["Common/Common.csproj", "Common/"]
RUN dotnet restore "webapi01/webapi01.csproj"
COPY . .
WORKDIR "/src/webapi01"
RUN dotnet build "webapi01.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "webapi01.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "webapi01.dll"]
