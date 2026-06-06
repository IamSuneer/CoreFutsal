FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Shared/CoreFutsal.Shared/CoreFutsal.Shared.csproj", "src/Shared/CoreFutsal.Shared/"]
COPY ["src/Services/CoreFutsal.Match/CoreFutsal.Match.csproj", "src/Services/CoreFutsal.Match/"]
RUN dotnet restore "src/Services/CoreFutsal.Match/CoreFutsal.Match.csproj"
COPY . .
RUN dotnet publish "src/Services/CoreFutsal.Match/CoreFutsal.Match.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CoreFutsal.Match.dll"]
