# syntax=docker/dockerfile:1

# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution + csproj first to leverage Docker's layer cache for restore
COPY ResQ.sln ./
COPY ResQ.API/ResQ.API.csproj ./ResQ.API/
RUN dotnet restore ResQ.sln

# Copy the rest of the sources and publish
COPY ResQ.API/ ./ResQ.API/
RUN dotnet publish ResQ.API/ResQ.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Development

EXPOSE 8080
ENTRYPOINT ["dotnet", "ResQ.API.dll"]
