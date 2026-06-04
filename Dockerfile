FROM mcr.microsoft.com/dotnet/sdk:10.0

EXPOSE 7373

ARG UUID=1001
ARG GUID=1001
RUN groupadd -g $GUID -o karaweb
RUN useradd -m -u $UUID -g $GUID -o -s /bin/bash karaweb

RUN mkdir /app
RUN chown karaweb:karaweb /app

USER karaweb:karaweb

RUN mkdir /home/karaweb/src

RUN mkdir /home/karaweb/src/Back
WORKDIR /home/karaweb/src/Back
COPY Host/* .
RUN dotnet build --configuration Release

WORKDIR /app
RUN mv /home/mediasaccess/src/bin/Release/* ./

RUN rm -r /home/karaweb/src

ENTRYPOINT ["dotnet", "run", "KaraWeb.dll"]
