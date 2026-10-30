# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only the csproj first so restore is cached unless dependencies change
COPY src/MongoDbTask.API/MongoDbTask.API.csproj src/MongoDbTask.API/
RUN dotnet restore src/MongoDbTask.API/MongoDbTask.API.csproj

# Copy the rest of the source and publish
COPY . .
RUN dotnet publish src/MongoDbTask.API/MongoDbTask.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

# ASP.NET images listen on 8080 by default (non-root user)
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "MongoDbTask.API.dll"]