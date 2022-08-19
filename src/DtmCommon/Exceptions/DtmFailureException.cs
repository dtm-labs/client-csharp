namespace DtmCommon
{
    public class DtmFailureException : DtmException
    {
        public DtmFailureException(string message = ErrFailure)
            : base(message)
        {
        }
    }
}