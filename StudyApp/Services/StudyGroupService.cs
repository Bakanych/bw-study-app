using Microsoft.EntityFrameworkCore;
using StudyApp.Models;

namespace StudyApp.Services;

public interface IStudyGroupService
{
    Task<StudyGroup?> GetByIdAsync(int id);
    Task<StudyGroup> CreateGroupAsync(string name, Subject subject, IEnumerable<int> memberUserIds);
    Task<bool> JoinGroupAsync(int groupId, int userId);
    Task<bool> LeaveGroupAsync(int groupId, int userId);
    Task<List<StudyGroup>> SearchAsync(Subject? subject);
    Task<List<StudyGroup>> GetAllAsync();
    Task<bool> DeleteGroupAsync(int id);
}

public class StudyGroupService(AppDbContext db) : IStudyGroupService
{
    public async Task<StudyGroup?> GetByIdAsync(int id)
    {
        return await db.StudyGroups
            .Include(sg => sg.Members)
            .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(sg => sg.StudyGroupId == id);
    }

    public async Task<StudyGroup> CreateGroupAsync(string name, Subject subject, IEnumerable<int> memberUserIds)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length < 5 || name.Length > 30)
            throw new ArgumentException("Invalid name length");

        var memberIds = memberUserIds?.Distinct().ToList() ?? [];
        if (memberIds.Count > 0)
        {
            // ensure all users exist
            var existingIds =
                await db.Users.Where(u => memberIds.Contains(u.UserId)).Select(u => u.UserId).ToListAsync();
            if (existingIds.Count != memberIds.Count) throw new InvalidOperationException("Some users do not exist");

            // uniqueness: none of the users can already be in a group for the subject
            if (await AreUsersInSubjectGroupAsync(memberIds, subject))
                throw new InvalidOperationException("One or more users already in a group for this subject");
        }

        var group = new StudyGroup
        {
            Name = name,
            Subject = subject,
            CreateDate = DateTime.UtcNow
        };
        db.StudyGroups.Add(group);
        await db.SaveChangesAsync();

        if (memberIds.Count > 0)
        {
            foreach (var uid in memberIds)
                db.StudyGroupMembers.Add(new StudyGroupMember { StudyGroupId = group.StudyGroupId, UserId = uid });
            await db.SaveChangesAsync();

            // Reload the group with Members to return complete data
            var groupWithMembers = await db.StudyGroups
                .Include(g => g.Members)
                .ThenInclude(m => m.User)
                .FirstAsync(g => g.StudyGroupId == group.StudyGroupId);
            return groupWithMembers;
        }

        return group;
    }

    public async Task<bool> JoinGroupAsync(int groupId, int userId)
    {
        var group = await db.StudyGroups.FindAsync(groupId)
                    ?? throw new InvalidOperationException("Group does not exist");

        var userExists = await db.Users.AnyAsync(u => u.UserId == userId);
        if (!userExists)
            throw new InvalidOperationException("User does not exist");

        // user can have only one group per subject
        var alreadyInSubject = await db.StudyGroupMembers
            .Where(m => m.UserId == userId)
            .Join(db.StudyGroups, m => m.StudyGroupId, g => g.StudyGroupId, (m, g) => g.Subject)
            .AnyAsync(s => s == group.Subject);
        if (alreadyInSubject) return false;

        var exists = await db.StudyGroupMembers.AnyAsync(m => m.StudyGroupId == groupId && m.UserId == userId);
        if (!exists)
        {
            db.StudyGroupMembers.Add(new StudyGroupMember { StudyGroupId = groupId, UserId = userId });
            await db.SaveChangesAsync();
        }

        return true;
    }

    public async Task<bool> LeaveGroupAsync(int groupId, int userId)
    {
        var groupExists = await db.StudyGroups.AnyAsync(g => g.StudyGroupId == groupId);
        if (!groupExists) return false;

        var membership =
            await db.StudyGroupMembers.FirstOrDefaultAsync(m => m.StudyGroupId == groupId && m.UserId == userId);
        if (membership is null) return true; // Idempotent - already not a member
        db.StudyGroupMembers.Remove(membership);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<List<StudyGroup>> SearchAsync(Subject? subject)
    {
        var query = db.StudyGroups
            .Include(g => g.Members)
            .ThenInclude(m => m.User)
            .AsQueryable();

        if (subject.HasValue)
            query = query.Where(g => g.Subject == subject.Value);

        return await query.ToListAsync();
    }

    public async Task<List<StudyGroup>> GetAllAsync()
    {
        return await db.StudyGroups
            .Include(g => g.Members)
            .ThenInclude(m => m.User)
            .ToListAsync();
    }

    public async Task<bool> DeleteGroupAsync(int id)
    {
        await db.StudyGroups
            .Where(x => x.StudyGroupId == id)
            .ExecuteDeleteAsync();

        return true;
    }

    private async Task<bool> AreUsersInSubjectGroupAsync(IEnumerable<int> userIds, Subject subject)
    {
        return await db.StudyGroupMembers
            .Where(m => userIds.Contains(m.UserId))
            .Join(db.StudyGroups, m => m.StudyGroupId, g => g.StudyGroupId, (m, g) => new { m.UserId, g.Subject })
            .AnyAsync(x => x.Subject == subject);
    }
}