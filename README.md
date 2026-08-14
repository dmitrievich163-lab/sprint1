RESTful API для управления событиями и бронированиями билетов. Приложение построено на принципах чистой архитектуры (Clean Architecture).

Технологический стек
Язык: C# / .NET 8
База данных: PostgreSQL + Entity Framework Core
Архитектура: Clean Architecture (Domain / Application / Infrastructure / Presentation)
Аутентификация: JWT Bearer Tokens
Тестирование: xUnit, Testcontainers
Запуск проекта
Убедитесь, что запущен экземпляр PostgreSQL версии 12+.
Настройте строку подключения в файле appsettings.json:
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=eventapi;Username=postgres;Password=your_password"
  }
}

Примените миграции базы данных:
dotnet ef database update --project ./src/AspNetCoreApi.Infrastructure --startup-project ./src/AspNetCoreApi.Api

Запустите приложение:
dotnet run --project ./src/AspNetCoreApi.Api

Документация Swagger доступна по адресу: https://localhost:{port}/swagger.

Аутентификация и Авторизация (JWT)
API использует JSON Web Token (JWT) для защиты эндпоинтов.

Регистрация пользователя
Эндпоинт регистрации создает нового пользователя.

POST /api/auth/register
Content-Type: application/json

{
  "login": "user@example.com",
  "password": "StrongPassword123",
  "role": "User" 
}

Вход в систему (Получение токена)

POST /api/auth/login
Content-Type: application/json

{
  "login": "user@example.com",
  "password": "StrongPassword123"
}

Ответ:
Успешный ответ вернет объект { "token": "eyJhbGciOi..." }. Этот токен необходимо передавать во всех последующих запросах в заголовке:
Authorization: Bearer <ваш_токен>

Эндпоинты API
Управление событиями (/api/events)
Метод	URL	Описание	Доступ
GET	/api/events	Получить список событий с фильтрацией и пагинацией.	Все
GET	/api/events/{id}	Получить детали события.	Все
POST	/api/events	Создать новое событие.	Только Admin
PUT	/api/events/{id}	Обновить данные события.	Только Admin
DELETE	/api/events/{id}	Удалить событие.	Только Admin
Управление бронями (/api/bookings & /api/events/{id}/book)
Метод	URL	Описание	Доступ
POST	/api/events/{eventId}/book	Забронировать место на событии.	Авторизованные пользователи
DELETE	/api/bookings/{id}	Отменить бронирование.	Владелец ИЛИ Admin
GET	/api/bookings/{id}	Получить информацию о своей броне.	Владелец ИЛИ Admin
Поведение системы подтверждения броней
Система работает асинхронно через Background Service (PendingBookingProcessor):

При создании бронь получает статус Pending.
Фоновый сервис каждые N секунд проверяет Pending-брони.
Если места есть — статус меняется на Confirmed, количество мест уменьшается.
Если мест нет — статус меняется на Rejected.
Для тестирования можно создать несколько броней вручную и подождать один цикл процессора, либо вызвать метод обработки принудительно из тестов.

Конфигурация appsettings.json (Security)
Для работы авторизации обязательно заполните секцию JwtSettings:

"Jwt": {
  "Secret": "ВАШ_СУПЕР_ДЛИННЫЙ_И_СЛУЧАЙНЫЙ_СЕКРЕТНЫЙ_КЛЮЧ_НЕ_МЕНЕЕ_32_СИМВОЛОВ",
  "Issuer": "EventService",
  "Audience": "EventClients",
  "LifetimeMinutes": 60
}

Тестирование
Проект содержит два типа тестов:

Application Layer Tests (Unit):
Проверяют бизнес-правила без запуска веб-сервера.

dotnet test AspNetCoreApi.Application.Tests

Integration Tests (Infrastructure):
Используют Docker-контейнер PostgreSQL (Testcontainers) для проверки реального взаимодействия с БД.

# Убедитесь, что Docker Desktop запущен
dotnet test AspNetCoreApi.IntegrationTests