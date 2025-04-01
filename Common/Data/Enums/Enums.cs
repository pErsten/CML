namespace Common.Data.Enums;

public enum OrderTypeEnum
{
    Unknown = 0,
    Bid = 1,
    Ask = 2
}

public enum OrderStatusEnum
{
    Unknown = 0,
    Open = 1,
    Filled = 2,
    Cancelled = 3,
    PartiallyCancelled = 4
}