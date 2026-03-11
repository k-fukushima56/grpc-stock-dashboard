FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 50051

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["GrpcStockServer/GrpcStockServer.csproj", "GrpcStockServer/"]
RUN dotnet restore "GrpcStockServer/GrpcStockServer.csproj"
COPY . .
WORKDIR "/src/GrpcStockServer"
RUN dotnet build "GrpcStockServer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "GrpcStockServer.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# curlをインストール（ヘルスチェック用）
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
ENTRYPOINT ["dotnet", "GrpcStockServer.dll"]
