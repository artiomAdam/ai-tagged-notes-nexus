using Nexus.Core.Utilities;
using System.Collections.ObjectModel;

namespace Nexus.Core.Models
{
    public class Note : ObservableObject
    {
        private string _title = string.Empty;
        private string _content = string.Empty;
        private DateTime _createdAt = DateTime.UtcNow;
        private DateTime _updatedAt = DateTime.UtcNow;
        private string? _parentId = null;
        public string? ParentId
        {
            get => _parentId;
            set => SetProperty(ref _parentId, value);
        }
        
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string Content
        {
            get => _content;
            set => SetProperty(ref _content, value);
        }

        public DateTime CreatedAt
        {
            get => _createdAt;
            set => SetProperty(ref _createdAt, value);
        }

        public DateTime UpdatedAt
        {
            get => _updatedAt;
            set => SetProperty(ref _updatedAt, value);
        }

        public bool HasUnsavedChanges { get; set; }

        public ObservableCollection<Note> Children { get; set; } = new();


        public Note Clone() => (Note)MemberwiseClone();
    }
}
