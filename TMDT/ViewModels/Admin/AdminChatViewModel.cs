using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using TMDT.Models;
using TMDT.Utilities;
using System.Windows.Threading;

namespace TMDT.ViewModels.Admin
{
    public class ChatContact : ViewModelBase
    {
        public int ShopId { get; set; }
        public int? UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Avatar { get; set; } = string.Empty;
        public string LastMessage { get; set; } = string.Empty;
        public string TimeDisplay { get; set; } = string.Empty;
        public int ConversationId { get; set; }
    }

    public class ChatMessageWrapper
    {
        public int MessageId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime? SentAt { get; set; }
        public bool IsMine { get; set; }
        public string TimeDisplay => SentAt?.ToString("HH:mm") ?? "";
    }

    public class AdminChatViewModel : ViewModelBase
    {
        private TmdtContext _context;
        private DispatcherTimer _pollTimer;

        public ObservableCollection<ChatContact> Contacts { get; set; } = new ObservableCollection<ChatContact>();
        public ObservableCollection<ChatMessageWrapper> Messages { get; set; } = new ObservableCollection<ChatMessageWrapper>();

        private ChatContact? _selectedContact;
        public ChatContact? SelectedContact
        {
            get => _selectedContact;
            set
            {
                _selectedContact = value;
                OnPropertyChanged();
                LoadMessages();
            }
        }

        private string _messageText = string.Empty;
        public string MessageText
        {
            get => _messageText;
            set { _messageText = value; OnPropertyChanged(); }
        }

        private bool _isOpen = false;
        public bool IsOpen
        {
            get => _isOpen;
            set { _isOpen = value; OnPropertyChanged(); if (value) LoadContacts(); }
        }

        private bool _isAiLoading = false;
        public bool IsAiLoading
        {
            get => _isAiLoading;
            set { _isAiLoading = value; OnPropertyChanged(); }
        }

        private readonly AiService _aiService;

        public ICommand ToggleChatCommand { get; }
        public ICommand SendMessageCommand { get; }
        public ICommand AiSuggestCommand { get; }

        public AdminChatViewModel()
        {
            _aiService = new AiService();

            ToggleChatCommand = new RelayCommand(o => { IsOpen = !IsOpen; });
            SendMessageCommand = new RelayCommand(o => SendMessage(), o => !string.IsNullOrWhiteSpace(MessageText) && SelectedContact != null);
            AiSuggestCommand = new RelayCommand(async o => await GenerateAiSuggestion(), o => SelectedContact != null && !IsAiLoading);

            _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            _pollTimer.Tick += (s, e) => { if (IsOpen && SelectedContact != null) LoadMessages(isPolling: true); };
            _pollTimer.Start();
        }

