using ApiContracts.DTOs;
using Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RepositoryContracts;

namespace WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class CommentsController : ControllerBase
{
    private readonly ICommentRepository _commentRepository;
    private readonly IUserRepository _userRepository;

    public CommentsController(ICommentRepository commentRepository,
        IUserRepository userRepository)
    {
        this._commentRepository = commentRepository;
        this._userRepository = userRepository;
    }

    [HttpPost]
    public async Task<ActionResult<CommentDto>> AddComment(
        [FromBody] CreateCommentDto request)
    {
        Comment comment = new(request.PostId, request.Body, request.UserId);
        User user = await _userRepository.GetSingleAsync(request.UserId);
        Comment created = await _commentRepository.AddAsync(comment);

        CommentDto dto = new(created.Id, created.Body, user.Username,
            created.PostId);
        return Created($"/comments/{dto.Id}", dto);
    }

    [HttpPut]
    public async Task<ActionResult> UpdateComment(
        [FromBody] UpdateCommentDto request)
    {
        Comment comment = await _commentRepository.GetSingleAsync(request.Id);
        comment.Body = request.Body;
        try
        {
            await _commentRepository.UpdateAsync(comment);
            return NoContent();
        }
        catch
        {
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("{commentId}")]
    public async Task<ActionResult> DeleteCommentAsync(
        [FromBody] DeleteCommentDto request)
    {
        try
        {
            Comment? comment =
                await _commentRepository.GetSingleAsync(request.Id);
            await _commentRepository.DeleteAsync(comment.Id);
            return NoContent();
        }
        catch
        {
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet]
    public async Task<IResult> GetCommentsAsync(
        [FromQuery] string? nameContains)
    {
        List<Comment> comments = await _commentRepository.GetManyAsync().ToListAsync();
        List<User> users = await _userRepository
            .GetManyAsync()
            .Where(u => nameContains == null || u.Username.ToLower().Contains(nameContains.ToLower()))
            .ToListAsync();
        
        List<CommentDto> commentDtos = comments.Select(comment =>
        {
            User user = users.First(u => u.Id == comment.UserId);
            return new CommentDto(comment.Id, comment.Body, user.Username, comment.PostId);
        }).ToList();

        return Results.Ok(commentDtos);
    }

    [HttpGet("post/{postId}")]
    public async Task<IResult> GetCommentsFromPostAsync([FromQuery] int postId)
    {
        var comments = await _commentRepository
            .GetManyAsync()
            .Where(c => c.PostId == postId)
            .ToListAsync();
        var userIds = comments.Select(c => c.UserId).Distinct();
        var users = await _userRepository
            .GetManyAsync()
            .Where(u => userIds.Contains(u.Id))
            .ToListAsync();
        var commentDtos = comments.Select(c =>
        {
            var user = users.Single(u => u.Id == c.UserId);
            return new CommentDto(c.Id, c.Body, user.Username, c.PostId);
        }).ToList();

        return Results.Ok(commentDtos);
    }

    [HttpGet("{commentId}")]
    public async Task<ActionResult<CommentDto>> GetSingleComment(int commentId)
    {
        Comment comment = await _commentRepository.GetSingleAsync(commentId);
        User user = await _userRepository.GetSingleAsync(comment.UserId);
        return await Task.FromResult(new CommentDto(comment.Id, comment.Body,
            user.Username, comment.PostId));
    }
}