namespace DtmSample.Dtos
{
    public class TransRequest
    {
        public TransRequest()
        {
        }

        public TransRequest(string uid, decimal amount)
        { 
            this.UserId = uid;
            this.Amount = amount;
        }

        public string UserId { get; set; }

        public decimal Amount { get; set; }

        public override string ToString()
        {
            return $"UserId={UserId},Amount={Amount}";
        }
    }
}
