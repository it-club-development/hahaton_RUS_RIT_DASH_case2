FROM public.ecr.aws/docker/library/node:24-bookworm-slim AS frontend
WORKDIR /src/frontend
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci
COPY frontend/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
WORKDIR /src
COPY WebOtchet/WebOtchet.csproj WebOtchet/
RUN dotnet restore WebOtchet/WebOtchet.csproj
COPY WebOtchet/ WebOtchet/
COPY --from=frontend /src/frontend/dist/ WebOtchet/wwwroot/
RUN dotnet publish WebOtchet/WebOtchet.csproj -c Release --no-restore -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble
WORKDIR /app
COPY --from=build /out/ ./
RUN mkdir -p /app/keys && chown app:app /app/keys
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "WebOtchet.dll"]
