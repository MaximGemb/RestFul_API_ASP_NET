# Event Booking Platform — микросервисный бэкенд на ASP.NET Core

## О проекте

Платформа для управления событиями и их бронирования, построенная по микросервисной архитектуре на .NET 10. Каждый сервис — самостоятельное ASP.NET Core-приложение с собственной базой данных. Межсервисное взаимодействие реализовано через Apache Kafka с паттернами **Outbox** (надёжная публикация) и **Inbox** (идемпотентное потребление).

### Используемые технологии

- **.NET 10.0** — последняя версия фреймворка.
- **ASP.NET Core** — высокопроизводительные веб-API.
- **Entity Framework Core 10 + Npgsql** — ORM для PostgreSQL.
- **Apache Kafka (Confluent.Kafka)** — асинхронный обмен сообщениями между сервисами.
- **Redis (StackExchange.Redis)** — кеширование данных событий в EventService (паттерн Cache-Aside).
- **OpenTelemetry** — метрики и распределённая трассировка.
- **Prometheus** — сбор и хранение метрик.
- **Jaeger** — UI для просмотра трейсов (OTLP gRPC).
- **Grafana** — дашборды и визуализация метрик.
- **Serilog** — структурированное логирование (JSON).
- **Swagger (OpenAPI)** — интерактивная документация API.
- **Docker Compose** — поднятие инфраструктуры и всех .NET-сервисов в контейнерах.

---

## Состав системы

| Сервис | HTTP-порт | База данных | Порт БД (внешний) | Kafka роль |
|---|---|---|---|---|
| **UserService** | `5000` | `users_db` | `5432` | — |
| **EventService** | `5001` | `events_db` | `5433` | Consumer (`booking-confirmed`) |
| **BookingService** | `5002` | `bookings_db` | `5434` | Producer (`booking-confirmed`) |
| **Kafka** | `9092` | — | — | Брокер сообщений |
| **Redis** | `6379` | — | — | Кеш EventService |
| **Prometheus** | `9090` | — | — | Сбор метрик |
| **Jaeger** | `16686` | — | — | Распределённая трассировка |
| **Grafana** | `3000` | — | — | Дашборды и визуализация |

Вся инфраструктура (Kafka + Redis + 3 × PostgreSQL) и все три .NET-сервиса поднимаются одним файлом `docker-compose.yml`.

### Описание сервисов

