# Booking Platform

Платформа для управления событиями и бронирования билетов. Построена на принципах **чистой архитектуры** и реализована как **набор микросервисов**, обменивающихся данными **только через Apache Kafka**.

## Технологический стек

- Язык: C# / .NET 10
- Базы данных: PostgreSQL + Entity Framework Core (отдельная БД на сервис)
- Оркестрация обмена: Apache Kafka (+ Zookeeper)
- Аутентификация: JWT Bearer Tokens
- Тестирование: xUnit, Testcontainers

## Архитектура

Проект состоит из трёх микросервисов (решение `BookingPlatform.slnx`):

| Сервис | Проект | Порт (http/https) | БД (PostgreSQL) | Назначение |
|--------|--------|-------------------|-----------------|------------|
| EventsService | `EventsService/Presentation` | 5002 / 7002 | `eventsdb` | Управление событиями и местами (`AvailableSeats`), приём подтверждений броней из Kafka |
| BookingsService | `BookingsService/Presentation` | 5003 / 7003 | `bookingsdb` | Бронирование, фоновое подтверждение брон, публикация событий в Kafka |
| UsersService | `UsersService/Presentation` | 5001 / 7001 | `usersdb` | Регистрация / аутентификация / JWT |

Каждый сервис оформлен по слоям: **Domain / Application / Infrastructure / Presentation**.
Общие контракты Kafka вынесены в проект `SharedContracts`.

### Схема обмена данными

```
 UsersService          EventsService             BookingsService              Kafka
  (auth/JWT)            (события, места)          (бронирование)
     │                    │                            │
     │ JWT token          │                            │  POST /api/events/{id}/book
     │───────────────────►│◄───────────────────────────│
     │                    │                             │ запись Booking (Pending)
     │                    │                             │
     │                    │      BookingProcessingHostedService (каждые 5с)
     │                    │             │ booking.Confirm() + PublishBookingConfirmed
     │                    │             │──────────────────────────────────────────────► topic: booking-confirmed
     │                    │  BookingConfirmedConsumer (EventsService)
     │                    │◄────────────────────────────────────────────────────────────│
     │                    │  уменьшение AvailableSeats (TryReserveSeats)
```

Обмен между BookingsService и EventsService происходит **только через Kafka**, без синхронных HTTP-вызовов. Валидацию события (существование, доступность мест, дата) выполняет EventsService асинхронно при обработке сообщения из топика.

**Поток бронирования (асинхронный):**

1. Пользователь вызывает `POST /api/events/{id}/book` → создаётся бронирование со статусом **`Pending`**.
2. Фоновый сервис `BookingProcessingHostedService` каждые ~5 секунд обрабатывает `Pending`-брони: статус меняется на **`Confirmed`** и публикуется сообщение `BookingConfirmed` в топик `booking-confirmed`.
3. EventsService через consumer `BookingConfirmedConsumer` получает сообщение, проверяет доступность места и **уменьшает `AvailableSeats`** события на `SeatCount`. Сообщения обрабатываются идемпотентно (таблица `ProcessedBookings`), а offset коммитится вручную после успешного сохранения.

## Требования к окружению

- .NET SDK 10
- PostgreSQL 12+ (один инстанс, в нём создаются БД `eventsdb`, `bookingsdb`, `usersdb`)
- Apache Kafka + Zookeeper (доступны на `localhost:9092`)

## Быстрый запуск через Docker

Весь стек (Zookeeper, Kafka, PostgreSQL и три микросервиса) поднимается одной командой:

```bash
docker compose up --build
```

- Строки подключения к БД и адрес Kafka для сервисов задаются через переменные окружения в `docker-compose.yml` (сервисы обращаются к контейнерам `postgres` и `kafka` по имени, а не через `localhost`).
- Каждый микросервис собирается из собственного multi-stage `Dockerfile`, а миграции БД применяются автоматически при старте.
- Остановить и удалить контейнеры вместе с volume: `docker compose down -v`.

Swagger каждого сервиса: `http://localhost:5001/swagger`, `http://localhost:5002/swagger`, `http://localhost:5003/swagger`.

## Запуск проекта (без Docker)

1. Убедитесь, что запущены PostgreSQL и Kafka/Zookeeper.

2. Настройте строки подключения и секции конфигурации в `appsettings.json` каждого сервиса:

```jsonc
// EventsService/Presentation/appsettings.json
{
  "ConnectionStrings": { "EventsConnection": "Host=localhost;Port=5432;Database=eventsdb;Username=postgres;Password=postgres" },
  "Kafka": { "BootstrapServers": "localhost:9092" },
  "Jwt": { "Secret": "...", "Issuer": "MyBookingApp", "Audience": "MyBookingAppClients", "LifetimeMinutes": 60 }
}
```

