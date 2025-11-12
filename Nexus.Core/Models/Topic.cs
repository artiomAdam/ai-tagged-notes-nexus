using Nexus.Core.Utilities;
using System.Collections.ObjectModel;

namespace Nexus.Core.Models
{
    public class Topic : ObservableObject
    {
        private string _name = string.Empty;

        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public float[]? Embedding { get; set; }

        public ObservableCollection<Note> Notes { get; } = new();

        public Topic Clone() => (Topic)MemberwiseClone();
    }
}