- **UserService** — регистрация и аутентификация пользователей. Выдаёт JWT-токены, которые остальные сервисы используют для проверки прав.
- **EventService** — каталог событий (CRUD). Хранит `events_db` с таблицами `Events` и `InboxMessages`. Подписан на Kafka-топик `booking-confirmed` — при получении события уменьшает `availableSeats` у соответствующего мероприятия. Кеширует событие по ID и топ-10 популярных событий в Redis (см. раздел [Кеширование (Redis) в EventService](#кеширование-redis-в-eventservice)).
- **BookingService** — создание, подтверждение и отмена бронирований. Хранит `bookings_db` с таблицами `Bookings` и `OutboxMessages`. После подтверждения брони публикует сообщение `BookingConfirmed` в Kafka через паттерн Outbox.
- **Shared.Contracts** — общая библиотека с контрактами Kafka-сообщений (`BookingConfirmed`, `BookingTopics`).

---

## Поток данных BookingConfirmed

```
Клиент
  │
  │  POST /bookings/events/{eventId}
  ▼
BookingService (порт 5002)
  │  1. Создаёт Booking(Status=Pending) в bookings_db
  │
  │  BookingBackgroundService (каждые 5 с)
  │  2. Подтверждает бронь: Status → Confirmed
  │  3. Атомарно сохраняет OutboxMessage(Type=BookingConfirmed) в той же транзакции
  │
  │  OutboxRelayService (каждые 5 с)
  │  4. Читает необработанные OutboxMessages
  │  5. Публикует BookingConfirmed → топик "booking-confirmed"
  │  6. Помечает OutboxMessage как отправленное (at-least-once)
  │
  ▼
Apache Kafka  (localhost:9092)  топик: booking-confirmed
  │
  ▼
EventService (порт 5001)
  │  BookingConfirmedConsumer (BackgroundService)
  │  7. Проверяет InboxMessages — если BookingId уже есть, пропускает (идемпотентность)
  │  8. Вызывает event.TryReserveSeats(seatsCount) — уменьшает availableSeats
  │  9. Атомарно сохраняет: изменение события + InboxMessage(BookingId)
```

### Гарантии надёжности

- **Outbox** (BookingService) — сообщение попадает в Kafka только после фиксации транзакции в БД. Повторные попытки при сбое гарантируют доставку *at-least-once*.
- **Inbox** (EventService) — перед обработкой проверяется таблица `InboxMessages` по `BookingId`. Дублирующее сообщение от Kafka молча пропускается.

---

## Кеширование (Redis) в EventService

`EventService` использует **Redis** (`StackExchange.Redis`) по паттерну **Cache-Aside**: при чтении сначала проверяется кеш, при отсутствии — данные берутся из БД и кладутся в кеш. Если Redis недоступен, `RedisCacheService` перехватывает ошибку, логирует её и возвращает `null`/пропускает запись — запрос при этом обслуживается напрямую из БД, деградация кеша не приводит к отказу сервиса.

### Что кешируется и почему

| Ключ | Что хранится | TTL | Обоснование TTL |
|---|---|---|---|
| `event:{id}` | Данные одного события (`EventInfo`) | **5 минут** | Карточка события запрашивается часто (просмотр, повторные обращения), но должна быть достаточно свежей — после бронирования доступные места должны обновляться быстро. Инвалидация при записи покрывает большинство изменений, TTL — защита на случай пропуска инвалидации. |
| `events:top10` | Список топ-10 популярных событий | **2 минуты** | Рейтинговый агрегат, который меняется нечасто и для которого небольшое устаревание некритично. Явная инвалидация при каждом бронировании была бы избыточной нагрузкой на Redis, поэтому список обновляется только по TTL, а также при создании/удалении событий (когда состав списка объективно меняется). |

### Что происходит при изменении данных

Для `event:{id}` выбрана стратегия **«инвалидация при записи»** (write-invalidate): при изменении события ключ удаляется из кеша, а не обновляется. Следующий читающий запрос обращается к базе и прогревает кеш заново (`EventService.Application.Services.EventService.GetEventByIdAsync`). Эта стратегия проще стратегии «обновление при записи», не требует пересобирать `EventInfo` в месте записи и одинаково хорошо работает как для HTTP-запросов, так и для Kafka-обработчика.

Порядок операций всегда одинаковый: **сначала сохраняем изменения в базе данных, затем инвалидируем кеш**. Если выполнение прервётся между этими двумя шагами, база останется в актуальном состоянии, а кеш будет обновлён при следующем запросе — то есть в худшем случае читающий запрос получит немного устаревшие данные, но никогда — рассинхронизацию с БД в другую сторону.

- **`EventService.UpdateEventAsync`** — сохраняет изменения в БД, затем удаляет `event:{id}`.
- **`EventService.DeleteEventAsync`** — удаляет событие из БД, затем удаляет `event:{id}` и `events:top10` (состав топ-10 мог измениться).
- **`EventService.CreateEventAsync`** — сохраняет событие в БД, затем удаляет `events:top10` (новое событие может попасть в топ-10).
- **`BookingConfirmedConsumer`** (Kafka-обработчик `booking-confirmed`) — после уменьшения `availableSeats` и сохранения в БД удаляет только `event:{id}` соответствующего события. `events:top10` **не** инвалидируется на каждое бронирование — этот кеш обновляется по TTL, что достаточно для рейтингового агрегата и не создаёт лишней нагрузки на Redis при высокой частоте бронирований.

---

## Наблюдаемость (Observability)

Добавлен полноценный стек мониторинга, трассировки и логирования для всех трёх микросервисов.

### Добавленные инструменты

| Инструмент | Назначение | UI / Порт |
|---|---|---|
| **Prometheus** | Сбор метрик (scrape `/metrics` каждые 15 с) | http://localhost:9090 |
| **Jaeger** | Распределённая трассировка (OpenTelemetry → OTLP gRPC) | http://localhost:16686 |
| **Grafana** | Визуализация метрик и дашборды | http://localhost:3000 (логин `admin` / пароль `admin`) |
| **Serilog** | Структурированное логирование (JSON в stdout) | — (логи видны через `docker compose logs`) |

### Что экспортируют сервисы

Каждый .NET-сервис (UserService, EventService, BookingService) настроен с помощью **OpenTelemetry SDK** и экспортирует:

- **Метрики** — эндпоинт `GET /metrics` (формат Prometheus). Включена инструментация ASP.NET Core и .NET Runtime.
- **Трейсы** — отправляются по OTLP gRPC на `http://jaeger:4317`. Инструментированы: ASP.NET Core, HttpClient, Entity Framework Core.
- **Логи** — структурированный JSON через Serilog (`CompactJsonFormatter`) в stdout контейнера.

### NuGet-пакеты (добавлены в каждый `*.Presentation.csproj`)

| Пакет | Версия |
|---|---|
| `OpenTelemetry.Exporter.OpenTelemetryProtocol` | 1.17.0 |
| `OpenTelemetry.Exporter.Prometheus.AspNetCore` | 1.17.0-beta.1 |
| `OpenTelemetry.Extensions.Hosting` | 1.17.0 |
| `OpenTelemetry.Instrumentation.AspNetCore` | 1.17.0 |
| `OpenTelemetry.Instrumentation.EntityFrameworkCore` | 1.17.0-beta.1 |
| `OpenTelemetry.Instrumentation.Http` | 1.17.0 |
| `OpenTelemetry.Instrumentation.Runtime` | 1.17.0 |
| `Serilog.AspNetCore` | 10.0.0 |
| `Serilog.Formatting.Compact` | 3.0.0 |

### Конфигурация (`appsettings.json`)

В каждый сервис добавлена секция:

```json
"Otlp": {
  "Endpoint": "http://localhost:4317",
  "ServiceName": "<имя-сервиса>"
}
```

В Docker-окружении переменная `Otlp__Endpoint` переопределяется на `http://jaeger:4317`.

### Prometheus (`prometheus.yml`)

Файл конфигурации маунтится в контейнер Prometheus. Scrape-конфиг собирает метрики со всех трёх сервисов:

```yaml
scrape_configs:
  - job_name: users-service
    static_configs:
      - targets: ["userservice:5000"]
  - job_name: events-service
    static_configs:
      - targets: ["eventservice:5001"]
  - job_name: bookings-service
    static_configs:
      - targets: ["bookingservice:5002"]
```

### Готовый дашборд Grafana

В репозитории лежит экспортированный дашборд `HTTP Metrics Overview-1784629061584.json`. Для импорта:

1. Откройте Grafana → **Dashboards** → **Import**.
2. Загрузите JSON-файл из корня репозитория.
3. Укажите Prometheus в качестве источника данных.

### Запуск стека мониторинга

**Полный запуск (вместе с сервисами):**

```sh
docker compose up -d
```

**Только инструменты мониторинга (если сервисы запускаются локально через `dotnet run`):**

```sh
docker compose up -d prometheus jaeger grafana
```

После запуска убедитесь, что контейнеры работают:

```sh
docker compose ps
```

Затем откройте:
- **Prometheus** — http://localhost:9090/targets — все таргеты должны быть в состоянии `UP`.
- **Jaeger** — http://localhost:16686 — выберите сервис из выпадающего списка и найдите трейсы.
- **Grafana** — http://localhost:3000 — добавьте Prometheus как Data Source (`http://prometheus:9090`) и импортируйте дашборд.

---

## Инфраструктура (Docker)

Файл `docker-compose.yml` поднимает всю инфраструктуру и .NET-сервисы:

| Контейнер | Образ/Сборка | Порт |
|---|---|---|
| `eventapi-zookeeper` | `confluentinc/cp-zookeeper:7.6.1` | внутренний `2181` |
| `eventapi-kafka` | `confluentinc/cp-kafka:7.6.1` | `9092` (внешний), `29092` (внутренний) |
| `eventapi-users-db` | `postgres:16` | `5432` (volume `users-db-data`) |
| `eventapi-events-db` | `postgres:16` | `5433` (volume `events-db-data`) |
| `eventapi-bookings-db` | `postgres:16` | `5434` (volume `bookings-db-data`) |
| `eventapi-redis` | `redis:7` (пароль `secret`, `maxmemory 256mb`, политика вытеснения `allkeys-lru`) | `6379` (volume `redis-data`) |
| `eventapi-userservice` | `UserService/Dockerfile` | `5000` |
| `eventapi-eventservice` | `EventService/Dockerfile` | `5001` |
| `eventapi-bookingservice` | `BookingService/Dockerfile` | `5002` |
| `eventapi-prometheus` | `prom/prometheus:v2.51.0` | `9090` |
| `eventapi-jaeger` | `jaegertracing/all-in-one:1.56` | `16686` (UI), `4317` (OTLP gRPC) |
| `eventapi-grafana` | `grafana/grafana:10.4.2` | `3000` (volume `grafana-data`) |

### Два режима запуска

**Режим 1 — Полный Docker (рекомендуемый):** все сервисы и инфраструктура в контейнерах. Сервисы общаются между собой через внутреннюю сеть Docker (`kafka:29092`, `users-db:5432` и т.д.).

**Режим 2 — Локальная разработка:** в Docker поднимается только инфраструктура (Kafka + БД), а .NET-сервисы запускаются через `dotnet run` и подключаются к `localhost`. Для этого режима в `appsettings.json` уже прописаны `localhost:9092` и соответствующие порты БД (`5432`, `5433`, `5434`).

---

## Инструкция по запуску

### Предварительные требования

- **Docker Desktop** (или Docker Engine) — для запуска инфраструктуры и сервисов.
- **.NET 10.0 SDK** — [скачать с сайта Microsoft](https://dotnet.microsoft.com/download) (только для режима локальной разработки).

### Режим 1 — Полный запуск через Docker (рекомендуемый)

```sh
# Запустить всё одной командой
docker compose up -d
```

Команда собирает образы .NET-сервисов и поднимает все 8 контейнеров. Дождитесь, пока все контейнеры перейдут в статус `healthy`:

```sh
docker compose ps
```

Миграции EF Core применяются **автоматически** при каждом старте сервиса через `DatabaseMigrationRunner.MigrateIfRelational`.

### Режим 2 — Локальная разработка (инфраструктура в Docker, сервисы через dotnet run)

#### Шаг 1 — Запустить только инфраструктуру

```sh
docker compose up -d zookeeper kafka users-db events-db bookings-db redis
```

#### Шаг 2 — Восстановить зависимости и собрать решение

```sh
dotnet restore Microservices_EventBooking.slnx
dotnet build Microservices_EventBooking.slnx
```

#### Шаг 3 — Запустить сервисы (каждый в отдельном терминале)

```sh
# Терминал 1 — UserService (http://localhost:5000)
dotnet run --project UserService/UserService.Presentation --launch-profile user_http

# Терминал 2 — EventService (http://localhost:5001)
dotnet run --project EventService/EventService.Presentation --launch-profile events_http

# Терминал 3 — BookingService (http://localhost:5002)
dotnet run --project BookingService/BookingService.Presentation --launch-profile booking-http
```

### Проверить Swagger UI

| Сервис | Swagger |
|---|---|
| UserService | http://localhost:5000/swagger |
| EventService | http://localhost:5001/swagger |
| BookingService | http://localhost:5002/swagger |

### Остановить

```sh
# Остановить все контейнеры
docker compose down
```

Для полной очистки (включая тома с данными):

```sh
docker compose down -v
```

## Управление миграциями EF Core

Каждый сервис управляет своей схемой независимо. Миграции применяются **автоматически** при старте через `DatabaseMigrationRunner.MigrateIfRelational`.

> Установите инструмент `dotnet-ef`, если ещё не установлен:
> ```sh
> dotnet tool install --global dotnet-ef
> ```

Для каждого сервиса шаблон команд одинаков — нужно указывать `--project <Сервис>.Infrastructure` и `--startup-project <Сервис>.Presentation`.

### UserService

```sh
# Создать миграцию
dotnet ef migrations add <Название> --project UserService/UserService.Infrastructure --startup-project UserService/UserService.Presentation

# Применить вручную
dotnet ef database update --project UserService/UserService.Infrastructure --startup-project UserService/UserService.Presentation
```

### EventService

```sh
dotnet ef migrations add <Название> --project EventService/EventService.Infrastructure --startup-project EventService/EventService.Presentation

dotnet ef database update --project EventService/EventService.Infrastructure --startup-project EventService/EventService.Presentation
```

### BookingService

```sh
dotnet ef migrations add <Название> --project BookingService/BookingService.Infrastructure --startup-project BookingService/BookingService.Presentation

dotnet ef database update --project BookingService/BookingService.Infrastructure --startup-project BookingService/BookingService.Presentation
```

---

## Аутентификация и авторизация

Все три сервиса используют **JWT Bearer** (HMAC-SHA256) с общим секретом. Токены выдаёт **UserService** (`http://localhost:5000`). Остальные сервисы только валидируют подпись и claims.

```
Authorization: Bearer <токен>
```

### Ролевая модель

| Роль    | Описание |
|---------|----------|
| `User`  | Обычный пользователь. Назначается при регистрации по умолчанию. |
| `Admin` | Администратор с правами управления событиями. |

### Получение токена

1. Зарегистрируйте пользователя — **`POST /auth/register`** на `http://localhost:5000`:
   ```json
   { "login": "your_login", "password": "your_password", "role": "User" }
   ```
2. Получите токен — **`POST /auth/login`**:
   ```json
   { "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." }
   ```
3. Вставьте токен в Swagger UI нужного сервиса через кнопку **Authorize** → `Bearer <токен>`.

> Токен действителен **15 минут**. По истечении повторите шаг 2.

### Настройка JWT (одинакова во всех сервисах)

```json
"Jwt": {
  "Secret": "we_are_the_nobodies_wanna_be_somebodies",
  "Issuer": "EventBookingPlatform",
  "Audience": "EventBookingPlatformUsers",
  "ExpiryMinutes": 15
}
```

> **Безопасность в продакшне:** используйте криптографически стойкий секрет длиной не менее 64 символов, храните его вне репозитория (переменная окружения, Azure Key Vault и т. п.):
> ```powershell
> $env:Jwt__Secret = "ваш_криптографически_стойкий_секрет_длиной_не_менее_64_символов"
> ```

---

## Документация API

### UserService — `http://localhost:5000`

| Метод | Эндпоинт | Доступ | Описание |
|---|---|---|---|
| POST | `/auth/register` | Публичный | Регистрация пользователя |
| POST | `/auth/login` | Публичный | Аутентификация, возврат JWT |

### EventService — `http://localhost:5001`

| Метод | Эндпоинт | Доступ | Описание |
|---|---|---|---|
| GET | `/events` | Публичный | Список событий (фильтрация + пагинация) |
| GET | `/events/{id}` | Публичный | Событие по ID |
| POST | `/events` | `Admin` | Создать событие |
| PUT | `/events/{id}` | `Admin` | Обновить событие |
| DELETE | `/events/{id}` | `Admin` | Удалить событие |

#### Параметры фильтрации GET /events

- `title` — фильтр по части названия (без учёта регистра).
- `from` / `to` — диапазон дат (`StartAt >= from`, `EndAt <= to`).
- `page` / `pageSize` — пагинация (по умолчанию `1` / `10`).

#### Пример ответа GET /events

```json
{
  "totalCount": 7,
  "items": [
    {
      "id": "9004537c-7940-49ea-b8f8-35e9dd8f03a9",
      "title": "Backend meetup",
      "description": "...",
      "startAt": "2026-08-10T18:00:00Z",
      "endAt": "2026-08-10T21:00:00Z",
      "totalSeats": 100,
      "availableSeats": 97
    }
  ],
  "currentPageNumber": 1,
  "currentPageItemsCount": 1
}
```

### BookingService — `http://localhost:5002`

| Метод | Эндпоинт | Доступ | Описание |
|---|---|---|---|
| POST | `/bookings/events/{eventId}` | Аутентифицированный | Создать бронирование |
| GET | `/bookings/{id}` | Владелец или `Admin` | Статус бронирования |
| DELETE | `/bookings/{id}` | Владелец или `Admin` | Отменить бронирование |

#### Статусы бронирования

| Статус | Описание |
|---|---|
| `Pending` | Создано, ожидает подтверждения фоновым сервисом |
| `Confirmed` | Подтверждено, `BookingConfirmed` опубликован в Kafka |
| `Cancelled` | Отменено пользователем или администратором |

#### Пример ответа POST /bookings/events/{eventId} → 202 Accepted

```json
{
  "id": "12345678-1234-1234-1234-123456789012",
  "eventId": "9004537c-7940-49ea-b8f8-35e9dd8f03a9",
  "userId": "aaaaaaaa-0000-0000-0000-000000000001",
  "status": "Pending",
  "createdAt": "2026-06-28T17:00:00Z",
  "processedAt": null
}
```

### Формат ошибок

Все сервисы возвращают ошибки в формате `ProblemDetails`:

```json
{ "status": 404, "detail": "Бронь с идентификатором ... не найдена." }
```

---

## Структура проекта

Решение `Microservices_EventBooking.slnx`. Каждый сервис следует **Clean Architecture**: `Presentation` → `Application` → `Domain`; `Infrastructure` реализует интерфейсы `Application`.

```
RestFul_API_ASP_NET/
│
├── docker-compose.yml               # Kafka + Zookeeper + 3 × PostgreSQL + 3 × .NET сервиса
├── Microservices_EventBooking.slnx  # Solution-файл
│
├── Shared.Contracts/                # Общие Kafka-контракты
│   └── BookingContracts/
│       ├── BookingConfirmed.cs      # Record-сообщение (BookingId, EventId, UserId, SeatsCount, ConfirmedAt)
│       └── BookingTopics.cs         # Константа топика "booking-confirmed"
│
├── UserService/
│   ├── Dockerfile                   # Сборка контейнера UserService
│   ├── UserService.Domain/          # User, роли, доменные исключения
│   ├── UserService.Application/     # IUserService, JWT-логика, DTOs
│   ├── UserService.Infrastructure/  # UsersDbContext, репозитории, миграции
│   └── UserService.Presentation/    # Program.cs, UsersController (/auth)
│
├── EventService/
│   ├── Dockerfile                   # Сборка контейнера EventService
│   ├── EventService.Domain/         # Event, InboxMessage, исключения
│   ├── EventService.Application/    # IEventService, IEventRepository, IInboxRepository, DTOs
│   ├── EventService.Infrastructure/
│   │   ├── DataAccess/              # EventsDbContext (Events + InboxMessages), репозитории, миграции
│   │   ├── Kafka/
│   │   │   ├── BookingConfirmedConsumer.cs  # BackgroundService-подписчик топика "booking-confirmed"
│   │   │   └── KafkaTopicInitializer.cs     # Создание топика при старте
│   │   └── Redis/
│   │       └── RedisCacheService.cs         # Реализация ICacheService на StackExchange.Redis
│   └── EventService.Presentation/   # Program.cs, EventsController (/events)
│
├── BookingService/
│   ├── Dockerfile                   # Сборка контейнера BookingService
│   ├── BookingService.Domain/       # Booking, BookingStatus, OutboxMessage, исключения
│   ├── BookingService.Application/
│   │   ├── Services/
│   │   │   ├── BookingService.cs              # Логика создания/отмены брони
│   │   │   └── BookingBackgroundService.cs    # Pending → Confirmed + Outbox (каждые 5 с)
│   │   └── Interfaces/              # IBookingService, IBookingRepository, IEventPublisher, IOutboxRepository
│   ├── BookingService.Infrastructure/
│   │   ├── DataAccess/              # BookingsDbContext (Bookings + OutboxMessages), репозитории, миграции
│   │   └── Kafka/
│   │       ├── KafkaEventPublisher.cs   # Реализация IEventPublisher через Kafka-продюсер
│   │       └── OutboxRelayService.cs    # BackgroundService: Outbox → Kafka (каждые 5 с, at-least-once)
│   └── BookingService.Presentation/ # Program.cs, BookingsController (/bookings)
```

### Назначение слоёв (одинаково для всех сервисов)

- **Domain** — сущности и доменные исключения. Нет зависимостей на другие проекты.
- **Application** — бизнес-логика и интерфейсы контрактов. Зависит только от `Domain`.
- **Infrastructure** — EF Core, репозитории, Kafka, миграции. Реализует интерфейсы `Application`.
- **Presentation** — контроллеры, middleware, `Program.cs`. Компонует все слои через DI.