using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using TMDT.Utilities;

namespace TMDT.ViewModels.Buyer
{
    public class SupportChatMessage
    {
        public string Sender { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsUser { get; set; }
    }

    public class BuyerGuideViewModel : ViewModelBase
    {
        private readonly BuyerMainViewModel _mainVm;
        private readonly AiService _aiService;

        private bool _isChatOpen;
        public bool IsChatOpen
        {
            get => _isChatOpen;
            set => SetProperty(ref _isChatOpen, value);
        }

        private string _currentMessage = string.Empty;
        public string CurrentMessage
        {
            get => _currentMessage;
            set => SetProperty(ref _currentMessage, value);
        }

        private bool _isAiTyping;
        public bool IsAiTyping
        {
            get => _isAiTyping;
            set => SetProperty(ref _isAiTyping, value);
        }

        public ObservableCollection<SupportChatMessage> Messages { get; set; }

        public ICommand OpenChatCommand { get; }
        public ICommand CloseChatCommand { get; }
        public ICommand SendMessageCommand { get; }

        public BuyerGuideViewModel(BuyerMainViewModel mainVm)
        {
            _mainVm = mainVm;
            _aiService = new AiService();
            Messages = new ObservableCollection<SupportChatMessage>
            {
                new SupportChatMessage { Sender = "Admin Bot", Content = "Xin chào! Mình là trợ lý ảo của Volox. Mình có thể giúp gì cho bạn hôm nay?", IsUser = false }
            };

            OpenChatCommand = new RelayCommand(_ => IsChatOpen = true);
            CloseChatCommand = new RelayCommand(_ => IsChatOpen = false);
            SendMessageCommand = new RelayCommand(async _ => await SendMessageAsync(), _ => !string.IsNullOrWhiteSpace(CurrentMessage) && !IsAiTyping);
        }

        private async Task SendMessageAsync()
        {
            if (string.IsNullOrWhiteSpace(CurrentMessage) || IsAiTyping) return;

            string userText = CurrentMessage.Trim();
            CurrentMessage = string.Empty;

            // Add user message
            Messages.Add(new SupportChatMessage { Sender = "Bạn", Content = userText, IsUser = true });

            // Prepare chat history for AI
            string chatHistory = "";
            foreach (var msg in Messages)
            {
                string senderName = msg.IsUser ? "Khách hàng" : "Admin";
                chatHistory += $"{senderName}: {msg.Content}\n";
            }

            IsAiTyping = true;
            CommandManager.InvalidateRequerySuggested();

            // Call AI
            string reply = await _aiService.GenerateReplyAsync(chatHistory);

            IsAiTyping = false;
            Messages.Add(new SupportChatMessage { Sender = "Admin Bot", Content = reply, IsUser = false });
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
