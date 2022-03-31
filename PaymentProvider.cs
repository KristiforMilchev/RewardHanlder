namespace MuuCoinRewards
{
    internal class PaymentProvider
    {
        static System.Timers.Timer t { get; set; }
        static DateTime LastUpdate { get; set; }
        public static List<ConnectedUsers>? ConnectedUsers { get; set; }
        public static List<ConnectedUsers>? WebConnectedUsers { get; set; }
        private static muucoinContext Context { get; set; }
        private static int RewardTime { get; set; }
        public static string ContractAddress { get; set; }

  
        public static async void Init()
        {

            Context = new muucoinContext();
            var systemDefault = Context.SystemDefaults.FirstOrDefault();
            RewardTime = systemDefault.RewardTime.Value;
            ContractAddress = systemDefault.ContractAddress;
            WebConnectedUsers = new List<ConnectedUsers>();
            ConnectedUsers = new List<ConnectedUsers>();
            t = new System.Timers.Timer();
            t.AutoReset = false;
            t.Elapsed += new System.Timers.ElapsedEventHandler(t_Elapsed);
            t.Interval = GetInterval();
            t.Start();

        }

        private static double GetInterval()
        {
            return RewardTime;
        }

        private static void t_Elapsed(object? sender, ElapsedEventArgs e)
        {
            RewardTime = Context.SystemDefaults.FirstOrDefault().RewardTime.Value;

            Console.WriteLine(DateTime.Now.ToString("o"));
            Update();
            t.Interval = GetInterval();
            t.Start();
        }

        [Function("balanceOf", "uint256")]
        public class BalanceOfFunction : FunctionMessage
        {
            [Parameter("address", "_owner", 1)]
            public string Owner { get; set; }
        }

        [Parameter("uint256", "totalSupply")]
        public BigInteger TotalSupply { get; set; }



        [Function("transfer", "bool")]
        public class TransferFunction : FunctionMessage
        {
            [Parameter("address", "_to", 1)]
            public string To { get; set; }

            [Parameter("uint256", "_value", 2)]
            public BigInteger TokenAmount { get; set; }


        }

        public static async void Update()
        {
            try
            {
                if (DateTime.Now > LastUpdate.AddMinutes(-30))
                {
                    ConnectedUsers = await BlockDataProvider.GetCurrencyHolders(ContractAddress);

                    var checkWinner = await BlockDataProvider.CheckValidWinner(ConnectedUsers, ContractAddress);
                    if (!checkWinner.Item1)
                        Update();
                    else
                    {
                        LastUpdate = DateTime.Now;
                        PayReward(checkWinner.Item2, checkWinner.Item3);
                    }
                }

            }
            catch (Exception e)
            {
                System.Console.WriteLine(e.ToString());
            }
        }

        private static async void PayReward(string address, BigInteger amount)
        {
            var context = new muucoinContext();

            var privateKey = "PrivateKey";
            var account = new Nethereum.Web3.Accounts.Account(privateKey, 56);
            // Create endpoint to the testbet !!! TODO replace it with a config file so production systems dont fail

            //var web3 = new Web3(account, "https://bsc-dataseed.binance.org/"); //  testnet
            var web3 = new Web3(account, "https://bsc-dataseed.binance.org");  // mainnet
            var transferHandler = web3.Eth.GetContractTransactionHandler<TransferFunction>();


            var transfer = new TransferFunction()
            {
                FromAddress = "0x09799b077BdDd3AA6690d03F5DC9458Fdea6BD69",
                To = address,
                TokenAmount = amount,
            };

            transfer.GasPrice = Nethereum.Web3.Web3.Convert.ToWei(65, UnitConversion.EthUnit.Gwei);
            //  var estimate = await transferHandler.EstimateGasAsync(ContractAddress, transfer);
            var transactionReceipt = default(TransactionReceipt);
            //    transfer.Gas = estimate.Value;
            var logged = false;
            try
            {
                transactionReceipt = await transferHandler.SendRequestAndWaitForReceiptAsync(ContractAddress, transfer);
            }
            catch (Exception e)
            {
                logged = true;
                context.TransactionLogs.Add(new TransactionLog
                {
                    AccountId = address,
                    Message = e.Message,
                    TxHash = "",
                    DateOfTransaction = DateTime.UtcNow.ToString()
                });
                context.SaveChanges();
            }

            if (transactionReceipt != null)
            {
                var txHash = string.Empty;
                if (!logged)
                {
                    context.TransactionLogs.Add(new TransactionLog
                    {
                        AccountId = address,
                        Message = "Payment Transaction",
                        TxHash = transactionReceipt.TransactionHash,
                        DateOfTransaction = DateTime.UtcNow.ToString()
                    });
                    txHash = transactionReceipt.TransactionHash;
                    context.WinnerLists.Add(new WinnerList
                    {
                        Address = address,
                        DateOfTransaction = DateTime.UtcNow,
                    });
                    context.SaveChanges();
                }
            }
        }
    }
}
