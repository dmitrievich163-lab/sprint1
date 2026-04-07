// 1. Создание строителя
using AspNetCoreApi.Services;

var builder = WebApplication.CreateBuilder(args);

// 2. Регистрация сервисов
builder.Services.AddControllers();
builder.Services.AddScoped<IEventService, EventService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 3. Построение приложения
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
// 4. Настройка middleware
app.UseHttpsRedirection();
app.MapControllers();

// 5. Запуск (слушаем входящие запросы)
app.Run();