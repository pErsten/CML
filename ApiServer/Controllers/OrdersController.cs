using ApiServer.Services;
using Common.Data;
using Common.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Channels;
using Common.Data.Enums;
using ApiServer.Utils;
using Common.Data.Models;

namespace ApiServer.Controllers;

/// <summary>
/// Defines endpoints for managing Bitcoin orders and (in development) wallet balance changes.
/// </summary>
public static class OrdersController
{
    /// <summary>
    /// Maps order-related endpoints to the application's routing pipeline.
    /// Adds a development-only endpoint to manipulate wallet balances.
    /// </summary>
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

    /// <summary>
    /// Creates a new Bitcoin buy (bid) or sell (ask) order for the authenticated user.
    /// Validates wallet balance before order creation and enqueues the order for processing.
    /// </summary>
    /// <param name="req">The order request payload containing order type, amount, and price.</param>
    /// <param name="context">The current HTTP context for extracting user info.</param>
    /// <param name="dbContext">The database context for accessing account and order data.</param>
    /// <param name="walletService">Service for managing user wallets.</param>
    /// <param name="channelWriter">Channel to write the new order for background processing.</param>
    /// <returns>A 200 OK result if the order was successfully created and queued; otherwise, a 400 BadRequest.</returns>
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

    /// <summary>
    /// Development-only endpoint to directly change a user's wallet balance for testing purposes.
    /// </summary>
    /// <param name="currency">The currency type to modify (e.g., USD or BTC).</param>
    /// <param name="amount">The new amount to set in the wallet.</param>
    /// <param name="context">The current HTTP context for extracting user info.</param>
    /// <param name="dbContext">The database context for accessing account and wallet data.</param>
    /// <param name="walletService">Service for retrieving or creating the user's wallet.</param>
    /// <returns>A 200 OK result if the update was successful; otherwise, a 400 BadRequest.</returns>
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
