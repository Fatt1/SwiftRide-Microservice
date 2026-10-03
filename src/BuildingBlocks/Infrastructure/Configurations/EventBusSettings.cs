namespace Infrastructure.Configurations;

public class EventBusSettings
{
    public const string SectionName = "EventBusSettings";

    public string HostAddress { get; set; } = "localhost";
    public ushort Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";

    public string GetConnectionString() => $"amqp://{UserName}:{Password}@{HostAddress}:{Port}";
}
