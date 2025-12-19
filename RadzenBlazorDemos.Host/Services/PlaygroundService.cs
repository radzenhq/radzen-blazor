#nullable enable
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;

namespace RadzenBlazorDemos.Host.Services
{
    public class PlaygroundOptions
    {
        public S3Options S3 { get; set; } = new();
    }

    public class S3Options
    {
        public string ServiceUrl { get; set; } = "";
        public string BucketName { get; set; } = "playground-snippets";
        public string AccessKey { get; set; } = "";
        public string SecretKey { get; set; } = "";
    }

    public class Snippet
    {
        public string Id { get; set; } = "";
        public string Source { get; set; } = "";
        public string EditTokenHash { get; set; } = "";
        public string? ParentId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class SaveSnippetRequest
    {
        public string? Id { get; set; }
        public string Source { get; set; } = "";
        public string? EditToken { get; set; }
    }

    public class SaveSnippetResponse
    {
        public string Id { get; set; } = "";
        public string EditToken { get; set; } = "";
        public bool IsNew { get; set; }
    }

    public class PlaygroundService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;
        private readonly bool _isConfigured;

        public PlaygroundService(IOptions<PlaygroundOptions> options)
        {
            var s3Options = options.Value.S3;
            _bucketName = s3Options.BucketName;
            _isConfigured = !string.IsNullOrEmpty(s3Options.ServiceUrl) 
                         && !string.IsNullOrEmpty(s3Options.AccessKey) 
                         && !string.IsNullOrEmpty(s3Options.SecretKey);

            if (_isConfigured)
            {
                var config = new AmazonS3Config
                {
                    ServiceURL = s3Options.ServiceUrl,
                    ForcePathStyle = true
                };
                _s3Client = new AmazonS3Client(s3Options.AccessKey, s3Options.SecretKey, config);
            }
            else
            {
                // Create a dummy client that won't be used
                _s3Client = null!;
            }
        }

        public bool IsConfigured => _isConfigured;

        public async Task<Snippet?> GetSnippetAsync(string id)
        {
            if (!_isConfigured) return null;

            try
            {
                var response = await _s3Client.GetObjectAsync(_bucketName, $"snippets/{id}.json");
                using var reader = new StreamReader(response.ResponseStream);
                var json = await reader.ReadToEndAsync();
                return JsonSerializer.Deserialize<Snippet>(json, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task<SaveSnippetResponse> SaveSnippetAsync(SaveSnippetRequest request)
        {
            if (!_isConfigured)
            {
                throw new InvalidOperationException("Playground S3 storage is not configured.");
            }

            var now = DateTime.UtcNow;

            // Case 1: New snippet (no id provided)
            if (string.IsNullOrEmpty(request.Id))
            {
                return await CreateNewSnippetAsync(request.Source, null, now);
            }

            // Case 2: Existing snippet - check edit token
            var existingSnippet = await GetSnippetAsync(request.Id);
            
            if (existingSnippet == null)
            {
                // Snippet doesn't exist, create new one
                return await CreateNewSnippetAsync(request.Source, null, now);
            }

            // Verify edit token
            if (!string.IsNullOrEmpty(request.EditToken) && 
                VerifyEditToken(request.EditToken, existingSnippet.EditTokenHash))
            {
                // Valid token - update in place
                return await UpdateSnippetAsync(existingSnippet, request.Source, request.EditToken, now);
            }

            // Invalid or missing token - clone the snippet
            return await CreateNewSnippetAsync(request.Source, request.Id, now);
        }

        private async Task<SaveSnippetResponse> CreateNewSnippetAsync(string source, string? parentId, DateTime now)
        {
            var id = Guid.NewGuid().ToString();
            var editToken = GenerateEditToken();
            var editTokenHash = HashEditToken(editToken);

            var snippet = new Snippet
            {
                Id = id,
                Source = source,
                EditTokenHash = editTokenHash,
                ParentId = parentId,
                CreatedAt = now,
                UpdatedAt = now
            };

            await SaveToS3Async(snippet);

            return new SaveSnippetResponse
            {
                Id = id,
                EditToken = editToken,
                IsNew = true
            };
        }

        private async Task<SaveSnippetResponse> UpdateSnippetAsync(Snippet snippet, string source, string editToken, DateTime now)
        {
            snippet.Source = source;
            snippet.UpdatedAt = now;

            await SaveToS3Async(snippet);

            return new SaveSnippetResponse
            {
                Id = snippet.Id,
                EditToken = editToken,
                IsNew = false
            };
        }

        private async Task SaveToS3Async(Snippet snippet)
        {
            var json = JsonSerializer.Serialize(snippet, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });

            var putRequest = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = $"snippets/{snippet.Id}.json",
                ContentBody = json,
                ContentType = "application/json"
            };

            await _s3Client.PutObjectAsync(putRequest);
        }

        private static string GenerateEditToken()
        {
            var bytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        private static string HashEditToken(string token)
        {
            var bytes = Encoding.UTF8.GetBytes(token);
            var hash = SHA256.HashData(bytes);
            return Convert.ToBase64String(hash);
        }

        private static bool VerifyEditToken(string token, string hash)
        {
            var computedHash = HashEditToken(token);
            return computedHash == hash;
        }
    }
}

