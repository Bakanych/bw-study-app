using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace StudyApp.Models;

public enum Subject
{
    Math,
    Chemistry,
    Physics
}

public class User
{
    [Key] public int UserId { get; init; }

    [Required] [StringLength(100)] public string Name { get; init; } = string.Empty;

    [JsonIgnore] // Don't serialize this to avoid circular references
    public ICollection<StudyGroupMember> Memberships { get; init; } = new List<StudyGroupMember>();
}

public class StudyGroup
{
    [Key] public int StudyGroupId { get; init; }

    [Required]
    [StringLength(30, MinimumLength = 5)]
    public string Name { get; init; } = string.Empty;

    [Required] public Subject Subject { get; init; }

    public DateTime CreateDate { get; init; } = DateTime.UtcNow;

    public ICollection<StudyGroupMember> Members { get; init; } = new List<StudyGroupMember>();
}

public class StudyGroupMember
{
    [JsonIgnore] // Don't serialize - redundant in context
    public int StudyGroupId { get; init; }

    [JsonIgnore] // Don't serialize back-reference to avoid circular reference
    public StudyGroup? StudyGroup { get; init; }

    public int UserId { get; init; }

    [JsonIgnore] // Don't serialize the nested object - we'll flatten it
    public User? User { get; init; }

    // JSON-only property that exposes userName from the nested User object
    public string UserName => User?.Name ?? string.Empty;
}