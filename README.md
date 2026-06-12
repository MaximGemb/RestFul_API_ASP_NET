# RESTful API на ASP.NET Core

## О проекте

Данный проект представляет собой реализацию RESTful API с использованием ASP.NET Core. Он спроектирован с учетом принципов чистой архитектуры и лучших практик разработки, что делает его масштабируемым и простым в поддержке. Основная цель — продемонстрировать создание надежного бэкенда для современных веб-приложений.

### Используемые технологии
- **.NET 10.0**: Последняя версия фреймворка для кроссплатформенной разработки.
- **ASP.NET Core**: Для создания высокопроизводительных веб-API.
- **Entity Framework Core 10**: ORM для работы с базой данных.
- **Npgsql.EntityFrameworkCore.PostgreSQL**: Провайдер EF Core для PostgreSQL.
- **Swagger (OpenAPI)**: Для автоматической генерации интерактивной документации по API.

## Начало работы

### Предварительные требования

Убедитесь, что у вас установлены:

- **.NET 10.0 SDK** или более поздняя версия — [скачать с сайта Microsoft](https://dotnet.microsoft.com/download).
- **PostgreSQL** (рекомендуется версия 14 и выше) — [скачать с официального сайта](https://www.postgresql.org/download/).

### Установка и запуск

1. **Клонируйте репозиторий:**
   ```sh
   git clone <URL-вашего-репозитория>
   cd RestFul_API_ASP_NET
   ```

2. **Настройте строку подключения к PostgreSQL:**

   Откройте файл `Presentation/appsettings.json` и задайте параметры подключения к вашей БД:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=localhost;Port=5432;Database=eventapi;Username=postgres;Password=ваш_пароль"
   }
   ```
   Параметры по умолчанию: хост `localhost`, порт `5432`, база данных `eventapi`, пользователь `postgres`.

   > **Важно:** схема базы данных управляется **миграциями EF Core** (файлы в `Infrastructure/Migrations/`). При запуске приложения они применяются автоматически через `DatabaseMigrationRunner.MigrateIfRelational`.

3. **Восстановите зависимости:**
   ```sh
   dotnet restore
   ```

4. **Соберите проект:**
   ```sh
   dotnet build
   ```

5. **Запустите приложение:**
   ```sh
   dotnet run --project Presentation
   ```
   После запуска API будет доступно по адресу `https://localhost:<port>` и `http://localhost:<port>`, где `<port>` — это порт, указанный в консоли.

## Управление миграциями EF Core

Схема БД версионируется миграциями EF Core (файлы находятся в `Infrastructure/Migrations/`). При старте приложения все непримененные миграции применяются автоматически через `DatabaseMigrationRunner.MigrateIfRelational`.

> Для работы команд `dotnet ef` необходимо установить инструмент:
> ```sh
> dotnet tool install --global dotnet-ef
> ```

Поскольку `AppDbContext` объявлен в проекте **Infrastructure**, а точка входа находится в проекте **Presentation**, все команды EF Core требуют явного указания обоих флагов:
- `--project Infrastructure` — проект, содержащий `AppDbContext` и папку `Migrations/`.
- `--startup-project Presentation` — стартовый проект, откуда читается строка подключения (`appsettings.json`).

### Создание новой миграции

```sh
dotnet ef migrations add <НазваниеМиграции> --project Infrastructure --startup-project Presentation
```

### Применение миграций вручную

```sh
dotnet ef database update --project Infrastructure --startup-project Presentation
```

### Откат до конкретной миграции

```sh
dotnet ef database update <НазваниеЦелевойМиграции> --project Infrastructure --startup-project Presentation
```

### Удаление последней непримененной миграции

```sh
dotnet ef migrations remove --project Infrastructure --startup-project Presentation
```

## Документация API

Проект интегрирован со Swagger, который предоставляет удобный UI для изучения и тестирования эндпоинтов.

После запуска приложения перейдите по адресу:
**`http://localhost:<port>/swagger`**

Вы увидите полный список доступных эндпоинтов, их описание, ожидаемые параметры и модели данных. Также в Swagger UI можно отправлять запросы напрямую к API для тестирования.

### GET /api/events

Эндпоинт возвращает список событий с поддержкой фильтрации и пагинации.

#### Query-параметры

- `title` — фильтрация по части названия события без учета регистра.
- `from` — минимальная дата начала события. В выборку попадут события, у которых `StartAt >= from`.
- `to` — максимальная дата окончания события. В выборку попадут события, у которых `EndAt <= to`.
- `page` — номер страницы, начиная с `1`. Значение по умолчанию: `1`.
- `pageSize` — количество элементов на странице. Значение по умолчанию: `10`.

#### Примеры запросов с фильтрацией

Получить все события:

```http
GET /api/events
```

Фильтрация по названию:

```http
GET /api/events?title=backend
```

Фильтрация по диапазону дат:

```http
GET /api/events?from=2026-08-01T00:00:00&to=2026-08-31T23:59:59
```

Пагинация:

```http
GET /api/events?page=2&pageSize=5
```

Комбинированная фильтрация:

```http
GET /api/events?title=meetup&from=2026-08-01T00:00:00&to=2026-08-31T23:59:59&page=1&pageSize=10
```

#### Пример успешного ответа

```json
{
   "totalCount": 7,
   "items": [
      {
         "id": "9004537c-7940-49ea-b8f8-35e9dd8f03a9",
         "title": "string",
         "description": "string",
         "startAt": "2026-03-15T14:31:09.516Z",
         "endAt": "2026-03-15T14:31:09.518Z",
         "totalSeats": 100,
         "availableSeats": 97
      },
      {
         "id": "7e94c545-089c-4559-8940-fcd526caa0ad",
         "title": "string",
         "description": "string",
         "startAt": "2026-03-15T14:31:09.516Z",
         "endAt": "2026-03-15T14:31:09.518Z",
         "totalSeats": 50,
         "availableSeats": 50
      }
   ],
   "currentPageNumber": 2,
   "currentPageItemsCount": 2
}
```

### POST /api/events/{id}/book

Создает заявку на бронирование билета на указанное событие. Запрос ставится в очередь на асинхронную обработку.

#### Коды ответа

- `202 Accepted` — заявка создана и принята в обработку.
- `404 Not Found` — событие с указанным `id` не найдено.
- `409 Conflict` — свободные места закончились, забронировать больше нельзя.

#### Пример ответа

```json
{
  "id": "12345678-1234-1234-1234-123456789012",
  "eventId": "9004537c-7940-49ea-b8f8-35e9dd8f03a9",
  "status": "Pending",
  "createdAt": "2026-03-29T16:00:00Z"
}
```

#### Пример ошибки при отсутствии мест

```json
{
  "status": 409,
  "detail": "No available seats for this event"
}
```

### GET /api/bookings/{id}

Возвращает информацию о бронировании по его идентификатору, включая текущий статус обработки.

#### Пример ответа

```json
{
  "id": "12345678-1234-1234-1234-123456789012",
  "eventId": "9004537c-7940-49ea-b8f8-35e9dd8f03a9",
  "status": "Confirmed",
  "createdAt": "2026-03-29T16:00:00Z"
}
```

### Модель Event и места

Модель `Event` содержит поля вместимости:
- `totalSeats` — общее количество мест на событии.
- `availableSeats` — текущее количество свободных мест.

При создании события `availableSeats` инициализируется значением `totalSeats`. При каждом успешном бронировании значение `availableSeats` уменьшается на 1.

### Модель Booking и статусы

Модель `Booking` представляет собой заявку на бронирование и содержит следующие поля:
- `id` (UUID) — уникальный идентификатор бронирования.
- `eventId` (UUID) — идентификатор события, на которое оформляется бронирование.
- `status` (строка) — текущий статус бронирования.
- `createdAt` (дата и время) — время создания заявки.

**Доступные статусы бронирования (`BookingStatus`):**
- `Pending` — заявка создана и ожидает обработки.
- `Confirmed` — заявка успешно обработана и подтверждена.
- `Rejected` — ошибка при обработке заявки или бронь отклонена (например, нет мест).

### Фоновая обработка

Для обработки заявок используется `BookingBackgroundService` (на базе `BackgroundService`).
При вызове `POST /api/events/{id}/book` бронь сохраняется со статусом `Pending`.
Фоновый сервис периодически выбирает pending-брони, обрабатывает их и обновляет статус на `Confirmed` или `Rejected` в зависимости от результата обработки.

### Примитивы синхронизации

В проекте используются следующие примитивы синхронизации:

- `SemaphoreSlim(1, 1)` в `BookingService.CreateBookingAsync` — делает операцию проверки/уменьшения `availableSeats` атомарной (через EF Core + PostgreSQL), чтобы несколько одновременных запросов не забронировали одно и то же место (защита от race condition).
- `IServiceScopeFactory` в `BookingBackgroundService` — каждый цикл обработки открывает собственный DI-скоуп для получения `AppDbContext`, что обеспечивает корректную работу scoped-сервисов из singleton-фонового сервиса.

### Пример сценария использования

1. **Просмотр событий:** Клиент запрашивает список доступных событий через `GET /api/events`.
2. **Создание бронирования:** Клиент выбирает событие и отправляет запрос на бронирование `POST /api/events/{eventId}/book`. API быстро отвечает, возвращая ID бронирования и статус `Pending`.
3. **Отслеживание статуса:** Клиент периодически (Long Polling или обычный Polling) запрашивает статус через `GET /api/bookings/{bookingId}`.
4. **Завершение:** Через некоторое время фоновый сервис завершает обработку, и статус бронирования меняется на `Confirmed` или `Rejected`.

### Пример сценария овербукинга

1. Есть событие с `totalSeats = 2`, `availableSeats = 2`.
2. Почти одновременно приходят 3 запроса на `POST /api/events/{id}/book`.
3. Первые 2 запроса проходят: создаются брони, `availableSeats` уменьшается до `0`.
4. Третий запрос в критической секции (`SemaphoreSlim`) видит, что свободных мест нет, и получает `409 Conflict`.
5. Таким образом API не допускает овербукинг: число успешных бронирований не превышает `totalSeats`.

### Формат ошибок

API использует глобальную обработку исключений и возвращает ошибки в формате `ProblemDetails` (`application/json`).

#### Общий формат ответа при ошибке

```json
{
  "status": 400,
  "detail": "Описание ошибки"
}
```

#### Примеры ответов с ошибками

Событие не найдено:

```http
GET /api/events/11111111-1111-1111-1111-111111111111
```

```json
{
  "status": 404,
  "detail": "Can't get event with id 11111111-1111-1111-1111-111111111111. Event not found"
}
```

Некорректные даты:

```json
{
  "status": 400,
  "detail": "Дата завершения должна быть позже даты начала."
}
```

### Запуск тестов

Для запуска всех тестов из корня решения выполните команду:

```sh
dotnet test
```

Для запуска только модульных тестов:

```sh
dotnet test .\Application.Tests\Application.Tests.csproj
```

Для запуска только интеграционных тестов:

```sh
dotnet test .\Infrastructure.IntegrationTests\Infrastructure.IntegrationTests.csproj
```

> **InMemory-провайдер в модульных тестах:** проект `Application.Tests` использует пакет `Microsoft.EntityFrameworkCore.InMemory`. `AppDbContext` конфигурируется с in-memory базой данных, что позволяет запускать тесты без реального PostgreSQL-сервера.

> **Интеграционные тесты (`Infrastructure.IntegrationTests`) требуют Docker.** Они используют пакет `Testcontainers.PostgreSql`, который автоматически поднимает контейнер `postgres:16-alpine` перед каждым тестовым классом и удаляет его после завершения. Перед запуском убедитесь, что Docker Desktop (или Docker Engine) запущен на вашей машине.

## Структура проекта

Проект реализует **Clean Architecture** с разделением на четыре основных слоя и два тестовых проекта. Зависимости направлены строго внутрь: `Presentation` → `Application` → `Domain`; `Infrastructure` реализует интерфейсы `Application`.

```
RestFul_API_ASP_NET/
├── Domain/                          # Слой домена (ядро)
│   ├── Entities/
│   │   ├── Event.cs                 # Сущность события
│   │   ├── Booking.cs               # Сущность бронирования
│   │   └── BookingStatus.cs         # Перечисление статусов брони
│   └── Exceptions/
│       ├── NotFoundException.cs     # Исключение «не найдено» (→ 404)
│       ├── NoAvailableSeatsException.cs  # Нет свободных мест (→ 409)
│       └── CustomValidationException.cs # Ошибка валидации (→ 400)
│
├── Application/                     # Слой приложения (бизнес-логика)
│   ├── Interfaces/
│   │   ├── IEventService.cs         # Контракт сервиса событий
│   │   ├── IBookingService.cs       # Контракт сервиса бронирований
│   │   ├── IEventRepository.cs      # Контракт репозитория событий
│   │   └── IBookingRepository.cs    # Контракт репозитория бронирований
│   ├── Services/
│   │   ├── EventService.cs          # Логика работы с событиями
│   │   ├── BookingService.cs        # Логика создания/проверки брони
│   │   └── BookingBackgroundService.cs  # Фоновая обработка pending-броней
│   ├── DTOs/
│   │   ├── EventInfo.cs             # Ответ с данными события
│   │   ├── CreateEvent.cs           # Запрос на создание события
│   │   ├── UpdateEvent.cs           # Запрос на обновление события
│   │   ├── BookingInfo.cs           # Ответ с данными бронирования
│   │   ├── PaginatedResult.cs       # Обёртка для пагинированного ответа
│   │   └── PaginationRequest.cs     # Параметры пагинации
│   └── Extensions/
│       └── ApplicationExtensions.cs # Класс ApplicationServiceRegistration — регистрация сервисов слоя Application
│
├── Infrastructure/                  # Инфраструктурный слой (реализация)
│   ├── DataAccess/
│   │   ├── AppDbContext.cs          # Контекст EF Core
│   │   ├── DatabaseMigrationRunner.cs  # Автоматическое применение миграций
│   │   ├── Configurations/
│   │   │   ├── EventConfiguration.cs   # Конфигурация таблицы Events
│   │   │   └── BookingConfiguration.cs # Конфигурация таблицы Bookings
│   │   └── Repositories/
│   │       ├── EventRepository.cs   # Реализация IEventRepository
│   │       └── BookingRepository.cs # Реализация IBookingRepository
│   ├── Migrations/                  # Файлы миграций EF Core
│   └── Extensions/
│       └── InfrastructureExtensions.cs  # Класс InfrastructureServiceRegistration — регистрация DbContext и репозиториев
│
├── Presentation/                    # Слой представления (точка входа)
│   ├── Controllers/
│   │   ├── EventsController.cs      # Эндпоинты /api/events
│   │   └── BookingsController.cs    # Эндпоинт GET /api/bookings/{id}
│   ├── Middleware/
│   │   └── GlobalExceptionHandlingMiddleware.cs  # Глобальный обработчик исключений
│   ├── Program.cs                   # Настройка DI, middleware и запуск приложения
│   ├── appsettings.json             # Конфигурация (строка подключения и др.)
│   └── appsettings.Development.json # Конфигурация для среды разработки
│
├── Application.Tests/               # Модульные тесты (xUnit + Moq)
│   ├── Controllers/                 # Тесты контроллеров
│   ├── Services/                    # Тесты сервисов (EventService, BookingService, фоновый)
│   ├── Middleware/                  # Тесты GlobalExceptionHandlingMiddleware
│   ├── DTOs/                        # Тесты DTO-валидации
│   ├── Exceptions/                  # Тесты доменных исключений
│   ├── Models/                      # Тесты доменных моделей
│   ├── DatabaseMigrationRunnerTests.cs
│   └── ProgramTests.cs              # Тесты конфигурации приложения
│
├── Infrastructure.IntegrationTests/ # Интеграционные тесты (Testcontainers)
│   ├── EventRepositoryTests.cs      # Тесты репозитория событий с реальной БД
│   └── BookingRepositoryTests.cs    # Тесты репозитория бронирований с реальной БД
│
└── RestFulApi/                      # (устаревший монолитный проект, сохранён для истории)
```

### Назначение слоёв

- **Domain** — содержит только бизнес-сущности и доменные исключения. Не зависит ни от одного другого проекта в решении.
- **Application** — оркестрирует бизнес-логику через сервисы. Определяет интерфейсы репозиториев и сервисов (контракты), которые реализуются в других слоях. Зависит только от `Domain`.
- **Infrastructure** — реализует интерфейсы из `Application`: `AppDbContext`, репозитории, `DatabaseMigrationRunner`. Зависит от `Application` и `Domain`. Именно здесь хранятся миграции EF Core.
- **Presentation** — HTTP-слой: контроллеры, middleware, `Program.cs`. Компонует все слои через DI, вызывая `AddApplicationServices()` и `AddInfrastructureServices()`. Зависит от `Application` и `Infrastructure`.
- **Application.Tests** — модульные тесты с замоканными зависимостями (без реальной БД и HTTP-сервера).
- **Infrastructure.IntegrationTests** — интеграционные тесты репозиториев с реальным PostgreSQL через Testcontainers (требуют Docker).