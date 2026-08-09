Events API
RESTful API для управления событиями и бронированиями. Приложение построено на принципах чистой архитектуры (Clean Architecture) с четким разделением слоев:

Domain: чистые сущности и исключения.
Application: сервисы (Use Cases), реализующие бизнес-правила.
Infrastructure: доступ к данным (PostgreSQL + EF Core) и фоновая обработка.
API: контроллеры и конфигурация DI.
Запуск проекта
Установите .NET SDK версии 8.0 или выше.
Убедитесь, что запущен экземпляр PostgreSQL (версия 12+).
Клонируйте репозиторий и перейдите в папку решения.
Выполните сборку и запуск:
bash
Копировать
dotnet build
dotnet run --project ./src/AspNetCoreApi.Api
Документация Swagger будет доступна по адресу: https://localhost:{port}/swagger.
Базовый адрес API: https://localhost:{port}/api.
Эндпоинты API
Управление событиями (/api/events)
Метод	URL	Описание
GET	/api/events	Получить пагинированный список событий с фильтрацией.
GET	/api/events/{id}	Получить событие по идентификатору.
POST	/api/events	Создать новое событие.
PUT	/api/events/{id}	Обновить существующее событие.
DELETE	/api/events/{id}	Удалить событие.
Параметры фильтрации и пагинации (GET /api/events):
Все параметры являются необязательными.

Параметр	Тип	Описание
title	string	Поиск по названию (частичное совпадение, без учета регистра).
from	DateTime	События, начинающиеся не раньше указанной даты (ISO 8601).
to	DateTime	События, заканчивающиеся не позже указанной даты (ISO 8601).
page	int	Номер страницы (по умолчанию: 1).
pageSize	int	Размер страницы (по умолчанию: 10).
Управление бронями (/api/events/{id}/book, /api/bookings)
Метод	URL	Описание
POST	/api/events/{eventId}/book	Создать новую бронь для события.
GET	/api/bookings/{bookingId}	Получить статус брони по её ID.
Поведение эндпоинта создания брони:

При успешном создании возвращается HTTP-статус 202 Accepted.
Тело ответа содержит объект созданной брони со статусом Pending.
Заголовок Location указывает на ресурс новой брони (/api/bookings/{bookingId}).
Фоновая обработка броней (Background Service)
В системе работает хостированный сервис PendingBookingProcessor. Он имитирует асинхронное подтверждение брони внешней системой (например, платежным шлюзом).

Алгоритм работы:

Вы создаете бронь через POST /api/events/{id}/book. Статус брони — Pending.
Фоновый сервис каждые несколько секунд сканирует базу данных на наличие броней в статусе Pending.
Для каждой найденной брони сервис проверяет наличие свободных мест на событии.
Если места есть, статус меняется на Confirmed, а поле ProcessedAt заполняется текущей датой.
Если мест нет или событие удалено, статус меняется на Rejected.
Как проверить:
Создайте бронь, подождите 5–7 секунд и выполните запрос GET /api/bookings/{id}. Статус должен измениться с Pending на Confirmed.

Защита от овербукинга (Race Conditions)
Для обеспечения корректности при высокой нагрузке используются примитивы синхронизации:

Lock (Mutex): Используется в методе создания брони внутри сервиса приложения. Гарантирует, что проверка количества мест и их списание происходят как единая неделимая операция.
SemaphoreSlim: Используется в фоновом сервисе. Предотвращает одновременную обработку одной и той же брони несколькими потоками воркера.
Валидация запросов
Модель использует строгую валидацию. Обязательные поля: Title, StartAt, EndAt, TotalSeats.
Правило: EndAt должно быть строго позже StartAt.

При ошибке валидации API возвращает статус 400 Bad Request в формате Problem Details (RFC 7807):

json
Копировать
{
  "type": "about:blank",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "detail": "Дата окончания (EndAt) должна быть позже даты начала (StartAt)."
}
Работа с базой данных (Entity Framework Core Migrations)
Приложение использует PostgreSQL. Схема базы данных управляется через миграции EF Core.

Необходимые инструменты:
Убедитесь, что установлен глобальный инструмент dotnet-ef:

bash
Копировать
dotnet tool install --global dotnet-ef
Настройка строки подключения:
Отредактируйте файл src/AspNetCoreApi.Api/appsettings.json:

json
Копировать
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=eventapi;Username=postgres;Password=your_password"
  }
}
Команды CLI:

Создание новой миграции (выполняется при изменении моделей C#):
bash
Копировать
dotnet ef migrations add <НазваниеМиграции> --project ./src/AspNetCoreApi.Infrastructure --startup-project ./src/AspNetCoreApi.Api
Применение миграций к базе данных:
bash
Копировать
dotnet ef database update --project ./src/AspNetCoreApi.Infrastructure --startup-project ./src/AspNetCoreApi.Api
Тестирование
Проект поддерживает два типа тестов:

Юнит-тесты: Проверяют логику сервисов (Application) в изоляции. Используют In-Memory провайдер EF Core.
Запуск: cd ./test/AspNetCoreApi.Application.Tests && dotnet test
Интеграционные тесты: Проверяют взаимодействие всего стека с реальной БД. Используют библиотеку Testcontainers для автоматического запуска временного контейнера PostgreSQL в Docker.
Запуск: Убедитесь, что Docker запущен, затем выполните dotnet test из корня решения.