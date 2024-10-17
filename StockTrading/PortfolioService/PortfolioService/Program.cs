using MassTransit;
using Microsoft.EntityFrameworkCore;
using PortfolioService.Consumers;
using PortfolioService.Data;
using PortfolioService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// PostgreSQL connection
builder.Services.AddDbContext<PortfolioDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PortfolioDb")));


builder.Services.AddHostedService<OrderPlacedRabbitMqConsumer>();
builder.Services.AddHostedService<PriceUpdateRabbitMqConsumer>();


builder.Services.AddSingleton<PriceCache>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
