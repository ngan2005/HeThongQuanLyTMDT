using System;

namespace TMDT.Messages
{
    public static class MessageBus
    {
        public static event Action<FlyToCartMessage> OnFlyToCart;

        public static void SendFlyToCart(FlyToCartMessage message)
        {
            OnFlyToCart?.Invoke(message);
        }

        public static event Action<ToastMessage> OnToastMessage;

        public static void SendToast(string message)
        {
            OnToastMessage?.Invoke(new ToastMessage(message));
        }
    }
}
