FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["EventService/EventService.Presentation/EventService.Presentation.csproj", "EventService/EventService.Presentation/"]
COPY ["EventService/EventService.Application/EventService.Application.csproj", "EventService/EventService.Application/"]
COPY ["EventService/EventService.Domain/EventService.Domain.csproj", "EventService/EventService.Domain/"]
COPY ["EventService/EventService.Infrastructure/EventService.Infrastructure.csproj", "EventService/EventService.Infrastructure/"]
COPY ["Shared.Contracts/Shared.Contracts.csproj", "Shared.Contracts/"]

RUN dotnet restore "EventService/EventService.Presentation/EventService.Presentation.csproj"

COPY . .

WORKDIR "/src/EventService/EventService.Presentation"
RUN dotnet publish "EventService.Presentation.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EventService.Presentation.dll"]
