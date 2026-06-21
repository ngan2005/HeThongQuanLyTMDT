using System.Windows;

namespace TMDT.Messages
{
    public class FlyToCartMessage
    {
        public string SourceImageUrl { get; set; }
        public Rect SourceRect { get; set; }

        public FlyToCartMessage(string sourceImageUrl, Rect sourceRect)
        {
            SourceImageUrl = sourceImageUrl;
            SourceRect = sourceRect;
        }
    }
}
