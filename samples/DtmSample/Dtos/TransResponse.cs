namespace DtmSample.Dtos
{
    public class TransResponse
    {
        public static object BuildSucceedResponse() => new { dtm_result = "SUCCESS" };

        public static object BuildFailureResponse() => new { dtm_result = "FAILURE" };
    }
}
