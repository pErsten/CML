using System.Diagnostics;
using Common.Data.Enums;

namespace Common.Data.Dtos;

public class CreateOrderRequest
{
    public decimal Amount { get; set; }
    public decimal Price { get; set; }
    public OrderTypeEnum Type { get; set; }

    public string Currency() => Type switch
    {
        OrderTypeEnum.Ask => Constants.CryptoCurrency,
        OrderTypeEnum.Bid => Constants.FiatCurrency,
        _ => throw new NotImplementedException()
    };
}