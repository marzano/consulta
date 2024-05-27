FROM registrya.azurecr.io/dotnet/sdk:6.0 as build

RUN wget -qO- https://raw.githubusercontent.com/Microsoft/artifacts-credprovider/master/helpers/installcredprovider.sh | bash
WORKDIR /app
COPY ./src/SPTrans.StatusCartaoPersonalizado .

ENV USE_NET6_ARTIFACTS_CREDENTIAL_PROVIDER=true
ENV DOTNET_SYSTEM_NET_HTTP_USESOCKETSHTTPHANDLER=0
ENV NUGET_CREDENTIALPROVIDER_SESSIONTOKENCACHE_ENABLED true
ENV VSS_NUGET_EXTERNAL_FEED_ENDPOINTS #{DOCKER_VSS_NUGET_EXTERNAL_FEED_ENDPOINTS}

RUN dotnet restore -s https://api.nuget.org/v3/index.json
RUN dotnet publish SPTrans.StatusCartaoPersonalizado.csproj -c Release -o /app/output

FROM registrya.azurecr.io/dotnet/aspnet:6.0
RUN sed -i 's/DEFAULT@SECLEVEL=2/DEFAULT@SECLEVEL=1/g' /etc/ssl/openssl.cnf
RUN sed -i 's/MinProtocol = TLSv1.2/MinProtocol = TLSv1/g' /etc/ssl/openssl.cnf
RUN sed -i 's/DEFAULT@SECLEVEL=2/DEFAULT@SECLEVEL=1/g' /usr/lib/ssl/openssl.cnf
RUN sed -i 's/MinProtocol = TLSv1.2/MinProtocol = TLSv1/g' /usr/lib/ssl/openssl.cnf
WORKDIR /app
COPY --from=build /app/output .
ENV TZ America/Sao_Paulo

ENTRYPOINT ["dotnet", "SPTrans.StatusCartaoPersonalizado.dll"]

# Install latest Chrome
RUN CHROME_URL=$(curl -s https://googlechromelabs.github.io/chrome-for-testing/last-known-good-versions-with-downloads.json | jq -r '.channels.Stable.downloads.chrome[] | select(.platform == "linux64") | .url') \
    && curl -sSLf --retry 3 --output /tmp/chrome-linux64.zip "https://edgedl.me.gvt1.com/edgedl/chrome/chrome-for-testing/116.0.5845.96/linux64/chrome-linux64.zip" \
    && unzip /tmp/chrome-linux64.zip -d /opt \
    && ln -s /opt/chrome-linux64/chrome /usr/local/bin/chrome \
    && rm /tmp/chrome-linux64.zip	
