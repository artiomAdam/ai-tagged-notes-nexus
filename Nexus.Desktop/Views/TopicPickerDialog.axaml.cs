using Avalonia.Controls;
using Nexus.Core.Models;
using Nexus.Desktop.ViewModels;
using System.Collections.Generic;

namespace Nexus.Desktop.Views;

public partial class TopicPickerDialog : Window
{
    public TopicPickerDialog(List<Topic> availableTopics)
    {
        InitializeComponent();

        DataContext = new TopicPickerDialogViewModel(availableTopics, this);
    }
}