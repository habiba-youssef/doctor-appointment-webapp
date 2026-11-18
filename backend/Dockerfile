
# Stage 1: Build the ASP.NET Core application
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# Copy the project file and restore dependencies
COPY DoctorsAppointment.Api.csproj ./
RUN dotnet restore DoctorsAppointment.Api.csproj

# Copy the remaining backend files
COPY . .

# Publish the application
RUN dotnet publish DoctorsAppointment.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore


# Stage 2: Run the application
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

# Copy the published files
COPY --from=build /app/publish .

# ASP.NET listens on port 8080 inside Docker
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# Start the backend
ENTRYPOINT ["dotnet", "DoctorsAppointment.Api.dll"]
