#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
# skip the download of chromium
ENV PUPPETEER_SKIP_CHROMIUM_DOWNLOAD true
ENV IMOVEISSCRAPPER_WEBENGINE_PATH /usr/bin/google-chrome
ENV IMOVEISSCRAPPER_CONNECTION_STRING "User ID=postgres;Password=postgres;Host=172.17.0.1;Port=5433;Database=imoveisdb;Pooling=true;"
#ENV IMOVEISSCRAPPER_WEBENGINE_PATH /usr/bin/chromium-browser


WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Scrapper.Worker/Scrapper.Worker.fsproj", "Scrapper.Worker/"]
RUN dotnet restore "Scrapper.Worker/Scrapper.Worker.fsproj"
COPY . .
WORKDIR "/src/Scrapper.Worker"
RUN dotnet build "Scrapper.Worker.fsproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Scrapper.Worker.fsproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
RUN apt-get update && apt-get install gnupg wget -y && \
  wget --quiet --output-document=- https://dl-ssl.google.com/linux/linux_signing_key.pub | gpg --dearmor > /etc/apt/trusted.gpg.d/google-archive.gpg && \
  sh -c 'echo "deb [arch=amd64] http://dl.google.com/linux/chrome/deb/ stable main" >> /etc/apt/sources.list.d/google.list' && \
  apt-get update && \
  apt-get install google-chrome-stable -y --no-install-recommends && \
  rm -rf /var/lib/apt/lists/* \
RUN dotnet dev-certs https
#RUN curl -LO https://dl.google.com/linux/direct/google-chrome-stable_current_amd64.deb
#RUN apt-get install -y ./google-chrome-stable_current_amd64.deb
#RUN rm google-chrome-stable_current_amd64.deb 
ENTRYPOINT ["dotnet", "Scrapper.Worker.dll"]