using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderService.Consumers;
using OrderService.Data;
using OrderService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// PostgreSQL connection
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("OrderDb")));

// Register PriceCache as a singleton
builder.Services.AddSingleton<PriceCache>();

// MassTransit and RabbitMQ configuration
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<PriceUpdateConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h => { });

        cfg.ReceiveEndpoint("price-update-queue", e =>
        {
            e.ConfigureConsumer<PriceUpdateConsumer>(context);
            //e.Consumer<PriceUpdateConsumer>(context);
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
