using Avalonia.Controls;
using Nexus.Core.Models;
using Nexus.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace Nexus.Desktop.ViewModels
{
    public class TopicPickerDialogViewModel : ObservableObject
    {
        public List<Topic> AvailableTopics { get; }
        public Topic? SelectedTopic { get; set; }
        public string NewTopicName { get; set; } = "";

        public ICommand AddNewTopicCommand { get; }
        public ICommand UseSelectedTopicCommand { get; }
        public ICommand CancelCommand { get; }

        private readonly Window _window;

        public TopicPickerDialogViewModel(List<Topic> availableTopics, Window window)
        {
            AvailableTopics = availableTopics.OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase).ToList();
            _window = window;

            AddNewTopicCommand = new RelayCommand(_ => AddNewTopic());
            UseSelectedTopicCommand = new RelayCommand(_ => UseSelected());
            CancelCommand = new RelayCommand(_ => _window.Close(null));
        }

        private void AddNewTopic()
        {
            if (string.IsNullOrWhiteSpace(NewTopicName))
                return;

            var t = new Topic { Name = NewTopicName };
            _window.Close(t);   // <-- return new topic
        }

        private void UseSelected()
        {
            if (SelectedTopic == null)
                return;

            _window.Close(SelectedTopic);  // <-- return existing topic
        }
    }
}
