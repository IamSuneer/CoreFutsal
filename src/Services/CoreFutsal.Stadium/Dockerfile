FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Shared/CoreFutsal.Shared/CoreFutsal.Shared.csproj", "src/Shared/CoreFutsal.Shared/"]
COPY ["src/Services/CoreFutsal.Stadium/CoreFutsal.Stadium.csproj", "src/Services/CoreFutsal.Stadium/"]
RUN dotnet restore "src/Services/CoreFutsal.Stadium/CoreFutsal.Stadium.csproj"
COPY . .
RUN dotnet publish "src/Services/CoreFutsal.Stadium/CoreFutsal.Stadium.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CoreFutsal.Stadium.dll"]
