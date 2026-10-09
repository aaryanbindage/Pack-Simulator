# 1. Use the official Microsoft .NET SDK image to build the app
FROM :mcr.microsoft.com/dotnet/sdk AS build
WORKDIR /src

# 2. Copy your project files and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# 3. Copy the remaining source files and compile the production build
COPY . ./
RUN dotnet publish -c Release -o /app/out

# 4. Use the lightweight .NET Runtime image to execute the app
FROM mcr.microsoft.com/dotnet/aspnet AS runtime
WORKDIR /app
COPY --from=build /app/out .

# 5. Bind the web server port dynamically for Render
ENV ASPNETCORE_URLS=http://0.0.0

# 6. Command to start your C# API (Change GachaSystem to your exact project name)
ENTRYPOINT ["dotnet", "GachaSystem.dll"]
mcr.microsoft.com/dotnet/sdk