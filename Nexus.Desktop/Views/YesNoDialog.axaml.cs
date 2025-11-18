using Avalonia.Controls;
using System.Threading.Tasks;

namespace Nexus.Desktop.Views
{
    public partial class YesNoDialog : Window
    {
        private TaskCompletionSource<bool> _tcs = new();

        public YesNoDialog()
        {
            InitializeComponent();
        }

        public void SetMessage(string message)
        {
            var text = this.FindControl<TextBlock>("MessageText");
            text.Text = message;
        }

        public Task<bool> ShowDialogAsync(Window owner)
        {
            this.ShowDialog(owner);
            return _tcs.Task;
        }

        private void OnYesClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            _tcs.TrySetResult(true);
            Close();
        }

        private void OnNoClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            _tcs.TrySetResult(false);
            Close();
        }
    }
}
