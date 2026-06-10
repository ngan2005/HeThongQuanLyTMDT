using System.Windows.Controls;
using TMDT.ViewModels.Admin;

namespace TMDT.Views.Admin
{
    public partial class AdminChatWidget : UserControl
    {
        public AdminChatWidget()
        {
            InitializeComponent();
            
            // Auto scroll to bottom when new messages arrive
            DataContextChanged += (s, e) =>
            {
                if (DataContext is AdminChatViewModel vm)
                {
                    vm.Messages.CollectionChanged += (s2, e2) =>
                    {
                        if (e2.NewItems != null && e2.NewItems.Count > 0)
                        {
                            MessagesScrollViewer.ScrollToEnd();
                        }
                    };
                }
            };
        }
    }
}
