namespace Matching.Domain.Enums;

public enum MatchSessionStatus
{
    Searching = 1,
    Matched = 2,
    NoDriver = 3,
    Expired = 4
}

public enum DriverResponse
{
    Accepted = 1,
    Rejected = 2,
    Timeout = 3
}

public enum SurgeType
{
    Time = 1,
    Zone = 2,
    Weather = 3
}

public enum SurgeApplyMode
{
    Multiply = 1,
    Highest = 2
}

public enum WeatherCondition
{
    Rain = 1,
    Storm = 2
}
