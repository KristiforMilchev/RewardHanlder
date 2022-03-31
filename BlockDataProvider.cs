namespace MuuCoinRewards
{
    internal class BlockDataProvider
    {
        public static async Task<List<ConnectedUsers>> GetCurrencyHolders(string currency)
        {
            var graphQLClient = new GraphQLHttpClient("https://graphql.bitquery.io/", new GraphQL.Client.Serializer.Newtonsoft.NewtonsoftJsonSerializer());
            graphQLClient.HttpClient.DefaultRequestHeaders.Add("X-API-KEY", "APIKEY");
            var transactionQuery = new GraphQLRequest
            {
                Query = "{ ethereum(network: bsc) { transfers( currency: {is: \"" + currency + "\"} ) { receiver { address } amount count } } }"
            };
            var users = new List<ConnectedUsers>();
            var graphQLResponse = await graphQLClient.SendQueryAsync<MuuHolders>(transactionQuery);
            var holders = graphQLResponse.Data.ethereum.transfers.Select(x => new ConnectedUsers { Account = x.receiver.address, IsWinner = false }).ToList();


            return holders;
        }

        public static async Task<(bool, string, BigInteger)> CheckValidWinner(List<ConnectedUsers> getHodlers, string currency)
        {
            var result = false;
            var random = new Random();
            var index = random.Next(0, getHodlers.Count);
            var element = getHodlers.ElementAt(index);
            // System.Console.WriteLine(questions[index]);
            //var publicKey = element;//"0x7Aa350bEA485A4EA8860AAA42F6D4aFb5b6df2fa";
            var web3 = new Nethereum.Web3.Web3("https://bsc-dataseed.binance.org"); //testnet
            //var web3 = new Nethereum.Web3.Web3("https://bsc-dataseed.binance.org/");

            var balanceOfFunctionMessage = new BalanceOfFunction()
            {
                Owner = element.Account,
            };

            //LSYNC Testnet address: "0xF7CB0cf74D2771f345B29170FC62a10Ec194526a"
            var balanceHandler = web3.Eth.GetContractQueryHandler<BalanceOfFunction>();
            var balance = await balanceHandler.QueryAsync<BigInteger>(currency, balanceOfFunctionMessage);
            if (balance > 0)
                result = true;
            else
                result = false;

            Console.WriteLine(balance.ToString());

            Console.WriteLine(web3);
            RewardSplitter.ConnectedUsers.ElementAt(index).IsWinner = true;
            var totalReward = await BlockDataProvider.GetPoolReward();

            return new(result, balanceOfFunctionMessage.Owner, totalReward);
        }

        public static async Task<BigInteger> GetPoolReward()
        {
            var context = new muucoinContext();
            var publicKey = "0x09799b077BdDd3AA6690d03F5DC9458Fdea6BD69";
            //var web3 = new Nethereum.Web3.Web3("https://data-seed-prebsc-1-s1.binance.org:8545/");
            ////var txCount = await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(publicKey);
            //var balance = await web3.Eth.GetBalance.SendRequestAsync(publicKey);
            //var etherAmount = Web3.Convert.FromWei(balance.Value);
            var web3 = new Nethereum.Web3.Web3("https://bsc-dataseed.binance.org/"); //testnet
            //var web3 = new Nethereum.Web3.Web3("https://bsc-dataseed.binance.org/");

            var balanceOfFunctionMessage = new BalanceOfFunction()
            {
                Owner = publicKey,
            };

            //LSYNC Testnet address: "0xF7CB0cf74D2771f345B29170FC62a10Ec194526a"
            var balanceHandler = web3.Eth.GetContractQueryHandler<BalanceOfFunction>();
            var balance = await balanceHandler.QueryAsync<BigInteger>(context.SystemDefaults.FirstOrDefault().ContractAddress, balanceOfFunctionMessage);


            return balance;
        }


    }
}
