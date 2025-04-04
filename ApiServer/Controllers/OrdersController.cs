using ApiServer.Services;
using Common.Data;
using Common.Data.Entities;
using Common.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Channels;
using Common.Data.Enums;
using ApiServer.Utils;

namespace ApiServer.Controllers;

public static class OrdersController
{
    public static IEndpointRouteBuilder UserOrdersController(this IEndpointRouteBuilder builder, IWebHostEnvironment env)
    {
        var group = builder.MapGroup("Orders");
        group.MapPost("/createOrder", CreateOrder);
        if (env.IsDevelopment())
        {
            group.MapGet("/changeWalletAmount", ChangeWalletAmount);
        }
        return builder;
    }
    public static async Task<IResult> CreateOrder([FromBody] CreateOrderRequest req, 
        HttpContext context,
        SqlContext dbContext,
        WalletService walletService,
        ChannelWriter<BitcoinOrder> channelWriter)
    {
        var accountId = context.UserId();
        var account = await dbContext.Accounts.FirstOrDefaultAsync(x => x.AccountId == accountId);
        if (account is null)
        {
            return Results.BadRequest("Account not found");
        }

        var wallet = await walletService.GetOrCreateWallet(account.Id, req.Currency());
        var amount = req.Amount;
        if (req.Type == OrderTypeEnum.Bid)
        {
            amount *= req.Price;
        }
        if (wallet.Amount < amount)
        {
            return Results.BadRequest("Not enough money");
        }

        var bid = new BitcoinOrder(account.Id, req.Type, req.Amount, req.Price);
        await dbContext.BitcoinOrders.AddAsync(bid);
        await dbContext.SaveChangesAsync();

        await channelWriter.WriteAsync(bid);

        return Results.Ok();
    }

    public static async Task<IResult> ChangeWalletAmount(string currency, decimal amount,
        HttpContext context,
        SqlContext dbContext,
        WalletService walletService)
    {
        if (amount < 0)
        {
            return Results.BadRequest("Amount should be greater than 0");
        }
        var accountId = context.UserId();
        var account = await dbContext.Accounts.FirstOrDefaultAsync(x => x.AccountId == accountId);
        if (account is null)
        {
            return Results.BadRequest("Account not found");
        }

        var wallet = await walletService.GetOrCreateWallet(account.Id, currency);
        wallet.Amount = amount;
        await dbContext.SaveChangesAsync();

        return Results.Ok();
    }
}
