namespace Common.Data.Enums;

/// <summary>
/// Represents the type of an order in the order book (either a bid or ask).
/// </summary>
public enum OrderTypeEnum
{
    Unknown = 0,
    Bid = 1,
    Ask = 2
}

/// <summary>
/// Represents the status of an order, such as whether it is open, filled, or cancelled.
/// </summary>
public enum OrderStatusEnum
{
    Unknown = 0,
    Open = 1,
    Filled = 2,
    Cancelled = 3,
    PartiallyCancelled = 4
}

/// <summary>
/// Represents different types of events in the system, such as updates to the order book, wallet balances, or Bitcoin rates.
/// </summary>
public enum EventTypeEnum
{
    OrderBookUpdated = 1,
    [Obsolete]WalletBalancesChanged = 2,
    BitcoinRateChanged = 3
}

/// <summary>
/// Represents different split types for stock market data, such as 15-minute intervals.
/// </summary>
public enum StockMarketSplitTypeEnum
{
    Unknown = 0,
    FifteenMins = 1
    //OneHour = 2
}