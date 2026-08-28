# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore (layer caching): copy project files first
COPY SharedContracts/SharedContracts.csproj SharedContracts/
COPY BookingsService/BookingsService.Domain/BookingsService.Domain.csproj BookingsService/BookingsService.Domain/
COPY BookingsService/BookingsService.Application/BookingsService.Application.csproj BookingsService/BookingsService.Application/
COPY BookingsService/BookingsService.Infrastructure/BookingsService.Infrastructure.csproj BookingsService/BookingsService.Infrastructure/
COPY BookingsService/BookingsService.Presentation/BookingsService.Presentation.csproj BookingsService/BookingsService.Presentation/
RUN dotnet restore BookingsService/BookingsService.Presentation/BookingsService.Presentation.csproj

# Copy the rest and publish
COPY SharedContracts/ SharedContracts/
COPY BookingsService/ BookingsService/
RUN dotnet publish BookingsService/BookingsService.Presentation/BookingsService.Presentation.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5003
ENV ASPNETCORE_URLS=http://+:5003
ENTRYPOINT ["dotnet", "BookingsService.Presentation.dll"]
