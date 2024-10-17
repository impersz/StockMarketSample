using MassTransit;
using PriceService;

var builder = Host.CreateApplicationBuilder(args);

// MassTransit configuration for RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h => { });
        //cfg.ReceiveEndpoint("my-message-queue", e =>
        //{
        //    e.Consumer<MyMessageConsumer>(context);
        //});
    });

});

builder.Services.AddSingleton<PricePublisher>();
builder.Services.AddHostedService<PriceGenerator>();

var host = builder.Build();
host.Run();
