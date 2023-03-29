﻿using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shop.Infrastructure.Database;
using Shop.Infrastructure.Providers.MassTransit.Consumers;
using Shop.Infrastructure.Providers.MassTransit.CouriersActivities;
using Shop.Infrastructure.Providers.MassTransit.StateMachines;

namespace Shop.Infrastructure;

public static class ShopConfiguration
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection serviceCollection,
        IConfiguration configuration)
    {
        const string shopDbContextConnectionString = "POSTGRES";

        serviceCollection.AddDbContext<ShopDbContext>(opt =>
            opt.UseNpgsql(configuration.GetConnectionString(shopDbContextConnectionString)));

        AddMassTransit(serviceCollection, configuration);
        
        return serviceCollection;
    }

    private static IServiceCollection AddMassTransit(IServiceCollection serviceCollection, IConfiguration configuration)
    {
        serviceCollection.AddMassTransit(x =>
        {
            x.AddConsumersFromNamespaceContaining<ShopGetOrdersConsumer>();
            x.AddActivitiesFromNamespaceContaining<OrderAcceptActivity>();
            
            x.AddSagaStateMachine<OrderStateMachine, OrderState>()
                .MongoDbRepository(r =>
                {
                    r.Connection = configuration["MongoDb"];
                    r.DatabaseName = "orderStates";
                });
            
            x.UsingRabbitMq((context, cfg) =>
            {
                const string prefetchCountHeader = "PrefetchCount";

                var prefetchCount = int.Parse(configuration[prefetchCountHeader] ?? string.Empty);
                cfg.PrefetchCount = prefetchCount;

                cfg.Host(configuration["RABBIT_HOST"], "/", h =>
                {
                    h.Username(configuration["RABBIT_LOGIN"]);
                    h.Password(configuration["RABBIT_PASSWORD"]);
                });

                cfg.ConfigureEndpoints(context);
            });
        });

        return serviceCollection;
    }

}