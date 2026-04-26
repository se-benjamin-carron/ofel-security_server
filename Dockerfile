FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Ofel.Security.Server/Ofel.Security.Server.csproj ./Ofel.Security.Server/
RUN dotnet restore ./Ofel.Security.Server/Ofel.Security.Server.csproj
COPY Ofel.Security.Server/ ./Ofel.Security.Server/
RUN dotnet publish ./Ofel.Security.Server/Ofel.Security.Server.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Ofel.Security.Server.dll"]
