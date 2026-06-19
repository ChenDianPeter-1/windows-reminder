using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using WindowsReminder.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace WindowsReminder.Services;

public class ReminderFileService
{
    private readonly ILogger<ReminderFileService> _logger;
    private readonly IDeserializer _yamlDeserializer;

    public ReminderFileService(ILogger<ReminderFileService> logger)
    {
        _logger = logger;
        _yamlDeserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    /// <summary>
    /// Parses a reminder .md file into a Reminder model.
    /// Only reads — never modifies the file.
    /// </summary>
    public Reminder? ParseFile(string filePath)
    {
        try
        {
            var content = File.ReadAllText(filePath, new UTF8Encoding(false));
            return ParseContent(filePath, content);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read reminder file: {Path}", filePath);
            return null;
        }
    }

    /// <summary>
    /// Parses reminder content from text (useful for testing).
    /// </summary>
    public Reminder? ParseContent(string filePath, string rawContent)
    {
        try
        {
            // Split frontmatter from body: first --- to second ---
            var fm = ExtractFrontmatter(rawContent);
            if (fm == null)
            {
                _logger.LogWarning("No valid frontmatter found in: {Path}", filePath);
                return null;
            }

            var body = rawContent.Substring(rawContent.IndexOf("---", fm.Length + 3) + 3).TrimStart('\r', '\n');

            // Parse YAML frontmatter
            var yaml = _yamlDeserializer.Deserialize<ReminderFrontmatter>(fm);

            var reminder = new Reminder
            {
                FilePath = filePath,
                Status = ParseStatus(yaml.Status),
                Trigger = ParseDateTime(yaml.Trigger),
                Created = ParseDateTime(yaml.Created),
                TaskName = yaml.TaskName ?? "",
            };

            // Extract title from H1
            var titleMatch = Regex.Match(body, @"(?m)^#\s+(.+)$");
            if (titleMatch.Success)
            {
                reminder.Title = titleMatch.Groups[1].Value.Trim();
            }
            else
            {
                reminder.Title = reminder.TaskName;
            }

            // Extract content from **内容**：xxx
            var contentMatch = Regex.Match(body, @"\*\*.+?\*\*[：:]\s*(.+)$", RegexOptions.Multiline);
            if (contentMatch.Success)
            {
                // Get the second match (after **触发时间**)
                var allMatches = Regex.Matches(body, @"\*\*.+?\*\*[：:]\s*(.+)$", RegexOptions.Multiline);
                if (allMatches.Count >= 2)
                {
                    reminder.Content = allMatches[1].Groups[1].Value.Trim();
                }
                else
                {
                    reminder.Content = contentMatch.Groups[1].Value.Trim();
                }
            }

            if (string.IsNullOrEmpty(reminder.Content))
                reminder.Content = reminder.Title;

            _logger.LogInformation("Parsed reminder: {Reminder}", reminder);
            return reminder;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse reminder: {Path}", filePath);
            return null;
        }
    }

    /// <summary>
    /// Writes ONLY the trigger field in the frontmatter.
    /// </summary>
    public bool WriteTrigger(string filePath, DateTime newTrigger)
    {
        try
        {
            var content = File.ReadAllText(filePath, new UTF8Encoding(false));
            var triggerStr = newTrigger.ToString("yyyy-MM-dd HH:mm");
            var regex = new Regex(@"(?m)^trigger:\s*(.+)$");
            var updated = regex.Replace(content, $"trigger: {triggerStr}", 1);

            if (updated == content) { return false; }
            var oldBody = ExtractBody(content);
            var newBody = ExtractBody(updated);
            if (oldBody != newBody) { return false; }

            File.WriteAllText(filePath, updated, new UTF8Encoding(false));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write trigger in {Path}", filePath);
            return false;
        }
    }

    /// <summary>
    /// Writes ONLY the status field in the frontmatter. Does not touch the body.
    /// </summary>
    public bool WriteStatus(string filePath, ReminderStatus newStatus)
    {
        try
        {
            var content = File.ReadAllText(filePath, new UTF8Encoding(false));

            // Replace ONLY the first status line in frontmatter
            var regex = new Regex(@"(?m)^status:\s*(.+)$");
            var updated = regex.Replace(content, $"status: {StatusToString(newStatus)}", 1);

            if (updated == content)
            {
                _logger.LogWarning("Status line not found in {Path}", filePath);
                return false;
            }

            // Verify body integrity: the part after frontmatter should be unchanged
            var oldBody = ExtractBody(content);
            var newBody = ExtractBody(updated);
            if (oldBody != newBody)
            {
                _logger.LogError("Body changed unexpectedly during status write in {Path}", filePath);
                return false;
            }

            File.WriteAllText(filePath, updated, new UTF8Encoding(false));
            _logger.LogInformation("Status updated: {Path} -> {Status}", filePath, newStatus);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write status in {Path}", filePath);
            return false;
        }
    }

    // -- Private helpers --

    private static string? ExtractFrontmatter(string content)
    {
        var trimmed = content.TrimStart('﻿', ' ', '\r', '\n'); // skip BOM
        if (!trimmed.StartsWith("---")) return null;

        var end = trimmed.IndexOf("---", 3);
        if (end < 0) return null;

        return trimmed.Substring(3, end - 3).Trim();
    }

    private static string ExtractBody(string content)
    {
        var trimmed = content.TrimStart('﻿', ' ', '\r', '\n');
        if (!trimmed.StartsWith("---")) return trimmed;

        var end = trimmed.IndexOf("---", 3);
        if (end < 0) return trimmed;

        return trimmed.Substring(end + 3).TrimStart('\r', '\n');
    }

    private static ReminderStatus ParseStatus(string? s) => (s?.Trim().ToLowerInvariant()) switch
    {
        "waiting" => ReminderStatus.Waiting,
        "reminded" => ReminderStatus.Reminded,
        "done" => ReminderStatus.Done,
        "pending" => ReminderStatus.Pending,
        "snoozed" => ReminderStatus.Snoozed,
        _ => ReminderStatus.Unknown
    };

    private static string StatusToString(ReminderStatus s) => s switch
    {
        ReminderStatus.Waiting => "waiting",
        ReminderStatus.Reminded => "reminded",
        ReminderStatus.Done => "done",
        ReminderStatus.Pending => "pending",
        ReminderStatus.Snoozed => "snoozed",
        _ => "unknown"
    };

    private static DateTime? ParseDateTime(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        return DateTime.TryParse(s.Trim(), out var dt) ? dt : null;
    }

    /// <summary>YAML deserialization target</summary>
    private class ReminderFrontmatter
    {
        public string? Status { get; set; }
        public string? Trigger { get; set; }
        public string? Created { get; set; }
        [YamlMember(Alias = "task_name")]
        public string? TaskName { get; set; }
    }
}
