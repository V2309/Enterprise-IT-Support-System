# Giai đoạn Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files và restore thư viện
COPY ["DeskPilot.Server/DeskPilot.Server.csproj", "DeskPilot.Server/"]
COPY ["DeskPilot.Shared/DeskPilot.Shared.csproj", "DeskPilot.Shared/"]
RUN dotnet restore "DeskPilot.Server/DeskPilot.Server.csproj"

# Copy toàn bộ code và build
COPY . .
WORKDIR "/src/DeskPilot.Server"
RUN dotnet build "DeskPilot.Server.csproj" -c Release -o /app/build

# Giai đoạn Publish
FROM build AS publish
RUN dotnet publish "DeskPilot.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Giai đoạn Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Mở cổng 5000 cho SignalR
ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "DeskPilot.Server.dll"]