FROM mcr.microsoft.com/dotnet/aspnet:8.0 as base
WORKDIR /app
EXPOSE 7285
EXPOSE 7286

FROM mcr.microsoft.com/dotnet/sdk:8.0 as build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["KoiFengShuiSystem.Api/KoiFengShuiSystem.Api.csproj", "KoiFengShuiSystem.Api/"]
COPY ["KoiFengShuiSystem.Services/KoiFengShuiSystem.BusinessLogic.csproj", "KoiFengShuiSystem.BusinessLogic/"]
COPY ["KoiFengShuiSystem.Common/KoiFengShuiSystem.Common.csproj", "KoiFengShuiSystem.Common/"]
COPY ["KoiFengShuiSystem.DataAccess/KoiFengShuiSystem.DataAccess.csproj", "KoiFengShuiSystem.DataAccess/"]
COPY ["KoiFengShuiSystem.Shared/KoiFengShuiSystem.Shared.csproj", "KoiFengShuiSystem.Shared/"]
RUN dotnet restore "KoiFengShuiSystem.Api/KoiFengShuiSystem.Api.csproj"
COPY . .
WORKDIR "/src/KoiFengShuiSystem.Api"
RUN dotnet build "KoiFengShuiSystem.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build as publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "KoiFengShuiSystem.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base as final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "KoiFengShuiSystem.Api.dll"]
