FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_EnableDiagnostics=0

EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

ARG BUILD_CONFIGURATION=Release

WORKDIR /src

COPY InteresMe.API.csproj ./
RUN dotnet restore ./InteresMe.API.csproj

COPY . ./
RUN dotnet publish ./InteresMe.API.csproj \
    --configuration $BUILD_CONFIGURATION \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM runtime AS final

COPY --from=build /app/publish ./

ENTRYPOINT ["dotnet", "InteresMe.API.dll"]
