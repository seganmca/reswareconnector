namespace ReswareConnectorWeb.Config
{
    public class RetentionPolicyConfig
    {
        public int TransactionDataCacheDurationInDays { get; set; } = 30;
        public int TransactionFilesCacheDurationInDays { get; set; } = 30;
        //public bool StoreTransactionFilesForAllTransactions { get; set; } = true;
    }
}
