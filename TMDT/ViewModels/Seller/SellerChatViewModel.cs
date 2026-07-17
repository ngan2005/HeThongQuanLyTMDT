using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using TMDT.Models;
using TMDT.Utilities;

namespace TMDT.ViewModels.Seller
{
    public class ConversationItem : ViewModelBase
    {
        public Conversation ConversationData { get; set; } = null!;

        public string BuyerName => ConversationData.Buyer?.FullName ?? "Khách hàng";
        public string BuyerAvatar => ConversationData.Buyer?.Avatar ?? "/Assets/default_avatar.png";
        
        public string LastMessageSnippet
        {
            get
            {
                var lastMsg = ConversationData.Messages?.OrderByDescending(m => m.SentAt).FirstOrDefault();
                if (lastMsg == null) return "Chưa có tin nhắn";
                string prefix = lastMsg.SenderId == SessionManager.CurrentUser?.UserId ? "Bạn: " : "";
                string content = lastMsg.Content ?? "";
                if (content.Length > 25) content = content.Substring(0, 25) + "...";
                return prefix + content;
            }
        }

        public string LastMessageTime
        {
            get
            {
                var dt = ConversationData.LastMessageAt;
                if (!dt.HasValue) return "";
                if (dt.Value.Date == DateTime.Today) return dt.Value.ToString("HH:mm");
                return dt.Value.ToString("dd/MM");
            }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        public void RefreshDisplay()
        {
            OnPropertyChanged(nameof(LastMessageSnippet));
            OnPropertyChanged(nameof(LastMessageTime));
        }
    }

    public class MessageItem : ViewModelBase
    {
        public Message MessageData { get; set; } = null!;
        public bool IsMyMessage => MessageData.SenderId == SessionManager.CurrentUser?.UserId;
        public string Content => MessageData.Content ?? "";
        public string SentTime => MessageData.SentAt?.ToString("HH:mm") ?? "";
        public string SenderAvatar => IsMyMessage ? (SessionManager.CurrentUser?.Avatar ?? "/Assets/default_avatar.png") 
                                                  : (MessageData.Sender?.Avatar ?? "/Assets/default_avatar.png");
    }

    public class SellerChatViewModel : ViewModelBase
    {
        private readonly TmdtContext _context = null!;
        private readonly AiService _aiService = new AiService();

        private ObservableCollection<string> _aiReplySuggestions = new();
        public ObservableCollection<string> AIReplySuggestions
        {
            get => _aiReplySuggestions;
            set { _aiReplySuggestions = value; OnPropertyChanged(); }
        }

        private bool _isAILoading;
        public bool IsAILoading
        {
            get => _isAILoading;
            set { _isAILoading = value; OnPropertyChanged(); }
        }

        private bool _isAiDrafting;
        public bool IsAiDrafting
        {
            get => _isAiDrafting;
            set { _isAiDrafting = value; OnPropertyChanged(); }
        }

        private ObservableCollection<ConversationItem> _conversations = new();
        public ObservableCollection<ConversationItem> Conversations
        {
            get => _conversations;
            set { _conversations = value; OnPropertyChanged(); }
        }

        private ObservableCollection<MessageItem> _messages = new();
        public ObservableCollection<MessageItem> Messages
        {
            get => _messages;
            set { _messages = value; OnPropertyChanged(); }
        }

        private ConversationItem? _selectedConversation;
        public ConversationItem? SelectedConversation
        {
            get => _selectedConversation;
            set 
            { 
                if (_selectedConversation != null) _selectedConversation.IsSelected = false;
                _selectedConversation = value; 
                if (_selectedConversation != null) _selectedConversation.IsSelected = true;
                
                AIReplySuggestions.Clear();
                
                OnPropertyChanged(); 
                _ = LoadMessagesAsync();
            }
        }

        private string _messageInput = "";
        public string MessageInput
        {
            get => _messageInput;
            set { _messageInput = value; OnPropertyChanged(); }
        }

        public ICommand SendMessageCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand GenerateAISuggestionsCommand { get; }
        public ICommand UseAISuggestionCommand { get; }
        public ICommand AutoDraftMessageWithAICommand { get; }

        public SellerChatViewModel()
        {
            try { _context = new TmdtContext(); } catch { }

            SendMessageCommand = new RelayCommand(async _ => await ExecuteSendMessageAsync());
            RefreshCommand = new RelayCommand(async _ => await LoadConversationsAsync());
            GenerateAISuggestionsCommand = new RelayCommand(async _ => await ExecuteGenerateAISuggestionsAsync());
            UseAISuggestionCommand = new RelayCommand(ExecuteUseAISuggestion!);
            AutoDraftMessageWithAICommand = new RelayCommand(async _ => await ExecuteAutoDraftMessageWithAIAsync());

            _ = LoadConversationsAsync();
        }

        private int GetCurrentShopId()
        {
            return SessionManager.CurrentUser?.ShopId ?? 0;
        }

