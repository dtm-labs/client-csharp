namespace DtmCommon
{
    public class DtmDuplicatedException : DtmException
    {
        public DtmDuplicatedException(string message = ErrDuplicated)
            : base(message)
        {
        }
    }
}