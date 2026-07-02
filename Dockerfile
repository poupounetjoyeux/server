FROM mcr.microsoft.com/dotnet/aspnet:10.0

EXPOSE 7373

ARG UUID=1001
ARG GUID=1001
RUN groupadd -g $GUID -o KaraW3B
RUN useradd -m -u $UUID -g $GUID -o -s /bin/bash KaraW3B

RUN mkdir /app
COPY bin/Release/ /app/
RUN chown -R KaraW3B:KaraW3B /app

USER $UUID:$GUID
WORKDIR /app
ENTRYPOINT ["dotnet", "KaraW3B.Server.Host.dll"]
