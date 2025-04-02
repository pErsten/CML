using Common;
using Common.Data.Entities;
using Common.Dtos;
using Microsoft.AspNetCore.SignalR;

namespace ApiServer.Services
{
    public class SignalRHub : Hub
    {
        /*public async Task SendUserWalletUpdateMessages(List<int> accIds)
        {
            var tasks = accIds.Select(x => Clients.All.SendAsync("WalletUpdate", x));
            await Task.WhenAll(tasks);
        }

        public async Task SendOrdersUpdate(List<BitcoinOrder> openBids, List<BitcoinOrder> openAsks)
        {
            await Clients.All.SendAsync("OrdersUpdate", openBids[^Constants.OrdersShown..], openAsks[^Constants.OrdersShown..]);
        }
        public async Task SendOrdersUpdate(List<BitcoinOrdersDto> openBids, List<BitcoinOrdersDto> openAsks)
        {
            await Clients.All.SendAsync("OrdersUpdate", openBids, openAsks);
        }*/
    }
}
