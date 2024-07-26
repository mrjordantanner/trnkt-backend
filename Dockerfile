# Use the official ASP.NET Core runtime as a base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Use the .NET SDK image for building the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["trnkt-backend.csproj", "./"]
RUN dotnet restore "./trnkt-backend.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "trnkt-backend.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "trnkt-backend.csproj" -c Release -o /app/publish

# Create the final image from the base image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "trnkt-backend.dll"]
