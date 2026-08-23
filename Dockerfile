FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 7285


FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["global.json", "Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["src/Host/Host.csproj", "src/Host/"]
COPY ["KoiFengShuiSystem.Api/KoiFengShuiSystem.Api.csproj", "KoiFengShuiSystem.Api/"]
COPY ["KoiFengShuiSystem.Services/KoiFengShuiSystem.BusinessLogic.csproj", "KoiFengShuiSystem.Services/"]
COPY ["KoiFengShuiSystem.Common/KoiFengShuiSystem.Common.csproj", "KoiFengShuiSystem.Common/"]
COPY ["KoiFengShuiSystem.DataAccess/KoiFengShuiSystem.DataAccess.csproj", "KoiFengShuiSystem.DataAccess/"]
COPY ["KoiFengShuiSystem.Shared/KoiFengShuiSystem.Shared.csproj", "KoiFengShuiSystem.Shared/"]
COPY ["src/Shared/Shared.Kernel/Shared.Kernel.csproj", "src/Shared/Shared.Kernel/"]
COPY ["src/Shared/Shared.Infrastructure/Shared.Infrastructure.csproj", "src/Shared/Shared.Infrastructure/"]
RUN dotnet restore "src/Host/Host.csproj"
COPY . .
WORKDIR "/src/src/Host"
RUN dotnet build "Host.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Host.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Host.dll"]
