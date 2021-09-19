namespace Core.Models
{
    public class ErrorModel
    {
        public string Message { get; set; }
        public string ExceptionType { get; set; }
        public string StackTrace { get; set; }
    }
}
