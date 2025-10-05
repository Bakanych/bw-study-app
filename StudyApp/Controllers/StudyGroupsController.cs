using Microsoft.AspNetCore.Mvc;
using StudyApp.Models;
using StudyApp.Services;

namespace StudyApp.Controllers;

public record CreateGroupRequest(string Name, string Subject, IEnumerable<int>? UserIds);

[Route("api/studygroups")]
[ApiController]
public class StudyGroupsController(IStudyGroupService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<StudyGroup>> CreateStudyGroup([FromBody] CreateGroupRequest request)
    {
        if (!Enum.TryParse<Subject>(request.Subject, true, out var subject))
            return BadRequest(new ErrorResponse("Invalid subject"));
        try
        {
            var group = await service.CreateGroupAsync(request.Name, subject, request.UserIds ?? []);
            return CreatedAtAction(nameof(GetById), new { id = group.StudyGroupId }, group);
        }
        catch (Exception ex)
        {
            return BadRequest(new ErrorResponse(ex.Message));
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<StudyGroup>>> SearchStudyGroups([FromQuery] string? subject)
    {
        if (string.IsNullOrEmpty(subject))
        {
            // No subject filter - return all groups
            var allGroups = await service.GetAllAsync();
            return Ok(allGroups);
        }

        // parse subject or throw bad request
        if (!Enum.TryParse<Subject>(subject, true, out var parsedSubject))
            return BadRequest(new ErrorResponse("Invalid subject"));

        var list = await service.SearchAsync(parsedSubject);
        return Ok(list);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<StudyGroup>> GetById(int id)
    {
        var group = await service.GetByIdAsync(id);
        return group is null ? NotFound() : Ok(group);
    }

    [HttpPost("{studyGroupId}/join")]
    public async Task<ActionResult> JoinStudyGroup(int studyGroupId, [FromQuery] int userId)
    {
        try
        {
            var ok = await service.JoinGroupAsync(studyGroupId, userId);
            if (!ok) return Conflict(new ErrorResponse("User already in a group for this subject"));
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(new ErrorResponse(ex.Message));
        }
    }

    [HttpPost("{studyGroupId}/leave")]
    public async Task<ActionResult> LeaveStudyGroup(int studyGroupId, [FromQuery] int userId)
    {
        if (userId <= 0) return BadRequest();
        var ok = await service.LeaveGroupAsync(studyGroupId, userId);
        if (!ok) return BadRequest();
        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteStudyGroup(int id)
    {
        var ok = await service.DeleteGroupAsync(id);
        if (!ok) return BadRequest();
        return NoContent();
    }
}