```jsonc
// BookingsService/Presentation/appsettings.json
{
  "ConnectionStrings": { "BookingsConnection": "Host=localhost;Port=5432;Database=bookingsdb;Username=postgres;Password=postgres" },
  "Kafka": { "BootstrapServers": "localhost:9092" },
  "Jwt": { "Secret": "...", "Issuer": "MyBookingApp", "Audience": "MyBookingAppClients", "LifetimeMinutes": 60 }
}
```

```jsonc
// UsersService/Presentation/appsettings.json
{
  "ConnectionStrings": { "UsersConnection": "Host=localhost;Port=5432;Database=usersdb;Username=postgres;Password=postgres" },
  "Jwt": { "Secret": "...", "Issuer": "MyBookingApp", "Audience": "MyBookingAppClients", "LifetimeMinutes": 60 }
}
```

> Примечание: секрет JWT должен совпадать во всех сервисах, чтобы токены, выданные UsersService, принимались остальными.

3. Запустите сервисы (порты и переменные окружения берутся из `Properties/launchSettings.json`):

```bash
dotnet run --project UsersService/Presentation
dotnet run --project EventsService/Presentation
dotnet run --project BookingsService/Presentation
```

При первом запуске миграции БД применяются автоматически (в `Program.cs` каждого сервиса); топик Kafka `booking-confirmed` создаётся автоматически.

Swagger каждого сервиса доступен по адресам:
- UsersService: `http://localhost:5001/swagger`
- EventsService: `http://localhost:5002/swagger`
- BookingsService: `http://localhost:5003/swagger`

## Аутентификация (JWT)

Получите токен через UsersService и передавайте его в заголовке `Authorization: Bearer <token>`.

**Регистрация** — `POST http://localhost:5001/api/auth/register`
```json
{ "login": "user@example.com", "password": "StrongPassword123", "role": "User" }
```

**Вход** — `POST http://localhost:5001/api/auth/login`
```json
{ "login": "user@example.com", "password": "StrongPassword123" }
```
Ответ содержит `token`, который используйте в дальнейших запросах.

## API

### EventsService — события (`http://localhost:5002`)
| Метод | URL | Описание | Доступ |
|-------|-----|----------|--------|
| GET | `/api/events` | Список событий (фильтрация по title/from/to, пагинация) | Все |
| GET | `/api/events/{id}` | Детали события | Все |
| POST | `/api/events` | Создать событие | Admin |
| PUT | `/api/events/{id}` | Обновить событие | Admin |
| DELETE | `/api/events/{id}` | Удалить событие | Admin |

Пример создания события:
```json
{
  "title": "Концерт",
  "description": "Описание",
  "startAt": "2026-09-01T19:00:00Z",
  "endAt": "2026-09-01T21:00:00Z",
  "totalSeats": 100
}
```
Поле `AvailableSeats` при создании приравнивается к `TotalSeats`.

### BookingsService — бронирования (`http://localhost:5003`)
| Метод | URL | Описание | Доступ |
|-------|-----|----------|--------|
| POST | `/api/events/{id}/book` | Создать бронь (статус `Pending`) | User, Admin |
| GET | `/api/bookings/{id}` | Информация о брони | Владелец или Admin |
| DELETE | `/api/bookings/{id}` | Отменить бронь | Владелец или Admin |

## Статусы бронирования

- **`Pending`** — создана, ожидает обработки фоновым сервисом.
- **`Confirmed`** — место зарезервировано, `AvailableSeats` в EventsService уменьшено.
- **`Rejected`** — недостаточно мест либо событие недоступно.
- **`Cancelled`** — отменено владельцем или администратором (**отмена возвращает место**).

> Примечание. Уменьшение `AvailableSeats` происходит **асинхронно** (после подтверждения брони через Kafka). Сразу после `POST /book` значение в БД может не измениться — дождитесь следующего цикла фонового процессора (~5 секунд) и проверки статуса `Confirmed`.

## Бизнес-правила

- Нельзя бронировать событие в прошлом.
- Лимит активных брон на пользователя — 10.
- Нельзя забронировать больше мест, чем доступно (`AvailableSeats`).
- Отмена подтверждённой брони возвращает место событию.

## Тестирование

Проекты тестов:
- Unit/интеграционные тесты служб бронирования и репозиториев: `EventService.Tests`.

```bash
dotnet test EventService.Tests
```
