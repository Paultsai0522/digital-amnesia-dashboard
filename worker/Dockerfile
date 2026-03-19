FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY DigitalAmnesia.Worker.csproj ./
RUN dotnet restore DigitalAmnesia.Worker.csproj

COPY . .
RUN dotnet publish DigitalAmnesia.Worker.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "DigitalAmnesia.Worker.dll"]
