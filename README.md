# Event Booking Platform — микросервисный бэкенд на ASP.NET Core

## О проекте

Платформа для управления событиями и их бронирования, построенная по микросервисной архитектуре на .NET 10. Каждый сервис — самостоятельное ASP.NET Core-приложение с собственной базой данных. Межсервисное взаимодействие реализовано через Apache Kafka с паттернами **Outbox** (надёжная публикация) и **Inbox** (идемпотентное потребление).

### Используемые технологии

- **.NET 10.0** — последняя версия фреймворка.
- **ASP.NET Core** — высокопроизводительные веб-API.
- **Entity Framework Core 10 + Npgsql** — ORM для PostgreSQL.
- **Apache Kafka (Confluent.Kafka)** — асинхронный обмен сообщениями между сервисами.
- **Swagger (OpenAPI)** — интерактивная документация API.
- **Docker Compose** — поднятие инфраструктуры (Kafka + PostgreSQL).

---

## Состав системы

| Сервис | HTTP-порт | HTTPS-порт | База данных | Kafka роль |
|---|---|---|---|---|
| **UserService** | `5000` | `7000` | `users_db` | — |
| **EventService** | `5001` | `7001` | `events_db` | Consumer (`booking-confirmed`) |
| **BookingService** | `5002` | `7002` | `bookings_db` | Producer (`booking-confirmed`) |
| **Kafka** | `9092` | — | — | Брокер сообщений |

Все три PostgreSQL-базы и Kafka поднимаются одним файлом `docker-kafka_3_db.yml`.

### Описание сервисов

- **UserService** — регистрация и аутентификация пользователей. Выдаёт JWT-токены, которые остальные сервисы используют для проверки прав.
- **EventService** — каталог событий (CRUD). Хранит `events_db` с таблицами `Events` и `InboxMessages`. Подписан на Kafka-топик `booking-confirmed` — при получении события уменьшает `availableSeats` у соответствующего мероприятия.
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

## Инфраструктура (Docker)

Файл `docker-kafka_3_db.yml` поднимает всю необходимую инфраструктуру:

| Контейнер | Образ | Порт |
|---|---|---|
| `eventapi-zookeeper` | `confluentinc/cp-zookeeper:7.6.1` | внутренний `2181` |
| `eventapi-kafka` | `confluentinc/cp-kafka:7.6.1` | `9092` (внешний) |
| `eventapi-users-db` | `postgres:16` | внутренний (volume `users-db-data`) |
| `eventapi-events-db` | `postgres:16` | внутренний (volume `events-db-data`) |
| `eventapi-bookings-db` | `postgres:16` | внутренний (volume `bookings-db-data`) |

Сервисы .NET запускаются **локально** (`dotnet run`) и подключаются к инфраструктуре через `localhost:9092` и `localhost:5432`.

---

## Инструкция по запуску

### Предварительные требования

- **Docker Desktop** (или Docker Engine) — для запуска инфраструктуры.
- **.NET 10.0 SDK** — [скачать с сайта Microsoft](https://dotnet.microsoft.com/download).

### Шаг 1 — Запустить инфраструктуру

```sh
docker compose -f docker-kafka_3_db.yml up -d
```

Команда поднимает Zookeeper, Kafka и три PostgreSQL-базы. Дождитесь, пока все контейнеры перейдут в статус `healthy`:

```sh
docker compose -f docker-kafka_3_db.yml ps
```

### Шаг 2 — Восстановить зависимости и собрать решение

```sh
dotnet restore Microservices_EventBooking.slnx
dotnet build Microservices_EventBooking.slnx
```

### Шаг 3 — Запустить сервисы (каждый в отдельном терминале)

```sh
# Терминал 1 — UserService (http://localhost:5000)
dotnet run --project UserService/UserService.Presentation --launch-profile user_http

# Терминал 2 — EventService (http://localhost:5001)
dotnet run --project EventService/EventService.Presentation --launch-profile events_http

# Терминал 3 — BookingService (http://localhost:5002)
dotnet run --project BookingService/BookingService.Presentation --launch-profile booking-http
```

Миграции EF Core применяются **автоматически** при каждом старте сервиса через `DatabaseMigrationRunner.MigrateIfRelational`.

### Шаг 4 — Проверить Swagger UI

| Сервис | Swagger |
|---|---|
| UserService | http://localhost:5000/swagger |
| EventService | http://localhost:5001/swagger |
| BookingService | http://localhost:5002/swagger |

### Остановить инфраструктуру

```sh
docker compose -f docker-kafka_3_db.yml down
```

Для полной очистки (включая тома с данными):

```sh
docker compose -f docker-kafka_3_db.yml down -v
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
├── docker-kafka_3_db.yml            # Kafka + Zookeeper + 3 × PostgreSQL
├── Microservices_EventBooking.slnx  # Solution-файл
│
├── Shared.Contracts/                # Общие Kafka-контракты
│   └── BookingContracts/
│       ├── BookingConfirmed.cs      # Record-сообщение (BookingId, EventId, UserId, SeatsCount, ConfirmedAt)
│       └── BookingTopics.cs         # Константа топика "booking-confirmed"
│
├── UserService/
│   ├── UserService.Domain/          # User, роли, доменные исключения
│   ├── UserService.Application/     # IUserService, JWT-логика, DTOs
│   ├── UserService.Infrastructure/  # UsersDbContext, репозитории, миграции
│   └── UserService.Presentation/    # Program.cs, UsersController (/auth)
│
├── EventService/
│   ├── EventService.Domain/         # Event, InboxMessage, исключения
│   ├── EventService.Application/    # IEventService, IEventRepository, IInboxRepository, DTOs
│   ├── EventService.Infrastructure/
│   │   ├── DataAccess/              # EventsDbContext (Events + InboxMessages), репозитории, миграции
│   │   └── Kafka/
│   │       ├── BookingConfirmedConsumer.cs  # BackgroundService-подписчик топика "booking-confirmed"
│   │       └── KafkaTopicInitializer.cs     # Создание топика при старте
│   └── EventService.Presentation/   # Program.cs, EventsController (/events)
│
├── BookingService/
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