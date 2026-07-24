FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine3.24

EXPOSE 7373

RUN apk update && apk add ffmpeg libgdiplus

ARG UUID=1001
ARG GUID=1001
RUN addgroup -g $GUID -S KaraW3B && adduser -D -u $UUID -G KaraW3B -s /bin/bash KaraW3B

RUN mkdir /app
COPY bin/Release/ /app/
RUN chown -R KaraW3B:KaraW3B /app

USER $UUID:$GUID
WORKDIR /app
ENTRYPOINT ["dotnet", "KaraW3B.Server.Songs.Host.dll"]
