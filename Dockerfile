FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src


COPY ["Wasl.API/Wasl.API.csproj", "Wasl.API/"]
COPY ["Wasl.Application/Wasl.Application.csproj", "Wasl.Application/"]
COPY ["Wasl.Core/Wasl.Core.csproj", "Wasl.Core/"]
COPY ["Wasl.Infrastructure/Wasl.Infrastructure.csproj", "Wasl.Infrastructure/"]


RUN dotnet restore "Wasl.API/Wasl.API.csproj"
# نسخ باقي الكود وبناء المشروع
COPY . .
WORKDIR "/src/Wasl.API"
RUN dotnet publish -c Release -o /app/publish

# صورة التشغيل (Runtime) - بسيطة وسريعة
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Wasl.API.dll"]