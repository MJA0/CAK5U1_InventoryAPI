# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY CloudNativeInventory.Api/CloudNativeInventory.Api.csproj CloudNativeInventory.Api/
RUN dotnet restore CloudNativeInventory.Api/CloudNativeInventory.Api.csproj

COPY CloudNativeInventory.Api/ CloudNativeInventory.Api/
RUN dotnet publish CloudNativeInventory.Api/CloudNativeInventory.Api.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

RUN adduser --disabled-password --gecos "" appuser
USER appuser

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "CloudNativeInventory.Api.dll"]
