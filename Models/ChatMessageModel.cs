using System;

namespace Manage_KPI_or_OKR_System.Models
{
    public class ChatMessageModel
    {
        public string Role { get; set; } // "system", "user", "assistant"
        public string Content { get; set; }
    }
}