        private async Task LoadConversationsAsync()
        {
            int shopId = GetCurrentShopId();
            if (shopId <= 0 || _context == null) return;

            try
            {
                // Optimize: Use AsNoTracking and only include the last message to avoid loading all messages
                var query = await _context.Conversations
                    .AsNoTracking()
                    .Include(c => c.Buyer)
                    .Include(c => c.Messages.OrderByDescending(m => m.SentAt).Take(1))
                    .Where(c => c.ShopId == shopId)
                    .OrderByDescending(c => c.LastMessageAt)
                    .ToListAsync();

                var currentSelectedId = SelectedConversation?.ConversationData.ConversationId;

                // Update UI on Main Thread
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Conversations.Clear();
                    foreach (var conv in query)
                    {
                        var item = new ConversationItem { ConversationData = conv };
                        if (currentSelectedId.HasValue && item.ConversationData.ConversationId == currentSelectedId.Value)
                        {
                            item.IsSelected = true;
                            _selectedConversation = item;
                            OnPropertyChanged(nameof(SelectedConversation));
                        }
                        Conversations.Add(item);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("LoadConversationsAsync Error: " + ex.Message);
            }
        }

        private async Task LoadMessagesAsync()
        {
            Application.Current.Dispatcher.Invoke(() => Messages.Clear());
            
            if (SelectedConversation == null || _context == null) return;

            try
            {
                int conversationId = SelectedConversation.ConversationData.ConversationId;
                var query = await _context.Messages
                    .AsNoTracking()
                    .Include(m => m.Sender)
                    .Where(m => m.ConversationId == conversationId)
                    .OrderBy(m => m.SentAt)
                    .ToListAsync();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var msg in query)
                    {
                        Messages.Add(new MessageItem { MessageData = msg });
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("LoadMessagesAsync Error: " + ex.Message);
            }
        }

        private async Task ExecuteSendMessageAsync()
        {
            if (SelectedConversation == null || string.IsNullOrWhiteSpace(MessageInput)) return;

            int sellerId = SessionManager.CurrentUser?.UserId ?? 0;
            if (sellerId <= 0 || _context == null) return;

            try
            {
                var newMsg = new Message
                {
                    ConversationId = SelectedConversation.ConversationData.ConversationId,
                    SenderId = sellerId,
                    Content = MessageInput.Trim(),
                    MessageType = "Text",
                    SentAt = DateTime.Now,
                    IsRead = false
                };

                _context.Messages.Add(newMsg);

                // Update LastMessageAt
                var dbConv = await _context.Conversations.FindAsync(SelectedConversation.ConversationData.ConversationId);
                if (dbConv != null)
                {
                    dbConv.LastMessageAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                Application.Current.Dispatcher.Invoke(() => MessageInput = "");
                
                await LoadMessagesAsync();
                
                Application.Current.Dispatcher.Invoke(() => SelectedConversation.RefreshDisplay());
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() => MessageBox.Show("Lỗi gửi tin nhắn: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        private void ExecuteUseAISuggestion(object suggestionObj)
        {
            if (suggestionObj is string suggestion && !string.IsNullOrWhiteSpace(suggestion))
            {
                MessageInput = suggestion;
            }
        }

        private async Task ExecuteGenerateAISuggestionsAsync()
        {
            if (SelectedConversation == null) return;
            
            Application.Current.Dispatcher.Invoke(() => 
            {
                IsAILoading = true;
                AIReplySuggestions.Clear();
            });
            
            var lastBuyerMsg = Messages.LastOrDefault(m => !m.IsMyMessage)?.Content;
            
            string response = await _aiService.SuggestChatRepliesAsync(lastBuyerMsg ?? "");
            
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (!string.IsNullOrWhiteSpace(response))
                {
                    var parts = response.Split('|', StringSplitOptions.RemoveEmptyEntries);
                    foreach(var p in parts)
                    {
                        AIReplySuggestions.Add(p.Trim());
                    }
                }
                IsAILoading = false;
            });
        }

        private async Task ExecuteAutoDraftMessageWithAIAsync()
        {
            if (SelectedConversation == null || IsAiDrafting) return;

            IsAiDrafting = true;
            MessageInput = "AI đang suy nghĩ và soạn câu trả lời...";

            try
            {
                // Lấy 8 tin nhắn gần nhất để làm ngữ cảnh hội thoại cho AI
                var recentMsgs = Messages.TakeLast(8).ToList();
                var historyBuilder = new System.Text.StringBuilder();

                foreach (var msg in recentMsgs)
                {
                    string senderName = msg.IsMyMessage ? "Chủ Shop (Bạn)" : "Khách hàng";
                    historyBuilder.AppendLine($"{senderName}: {msg.Content}");
                }

                string chatHistory = historyBuilder.ToString();
                
                // Gọi API Gemini soạn câu trả lời đầy đủ
                string aiDraft = await _aiService.GenerateReplyAsync(chatHistory);

                if (!string.IsNullOrWhiteSpace(aiDraft))
                {
                    MessageInput = aiDraft;
                }
                else
                {
                    MessageInput = "";
                }
            }
            catch (Exception ex)
            {
                MessageInput = "";
                MessageBox.Show($"Lỗi gọi trợ lý AI: {ex.Message}", "Lỗi AI", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                IsAiDrafting = false;
            }
        }
    }
}
