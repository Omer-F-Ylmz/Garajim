FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Garajim.Core/Garajim.Core.csproj Garajim.Core/
COPY Garajim.Entity/Garajim.Entity.csproj Garajim.Entity/
COPY Garajim.Dal/Garajim.Dal.csproj Garajim.Dal/
COPY Garajim.Business/Garajim.Business.csproj Garajim.Business/
COPY Garajim.ML/Garajim.ML.csproj Garajim.ML/
COPY Garajim.API/Garajim.API.csproj Garajim.API/
RUN dotnet restore Garajim.API/Garajim.API.csproj -r linux-x64

COPY Garajim.Core/ Garajim.Core/
COPY Garajim.Entity/ Garajim.Entity/
COPY Garajim.Dal/ Garajim.Dal/
COPY Garajim.Business/ Garajim.Business/
COPY Garajim.ML/ Garajim.ML/
COPY Garajim.API/ Garajim.API/
RUN dotnet publish Garajim.API/Garajim.API.csproj -c Release -r linux-x64 --no-restore -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER $APP_UID

ENTRYPOINT ["dotnet", "Garajim.API.dll"]
