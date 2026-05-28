# Stage 1: Build ứng dụng
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy file solution và các file project để restore trước (tối ưu cache)
COPY Vivu.sln ./
COPY src/Vivu.Domain/Vivu.Domain.csproj ./src/Vivu.Domain/
COPY src/Vivu.Application/Vivu.Application.csproj ./src/Vivu.Application/
COPY src/Vivu.Infrastructure/Vivu.Infrastructure.csproj ./src/Vivu.Infrastructure/
COPY src/Vivu.WebApi/Vivu.WebApi.csproj ./src/Vivu.WebApi/
COPY tests/Vivu.Application.UnitTests/Vivu.Application.UnitTests.csproj ./tests/Vivu.Application.UnitTests/
COPY tests/Vivu.IntegrationTests/Vivu.IntegrationTests.csproj ./tests/Vivu.IntegrationTests/

RUN dotnet restore

# Copy toàn bộ mã nguồn và build
COPY . .
RUN dotnet publish "src/Vivu.WebApi/Vivu.WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 80
EXPOSE 443
ENTRYPOINT ["dotnet", "Vivu.WebApi.dll"]