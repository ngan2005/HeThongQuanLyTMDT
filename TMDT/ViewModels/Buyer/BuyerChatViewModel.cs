using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using TMDT.Models;
using TMDT.Utilities;
using System.Threading.Tasks;

namespace TMDT.ViewModels.Buyer
{
    public class BuyerChatContact : ViewModelBase
    {
        public int ShopId { get; set; }
        public int ConversationId { get; set; }
        public string Name { get; set; } = "";
        public string LogoChar => !string.IsNullOrEmpty(Name) ? Name.Substring(0, 1).ToUpper() : "S";
        public string LastMessage { get; set; } = "";
        public DateTime LastMessageAt { get; set; }
        
        public string TimeDisplay
        {
            get
            {
                var diff = DateTime.Now - LastMessageAt;
                if (diff.TotalDays >= 1) return LastMessageAt.ToString("dd/MM");
                if (diff.TotalHours >= 1) return $"{(int)diff.TotalHours} giờ trước";
                if (diff.TotalMinutes >= 1) return $"{(int)diff.TotalMinutes} phút trước";
                return "Vừa xong";
            }
        }
    }

    public class BuyerChatMessage : ViewModelBase
    {
        public int MessageId { get; set; }
        public string Content { get; set; } = "";
        public bool IsMine { get; set; } // true if sent by Buyer
        public DateTime CreatedAt { get; set; }

        public string TimeDisplay => CreatedAt.ToString("HH:mm");
    }

    public class BuyerChatViewModel : ViewModelBase
    {
        private bool _isOpen;
        private string _messageText = "";
        private BuyerChatContact? _selectedContact;

        public bool IsOpen
        {
            get => _isOpen;
            set => SetProperty(ref _isOpen, value);
        }

        public string MessageText
        {
            get => _messageText;
            set => SetProperty(ref _messageText, value);
        }

        public BuyerChatContact? SelectedContact
        {
            get => _selectedContact;
            set
            {
                if (SetProperty(ref _selectedContact, value))
                {
                    _ = LoadMessagesAsync();
                }
            }
        }

        public ObservableCollection<BuyerChatContact> Contacts { get; } = new();
        public ObservableCollection<BuyerChatMessage> Messages { get; } = new();

        public ICommand ToggleChatCommand { get; }
        public ICommand SendMessageCommand { get; }

        public BuyerChatViewModel()
        {
            ToggleChatCommand = new RelayCommand(_ => {
                IsOpen = !IsOpen;
                if (IsOpen && SessionManager.IsLoggedIn)
                {
                    _ = LoadContactsAsync();
                }
            });

            SendMessageCommand = new RelayCommand(_ => _ = SendMessageAsync(), _ => !string.IsNullOrWhiteSpace(MessageText) && SelectedContact != null);
        }

        public async Task OpenChatWithShopAsync(int shopId)
        {
            if (!SessionManager.IsLoggedIn)
            {
                MessageBox.Show("Vui lòng đăng nhập để sử dụng tính năng Chat.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            IsOpen = true;
            await LoadContactsAsync();

            var contact = Contacts.FirstOrDefault(c => c.ShopId == shopId);
            if (contact != null)
            {
                SelectedContact = contact;
            }
            else
            {
                // Create a new conversation if it doesn't exist
                try
                {
                    using var ctx = new TmdtContext();
                    var shop = await ctx.Shops.FindAsync(shopId);
                    if (shop != null)
                    {
                        var newConv = new Conversation
                        {
                            BuyerId = SessionManager.CurrentUser!.UserId,
                            ShopId = shopId,
                            CreatedAt = DateTime.Now,
                            LastMessageAt = DateTime.Now
                        };
                        ctx.Conversations.Add(newConv);
                        await ctx.SaveChangesAsync();

                        var newContact = new BuyerChatContact
                        {
                            ShopId = shop.ShopId,
                            ConversationId = newConv.ConversationId,
                            Name = shop.ShopName,
                            LastMessage = "Bắt đầu trò chuyện",
                            LastMessageAt = DateTime.Now
                        };

                        Application.Current.Dispatcher.Invoke(() => {
                            Contacts.Insert(0, newContact);
                            SelectedContact = newContact;
                        });
                    }
                }
                catch { }
            }
        }

        public async Task LoadContactsAsync()
        {
            if (!SessionManager.IsLoggedIn) return;

            try
            {
                using var ctx = new TmdtContext();
                var convs = await ctx.Conversations
                    .Include(c => c.Shop)
                    .Where(c => c.BuyerId == SessionManager.CurrentUser!.UserId)
                    .OrderByDescending(c => c.LastMessageAt)
                    .ToListAsync();

                Application.Current.Dispatcher.Invoke(() => {
                    Contacts.Clear();
                    foreach (var c in convs)
                    {
                        if (c.Shop != null)
                        {
                            Contacts.Add(new BuyerChatContact
                            {
                                ShopId = c.Shop.ShopId,
                                ConversationId = c.ConversationId,
                                Name = c.Shop.ShopName,
                                LastMessage = "Tin nhắn mới...", // Ideally from DB, but we keep it simple here or fetch latest
                                LastMessageAt = c.LastMessageAt ?? DateTime.Now
                            });
                        }
                    }
                });
            }
            catch { }
        }

        private async Task LoadMessagesAsync()
        {
            if (SelectedContact == null || !SessionManager.IsLoggedIn) return;

            try
            {
                using var ctx = new TmdtContext();
                var msgs = await ctx.Messages
                    .Where(m => m.ConversationId == SelectedContact.ConversationId)
                    .OrderBy(m => m.SentAt)
                    .ToListAsync();

                Application.Current.Dispatcher.Invoke(() => {
                    Messages.Clear();
                    foreach (var m in msgs)
                    {
                        Messages.Add(new BuyerChatMessage
                        {
                            MessageId = m.MessageId,
                            Content = m.Content ?? "",
                            IsMine = m.SenderId == SessionManager.CurrentUser!.UserId,
                            CreatedAt = m.SentAt ?? DateTime.Now
                        });
                    }
                });
            }
            catch { }
        }

        private async Task SendMessageAsync()
        {
            if (SelectedContact == null || string.IsNullOrWhiteSpace(MessageText) || !SessionManager.IsLoggedIn) return;

            var text = MessageText.Trim();
            MessageText = "";

            try
            {
                using var ctx = new TmdtContext();
                var msg = new Message
                {
                    ConversationId = SelectedContact.ConversationId,
                    SenderId = SessionManager.CurrentUser!.UserId,
                    Content = text,
                    SentAt = DateTime.Now,
                    IsRead = false
                };
                ctx.Messages.Add(msg);
                
                var conv = await ctx.Conversations.FindAsync(SelectedContact.ConversationId);
                if (conv != null)
                {
                    conv.LastMessageAt = DateTime.Now;
                }

                await ctx.SaveChangesAsync();

                var newMsg = new BuyerChatMessage
                {
                    MessageId = msg.MessageId,
                    Content = text,
                    IsMine = true,
                    CreatedAt = msg.SentAt ?? DateTime.Now
                };

                Application.Current.Dispatcher.Invoke(() => {
                    Messages.Add(newMsg);
                    SelectedContact.LastMessageAt = DateTime.Now;
                    SelectedContact.LastMessage = text;
                    OnPropertyChanged(nameof(SelectedContact));
                });
            }
            catch { }
        }
    }
}
