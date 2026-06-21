using System;

namespace TMDT.Messages
{
    public class ToastMessage
    {
        public string Message { get; }

        public ToastMessage(string message)
        {
            Message = message;
        }
    }
}
