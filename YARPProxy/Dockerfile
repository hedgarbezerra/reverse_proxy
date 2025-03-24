# Acesse https://aka.ms/customizecontainer para saber como personalizar seu contêiner de depuração e como o Visual Studio usa este Dockerfile para criar suas imagens para uma depuração mais rápida.

# Esta fase é usada durante a execução no VS no modo rápido (Padrão para a configuração de Depuração)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

ENV ASPNETCORE_URLS=http://+:80;http://+:8080;https://+:443;https://+:8081
ENV HTTPS_PORT=8081
#Add the following lines to the Dockerfile to enable HTTPS on Unix

VOLUME /etc/certificates
COPY ../Certificates /etc/certificates/
ENV ASPNETCORE_Kestrel__Certificates__Default__Password=123
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/etc/certificates/aspnetapp.pfx


# Esta fase é usada para compilar o projeto de serviço
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["YARPProxy/YARPProxy.csproj", "YARPProxy/"]
COPY ["Common/Common.csproj", "Common/"]
RUN dotnet restore "./YARPProxy/YARPProxy.csproj"
COPY . .
WORKDIR "/src/YARPProxy"
RUN dotnet build "./YARPProxy.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Esta fase é usada para publicar o projeto de serviço a ser copiado para a fase final
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./YARPProxy.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Esta fase é usada na produção ou quando executada no VS no modo normal (padrão quando não está usando a configuração de Depuração)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "YARPProxy.dll"]