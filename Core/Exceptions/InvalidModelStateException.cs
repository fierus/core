using System;
using System.Runtime.Serialization;

namespace Core.Exceptions
{
    public class InvalidModelStateException : Exception
    {
        public InvalidModelStateException()
        {
        }

        public InvalidModelStateException(string message) : base(message)
        {
        }

        public InvalidModelStateException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected InvalidModelStateException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}