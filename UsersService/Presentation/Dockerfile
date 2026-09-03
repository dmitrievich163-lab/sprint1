# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore (layer caching): copy project files first
COPY SharedContracts/SharedContracts.csproj SharedContracts/
COPY UsersService/UsersService.Domain/UsersService.Domain.csproj UsersService/UsersService.Domain/
COPY UsersService/UsersService.Application/UsersService.Application.csproj UsersService/UsersService.Application/
COPY UsersService/UsersService.Infrastructure/UsersService.Infrastructure.csproj UsersService/UsersService.Infrastructure/
COPY UsersService/UsersService.Presentation/UsersService.Presentation.csproj UsersService/UsersService.Presentation/
RUN dotnet restore UsersService/UsersService.Presentation/UsersService.Presentation.csproj

# Copy the rest and publish
COPY SharedContracts/ SharedContracts/
COPY UsersService/ UsersService/
RUN dotnet publish UsersService/UsersService.Presentation/UsersService.Presentation.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5001
ENV ASPNETCORE_URLS=http://+:5001
ENTRYPOINT ["dotnet", "UsersService.Presentation.dll"]
