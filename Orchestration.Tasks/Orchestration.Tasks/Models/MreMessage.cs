using System;

namespace Orchestration.Tasks.Models
{
    public class MreMessage
    {
        public bool IsError { get; set; }
        public string Content { get; set; }
        public Guid JobId { get; set; }
    }
}
