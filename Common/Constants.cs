namespace Common
{
    public class Constants
    {
        public const string FiatCurrency = "EUR";
        public const string CryptoCurrency = "BTC";

        public const int OrdersShown = 100;
        public static readonly DateTime BitcoinPricesStartDate = new DateTime(2025, 4, 6, 11, 0, 0);

        // Workers
        public const int CurrenciesFetcherDelaySecs = 1;
        public const int OrderBookSnapshotFetcherDelaySecs = 1;
    }
}
