using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

namespace Matching.Infrastructure.Persistence;

public static class MongoDbConfigurator
{
    private static bool _isInitialized;
    private static readonly object LockObj = new();

    public static void ConfigureConventions()
    {
        // Kiểm tra tránh việc register lặp lại nếu ứng dụng gọi nhiều lần (ví dụ khi chạy Unit/Integration Test)
        if (_isInitialized)
            return;

        lock (LockObj)
        {
            if (_isInitialized)
                return;

            // 1. Lưu Guid theo chuẩn quốc tế UUIDv4
            BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

            // 2. Lưu decimal dạng Decimal128 (đảm bảo độ chính xác tài chính)
            BsonSerializer.RegisterSerializer(new DecimalSerializer(BsonType.Decimal128));

            // 3. Đăng ký Convention tự động
            var conventionPack = new ConventionPack
            {
                new CamelCaseElementNameConvention(),
                new EnumRepresentationConvention(BsonType.String),
                new IgnoreExtraElementsConvention(true)
            };
            ConventionRegistry.Register("AppConventions", conventionPack, _ => true);

            _isInitialized = true;
        }
    }
}
