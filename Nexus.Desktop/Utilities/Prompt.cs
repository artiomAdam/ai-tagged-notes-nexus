using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using System.Threading.Tasks;

public static class Prompt
{
    public static async Task<string?> ShowAsync(string title, string message)
    {
        var window = new Window
        {
            Title = title,
            Width = 300,
            Height = 150,
            CanResize = false
        };

        var textBox = new TextBox { Margin = new Thickness(10) };
        var ok = new Button { Content = "OK", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right, Margin = new Thickness(10) };

        ok.Click += (_, _) => window.Close(textBox.Text);

        window.Content = new StackPanel
        {
            Children =
            {
                new TextBlock { Text = message, Margin = new Thickness(10) },
                textBox,
                ok
            }
        };

        return await window.ShowDialog<string?>(Application.Current!.ApplicationLifetime switch
        {
            IClassicDesktopStyleApplicationLifetime desktop => desktop.MainWindow,
            _ => null
        });
    }
}