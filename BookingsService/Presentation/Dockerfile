# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore (layer caching): copy project files first
COPY SharedContracts/SharedContracts.csproj SharedContracts/
COPY BookingsService/Domain/BookingsService.Domain.csproj BookingsService/Domain/
COPY BookingsService/Application/BookingsService.Application.csproj BookingsService/Application/
COPY BookingsService/Infrastructure/BookingsService.Infrastructure.csproj BookingsService/Infrastructure/
COPY BookingsService/Presentation/BookingsService.Presentation.csproj BookingsService/Presentation/
RUN dotnet restore BookingsService/Presentation/BookingsService.Presentation.csproj

# Copy the rest and publish
COPY SharedContracts/ SharedContracts/
COPY BookingsService/ BookingsService/
RUN dotnet publish BookingsService/Presentation/BookingsService.Presentation.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5003
ENV ASPNETCORE_URLS=http://+:5003
ENTRYPOINT ["dotnet", "BookingsService.Presentation.dll"]
