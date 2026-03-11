FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["GrpcStockWeb/GrpcStockWeb.csproj", "GrpcStockWeb/"]
RUN dotnet restore "GrpcStockWeb/GrpcStockWeb.csproj"
COPY . .
WORKDIR "/src/GrpcStockWeb"
RUN dotnet build "GrpcStockWeb.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "GrpcStockWeb.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# curlをインストール（ヘルスチェック用）
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
ENTRYPOINT ["dotnet", "GrpcStockWeb.dll"]
