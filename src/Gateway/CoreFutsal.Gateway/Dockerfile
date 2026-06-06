FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Gateway/CoreFutsal.Gateway/CoreFutsal.Gateway.csproj", "src/Gateway/CoreFutsal.Gateway/"]
RUN dotnet restore "src/Gateway/CoreFutsal.Gateway/CoreFutsal.Gateway.csproj"
COPY . .
RUN dotnet publish "src/Gateway/CoreFutsal.Gateway/CoreFutsal.Gateway.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CoreFutsal.Gateway.dll"]
