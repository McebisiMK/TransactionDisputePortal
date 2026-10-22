FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY TransactionDisputePortal.slnx Directory.Build.props Directory.Packages.props .editorconfig ./
COPY TransactionDisputePortal.API/TransactionDisputePortal.API.csproj TransactionDisputePortal.API/
COPY TransactionDisputePortal.Application/TransactionDisputePortal.Application.csproj TransactionDisputePortal.Application/
COPY TransactionDisputePortal.Domain/TransactionDisputePortal.Domain.csproj TransactionDisputePortal.Domain/
COPY TransactionDisputePortal.Infrastructure/TransactionDisputePortal.Infrastructure.csproj TransactionDisputePortal.Infrastructure/
RUN dotnet restore TransactionDisputePortal.API/TransactionDisputePortal.API.csproj

COPY . .
RUN dotnet publish TransactionDisputePortal.API/TransactionDisputePortal.API.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN mkdir -p /app/logs && chown $APP_UID /app/logs

COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

USER $APP_UID
ENTRYPOINT ["dotnet", "TransactionDisputePortal.API.dll"]
