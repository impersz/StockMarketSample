using MassTransit;
using Microsoft.EntityFrameworkCore;
using PortfolioService.Consumers;
using PortfolioService.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// PostgreSQL connection
builder.Services.AddDbContext<PortfolioDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PortfolioDb")));

// MassTransit and RabbitMQ configuration
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<PriceUpdateConsumer>();
    x.AddConsumer<OrderPlacedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h => { });

        cfg.ReceiveEndpoint("order-placed-queue", e =>
        {
            e.ConfigureConsumer<OrderPlacedConsumer>(context);
        });

        cfg.ReceiveEndpoint("price-update-queue", e =>
        {
            e.ConfigureConsumer<PriceUpdateConsumer>(context);
        });
    });

    x.SetKebabCaseEndpointNameFormatter();
});

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
