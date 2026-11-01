# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json nlwc-ems.slnx ./
COPY nlwc-ems.Base/nlwc-ems.Base.csproj nlwc-ems.Base/
COPY nlwc-ems.api/nlwc-ems.api.csproj nlwc-ems.api/
COPY nlwc-ems.Tests/nlwc-ems.Tests.csproj nlwc-ems.Tests/
RUN dotnet restore nlwc-ems.api/nlwc-ems.api.csproj
COPY nlwc-ems.Base nlwc-ems.Base
COPY nlwc-ems.api nlwc-ems.api
RUN dotnet publish nlwc-ems.api/nlwc-ems.api.csproj -c Release --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "nlwc.ems.api.dll"]
