# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore (layer caching): copy project files first
COPY SharedContracts/SharedContracts.csproj SharedContracts/
COPY EventsService/Domain/EventsService.Domain.csproj EventsService/Domain/
COPY EventsService/Application/EventsService.Application.csproj EventsService/Application/
COPY EventsService/Infrastructure/EventsService.Infrastructure.csproj EventsService/Infrastructure/
COPY EventsService/Presentation/EventsService.Presentation.csproj EventsService/Presentation/
RUN dotnet restore EventsService/Presentation/EventsService.Presentation.csproj

# Copy the rest and publish
COPY SharedContracts/ SharedContracts/
COPY EventsService/ EventsService/
RUN dotnet publish EventsService/Presentation/EventsService.Presentation.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5002
ENV ASPNETCORE_URLS=http://+:5002
ENTRYPOINT ["dotnet", "EventsService.Presentation.dll"]
