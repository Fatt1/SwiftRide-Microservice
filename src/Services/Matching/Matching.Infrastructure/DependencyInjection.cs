using Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Matching.Infrastructure;

public static class DependencyInjection
{
    static DependencyInjection()
    {
        try
        {
            BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
        }
        catch (BsonSerializationException)
        {
            // Already registered
        }
    }

    public static IServiceCollection AddMatchingInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Configure MongoDB Client & Database
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration["MongoDbSettings:ConnectionString"]
            ?? "mongodb://localhost:27017";

        var databaseName = configuration["MongoDbSettings:DatabaseName"] ?? "MatchingDb";

        services.AddSingleton<IMongoClient>(_ => new MongoClient(connectionString));
        services.AddSingleton<IMongoDatabase>(sp =>
        {
            var client = sp.GetRequiredService<IMongoClient>();
            return client.GetDatabase(databaseName);
        });

        // 2. Register MassTransit + RabbitMQ + MongoDB Transactional Outbox
        services.AddCustomMassTransitWithMongoOutbox(configuration);

        // 3. Register MongoDb Initializer (Indexes & Seed)
        services.AddHostedService<Persistence.MongoDbInitializer>();

        return services;
    }
}
