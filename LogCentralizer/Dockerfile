# Runtime (ASP.NET) .NET 10 (preview)
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS base
WORKDIR /app
EXPOSE 5080

# Build SDK .NET 10 (preview)
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# (souvent nécessaire pour net10 preview)
ENV DOTNET_EnablePreviewFeatures=1

COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /out

FROM base AS final
WORKDIR /app
COPY --from=build /out .
ENTRYPOINT ["dotnet", "LogCentralizer.dll"]