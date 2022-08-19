namespace DtmCommon
{
    public class DtmOngingException : DtmException
    {
        public DtmOngingException(string message = ErrOngoing)
            : base(message)
        {
        }
    }
}