using Nexus.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Nexus.Core.Models
{

    public class MediaAttachment : ObservableObject
    {
        public enum MediaType
        {
            Image,
            Video,
        }
        public string Id { get; }
        public string Path { get; set; }
        public MediaType Type { get; set; }
    }
}
