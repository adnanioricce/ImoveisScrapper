FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /app

COPY ./Scrapper.Giraffe.Api.fsproj ./
RUN dotnet restore

COPY . ./

RUN dotnet publish -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:9.0

WORKDIR /app

COPY --from=build /out .

EXPOSE 8090

ENV DB_CONNECTION_STRING="Host=imscrapperdb;Database=imscrapperdb;Username=imscrapper;Password=impassword"

ENTRYPOINT ["dotnet", "Scrapper.Giraffe.Api.App.dll"]