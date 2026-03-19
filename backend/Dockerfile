FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY DigitalAmnesia.Backend.csproj ./
RUN dotnet restore DigitalAmnesia.Backend.csproj

COPY . .
RUN dotnet publish DigitalAmnesia.Backend.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
USER root

# Keep the writable job store at a stable path for volume mounts on hosted demos.
RUN mkdir -p /app/data

COPY --from=build /app/publish .

ENV PORT=3001
EXPOSE 3001

ENTRYPOINT ["dotnet", "DigitalAmnesia.Backend.dll"]
