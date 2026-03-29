using System.Reflection;
using RestFulApi.Interfaces;
using RestFulApi.Middleware;
using RestFulApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Регистрация сервисов как Singleton (в памяти для всех запросов)
builder.Services.AddSingleton<IEventService, EventService>();
builder.Services.AddSingleton<IBookingService, BookingService>();
builder.Services.AddSingleton<IBookingTaskQueue, InMemoryBookingTaskQueue>();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

// Глобальная обработка исключений — должна быть зарегистрирована первой в pipeline
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();