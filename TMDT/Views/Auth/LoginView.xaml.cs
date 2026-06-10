using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using TMDT.ViewModels.Auth;

namespace TMDT.Views.Auth
{
    /// <summary>
    /// Interaction logic for LoginView.xaml
    /// </summary>
    public partial class LoginView : Window
    {
        private Point _targetPupilOffset = new Point(0, 0);
        private Point _currentPupilOffset = new Point(0, 0);
        private const double MaxPupilTravel = 5.0; // Khoảng cách di chuyển tối đa của con ngươi
        private bool _isEyesCovered = false;

        public LoginView()
        {
            InitializeComponent();
            
            // Đăng ký sự kiện tracking chuột và LERP chuyển động mắt
            this.PreviewMouseMove += LoginView_PreviewMouseMove;
            CompositionTarget.Rendering += OnRendering;

            // Đăng ký sự kiện focus trường Password để che mắt
            txtPassword.GotFocus += PasswordField_GotFocus;
            txtPassword.LostFocus += PasswordField_LostFocus;
            txtPasswordPlain.GotFocus += PasswordField_GotFocus;
            txtPasswordPlain.LostFocus += PasswordField_LostFocus;

            // Lắng nghe thay đổi trạng thái đăng nhập từ ViewModel để kích hoạt Mascot feedback
            this.DataContextChanged += LoginView_DataContextChanged;
            if (this.DataContext is LoginViewModel vm)
            {
                vm.PropertyChanged += ViewModel_PropertyChanged;
            }
        }

        private void LoginView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is LoginViewModel oldVm)
            {
                oldVm.PropertyChanged -= ViewModel_PropertyChanged;
            }
            if (e.NewValue is LoginViewModel newVm)
            {
                newVm.PropertyChanged += ViewModel_PropertyChanged;
            }
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender is LoginViewModel vm)
            {
                if (e.PropertyName == nameof(LoginViewModel.IsLoginFailed))
                {
                    if (vm.IsLoginFailed)
                    {
                        var sb = this.Resources["ErrorFeedbackStoryboard"] as Storyboard;
                        sb?.Begin();
                    }
                    else
                    {
                        var sb = this.Resources["ErrorFeedbackResetStoryboard"] as Storyboard;
                        sb?.Begin();
                    }
                }
                else if (e.PropertyName == nameof(LoginViewModel.IsLoginFailed))
                {
                    if (vm.IsLoginFailed)
                    {
                        var sb = this.Resources["ErrorFeedbackStoryboard"] as Storyboard;
                        sb?.Begin();
                    }
                    else
                    {
                        var sb = this.Resources["ErrorFeedbackResetStoryboard"] as Storyboard;
                        sb?.Begin();
                    }
                }
            }
        }

        private void LoginView_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_isEyesCovered || MascotCenter == null)
            {
                _targetPupilOffset = new Point(0, 0);
                return;
            }

            try
            {
                // Lấy tọa độ chuột tương đối với tâm Visor Mascot
                Point mousePos = e.GetPosition(MascotCenter);
                double centerX = MascotCenter.ActualWidth / 2;
                double centerY = MascotCenter.ActualHeight / 2;

                double dirX = mousePos.X - centerX;
                double dirY = mousePos.Y - centerY;
                double distance = Math.Sqrt(dirX * dirX + dirY * dirY);

                if (distance > 0.5)
                {
                    // Giới hạn tỉ lệ dịch chuyển theo khoảng cách chuột (chuột càng xa mắt di chuyển càng sát biên)
                    double factor = Math.Min(distance / 250.0, 1.0);
                    double travel = factor * MaxPupilTravel;
                    _targetPupilOffset = new Point((dirX / distance) * travel, (dirY / distance) * travel);
                }
                else
                {
                    _targetPupilOffset = new Point(0, 0);
                }
            }
            catch
            {
                _targetPupilOffset = new Point(0, 0);
            }
        }

        private void OnRendering(object sender, EventArgs e)
        {
            // Nội suy tuyến tính (LERP) vị trí hiện tại đến vị trí đích tạo quán tính mượt mà
            double lerpFactor = 0.12; 
            double newX = _currentPupilOffset.X + (_targetPupilOffset.X - _currentPupilOffset.X) * lerpFactor;
            double newY = _currentPupilOffset.Y + (_targetPupilOffset.Y - _currentPupilOffset.Y) * lerpFactor;
            _currentPupilOffset = new Point(newX, newY);

            if (leftPupilTransform != null)
            {
                leftPupilTransform.X = newX;
                leftPupilTransform.Y = newY;
            }
            if (rightPupilTransform != null)
            {
                rightPupilTransform.X = newX;
                rightPupilTransform.Y = newY;
            }
        }

        private void PasswordField_GotFocus(object sender, RoutedEventArgs e)
        {
            if (_isEyesCovered) return;
            _isEyesCovered = true;
            _targetPupilOffset = new Point(0, 0);

            var sb = this.Resources["CoverEyesStoryboard"] as Storyboard;
            sb?.Begin();
        }

        private void PasswordField_LostFocus(object sender, RoutedEventArgs e)
        {
            // Chỉ bỏ che mắt nếu không có ô password nào đang được focus
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!txtPassword.IsFocused && !txtPasswordPlain.IsFocused)
                {
                    _isEyesCovered = false;
                    var sb = this.Resources["UncoverEyesStoryboard"] as Storyboard;
                    sb?.Begin();
                }
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            CompositionTarget.Rendering -= OnRendering; // Hủy đăng ký tránh rò rỉ bộ nhớ
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;

            // Cho phép kéo thả cửa sổ từ bất kỳ vị trí trống nào (không click vào control)
            var source = e.OriginalSource as DependencyObject;
            while (source != null)
            {
                if (source is System.Windows.Controls.TextBox ||
                    source is System.Windows.Controls.PasswordBox ||
                    source is System.Windows.Controls.Button ||
                    source is System.Windows.Controls.CheckBox ||
                    source is System.Windows.Controls.ScrollViewer)
                    return;
                source = System.Windows.Media.VisualTreeHelper.GetParent(source);
            }
            DragMove();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void GoToRegister_Click(object sender, MouseButtonEventArgs e)
        {
            RegisterView register = new RegisterView();
            register.Show();
            this.Close();
        }

        private void txtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            // Được xử lý tự động qua XAML Binding
        }

        private void TogglePassword_Click(object sender, RoutedEventArgs e)
        {
            if (txtPassword.Visibility == Visibility.Visible)
            {
                // Chuyển sang hiển thị mật khẩu bằng TextBox thông thường
                txtPassword.Visibility = Visibility.Collapsed;
                txtPasswordPlain.Visibility = Visibility.Visible;
                
                // Đồng bộ giá trị từ PasswordBox sang TextBox
                txtPasswordPlain.Text = txtPassword.Password;
                txtPasswordPlain.Focus();
                
                // Thay đổi icon mắt thành icon ẩn mật khẩu (icon mắt có gạch chéo \uF270)
                tbEyeIcon.Text = "\uF270";
            }
            else
            {
                // Chuyển về ẩn mật khẩu bằng PasswordBox
                txtPasswordPlain.Visibility = Visibility.Collapsed;
                txtPassword.Visibility = Visibility.Visible;
                
                // Đồng bộ giá trị từ TextBox sang PasswordBox
                txtPassword.Password = txtPasswordPlain.Text;
                txtPassword.Focus();
                
                // Thay đổi icon mắt về bình thường (\uE7B3)
                tbEyeIcon.Text = "\uE7B3";
            }
        }
    }
}
