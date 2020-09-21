using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RadzenBlazorDemos.Models.GitHub
{
    public class Issue
    {
        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("closed_at")]
        public DateTime? ClosedAt { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("labels")]
        public IEnumerable<Label> Labels { get; set; }

        [JsonPropertyName("user")]
        public User User { get; set; }

        [JsonPropertyName("assignees")]
        public IEnumerable<User> Assignees { get; set; }

        [JsonPropertyName("state")]
        public IssueState State { get; set; }
    }
}