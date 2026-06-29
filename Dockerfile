FROM mcr.microsoft.com/dotnet/sdk:10.0

EXPOSE 7373

ARG UUID=1001
ARG GUID=1001
RUN groupadd -g $GUID -o KaraWeb
RUN useradd -m -u $UUID -g $GUID -o -s /bin/bash KaraWeb

COPY Back/ /KaraWebSrc/Back/
WORKDIR /KaraWebSrc/Back
RUN dotnet build --configuration Release

RUN mkdir /app
WORKDIR /app
RUN mv /KaraWebSrc/bin/Release/* ./
RUN chown -R KaraWeb:KaraWeb /app

RUN rm -r /KaraWebSrc

USER $UUID:$GUID
ENTRYPOINT ["dotnet", "run", "KaraWeb.dll"]