        private void LoadContacts()
        {
            try
            {
                _context = new TmdtContext();
                var shops = _context.Shops.Include(s => s.User).Where(s => s.IsActive == true).ToList();
                
                Contacts.Clear();
                foreach (var shop in shops)
                {
                    // Find or create conversation mapping
                    var conv = _context.Conversations
                        .FirstOrDefault(c => c.ShopId == shop.ShopId && c.BuyerId == SessionManager.CurrentUser.UserId);

                    string lastMsg = "";
                    string timeStr = "";
                    if (conv != null)
                    {
                        var msg = _context.Messages.Where(m => m.ConversationId == conv.ConversationId).OrderByDescending(m => m.SentAt).FirstOrDefault();
                        if (msg != null)
                        {
                            lastMsg = msg.Content ?? "";
                            timeStr = msg.SentAt?.ToString("dd/MM") ?? "";
                        }
                    }

                    Contacts.Add(new ChatContact
                    {
                        ShopId = shop.ShopId,
                        UserId = shop.UserId,
                        Name = shop.ShopName ?? "Cửa hàng",
                        Avatar = shop.Logo ?? "\xE719", // Shop icon as default
                        LastMessage = string.IsNullOrEmpty(lastMsg) ? "Chưa có tin nhắn" : lastMsg,
                        TimeDisplay = timeStr,
                        ConversationId = conv?.ConversationId ?? 0
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading contacts: {ex.Message}");
            }
        }

        private void LoadMessages(bool isPolling = false)
        {
            if (SelectedContact == null) return;

            try
            {
                using var context = new TmdtContext();
                
                // If conversation doesn't exist yet, we don't have messages
                if (SelectedContact.ConversationId == 0)
                {
                    if (!isPolling) Messages.Clear();
                    return;
                }

                var msgs = context.Messages
                    .Where(m => m.ConversationId == SelectedContact.ConversationId)
                    .OrderBy(m => m.SentAt)
                    .ToList();

                // If polling and no new messages, skip update to avoid flicker
                if (isPolling && msgs.Count == Messages.Count) return;

                Messages.Clear();
                int currentUserId = SessionManager.CurrentUser?.UserId ?? 0;

                foreach (var m in msgs)
                {
                    Messages.Add(new ChatMessageWrapper
                    {
                        MessageId = m.MessageId,
                        Content = m.Content ?? "",
                        SentAt = m.SentAt,
                        IsMine = m.SenderId == currentUserId
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading messages: {ex.Message}");
            }
        }

        private void SendMessage()
        {
            if (SelectedContact == null || string.IsNullOrWhiteSpace(MessageText)) return;

            try
            {
                _context = new TmdtContext();
                int currentUserId = SessionManager.CurrentUser?.UserId ?? 0;

                // Ensure conversation exists
                var conv = _context.Conversations.FirstOrDefault(c => c.ConversationId == SelectedContact.ConversationId);
                if (conv == null)
                {
                    conv = new Conversation
                    {
                        BuyerId = currentUserId,
                        ShopId = SelectedContact.ShopId,
                        CreatedAt = DateTime.Now,
                        LastMessageAt = DateTime.Now
                    };
                    _context.Conversations.Add(conv);
                    _context.SaveChanges();
                    SelectedContact.ConversationId = conv.ConversationId;
                }

                var newMessage = new Message
                {
                    ConversationId = conv.ConversationId,
                    SenderId = currentUserId,
                    Content = MessageText.Trim(),
                    SentAt = DateTime.Now,
                    IsRead = false,
                    MessageType = "Text"
                };

                _context.Messages.Add(newMessage);
                conv.LastMessageAt = DateTime.Now;
                _context.SaveChanges();

                MessageText = string.Empty;
                LoadMessages();
                
                // Update Last message in contact list
                SelectedContact.LastMessage = newMessage.Content;
                SelectedContact.TimeDisplay = newMessage.SentAt?.ToString("HH:mm") ?? "";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error sending message: {ex.Message}");
            }
        }

        private async Task GenerateAiSuggestion()
        {
            if (SelectedContact == null) return;

            IsAiLoading = true;
            try
            {
                // Gom 5 tin nhắn gần nhất để AI đọc ngữ cảnh
                var recentMessages = Messages.Skip(Math.Max(0, Messages.Count - 5)).ToList();
                string chatHistory = "";
                
                foreach (var msg in recentMessages)
                {
                    string sender = msg.IsMine ? "Admin" : SelectedContact.Name;
                    chatHistory += $"{sender}: {msg.Content}\n";
                }

                if (string.IsNullOrWhiteSpace(chatHistory))
                {
                    chatHistory = "Chưa có tin nhắn nào. Hãy tạo một câu chào hỏi lịch sự cho Shop " + SelectedContact.Name;
                }

                string suggestion = await _aiService.GenerateReplyAsync(chatHistory);
                
                // Nếu API Key chưa được cài đặt, nó sẽ trả về thông báo lỗi, mình vẫn hiển thị ra box cho user biết
                MessageText = suggestion;
            }
            finally
            {
                IsAiLoading = false;
            }
        }
    }
}
