FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["BookingService/BookingService.Presentation/BookingService.Presentation.csproj", "BookingService/BookingService.Presentation/"]
COPY ["BookingService/BookingService.Application/BookingService.Application.csproj", "BookingService/BookingService.Application/"]
COPY ["BookingService/BookingService.Domain/BookingService.Domain.csproj", "BookingService/BookingService.Domain/"]
COPY ["BookingService/BookingService.Infrastructure/BookingService.Infrastructure.csproj", "BookingService/BookingService.Infrastructure/"]

RUN dotnet restore "BookingService/BookingService.Presentation/BookingService.Presentation.csproj"

COPY . .

WORKDIR "/src/BookingService/BookingService.Presentation"
RUN dotnet publish "BookingService.Presentation.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BookingService.Presentation.dll"]
