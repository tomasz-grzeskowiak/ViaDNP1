using ApiContracts.DTOs;
using Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RepositoryContracts;

namespace WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class PostsController : ControllerBase
{
    private readonly IPostRepository _postRepository;
    private readonly ICommentRepository _commentRepository;
    private readonly IUserRepository _userRepository;

    public PostsController(IPostRepository postRepository,
        ICommentRepository commentRepository, IUserRepository userRepository)
    {
        this._postRepository = postRepository;
        this._commentRepository = commentRepository;
        this._userRepository = userRepository;
    }

    [HttpPost]
    public async Task<ActionResult<PostDto>> AddPost(
        [FromBody] CreatePostDto request)
    {
        var post = new Post(request.Title, request.Body, request.UserId);
        User user = await _userRepository.GetSingleAsync(request.UserId);
        Post created =
            await _postRepository.AddAsync(post ??
                                           throw new Exception(
                                               "Post not found"));

        PostDto dto = new(created.Id, created.Title, created.Body,
            user.Username);
        return Created($"/posts/{dto.Id}", dto);
    }

    [HttpPut]
    public async Task<ActionResult> UpdatePost([FromBody] UpdatePostDto request)
    {
        var post = await _postRepository.GetSingleAsync(request.Id);
        post.Title = request.Title;
        post.Body = request.Body;
        try
        {
            await _postRepository.UpdateAsync(post);
            return NoContent();
        }
        catch
        {
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("{postId}")]
    public async Task<ActionResult> DeletePostAsync(
        [FromBody] DeletePostDto request)
    {
        try
        {
            Post? post = await _postRepository.GetSingleAsync(request.Id);

            IQueryable<Comment> comments = _commentRepository.GetManyAsync();
            List<Comment> postComments =
                comments.Where(c => c.PostId == request.Id).ToList();

            foreach (Comment comment in postComments)
            {
                await _commentRepository.DeleteAsync(comment.Id);
            }

            await _postRepository.DeleteAsync(post.Id);
            return NoContent();
        }
        catch
        {
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet]
    public async Task<ActionResult> GetPostsAsync(
        [FromQuery] string? nameContains)
    {
        var postsList = await _postRepository.GetManyAsync().ToListAsync();
        var usersList = await _userRepository.GetManyAsync().ToListAsync();

        var postDtos = postsList.Select(post =>
        {
            var user = usersList.Single(u => u.Id == post.UserId);
            return new PostDto(post.Id, post.Title, post.Body, user.Username);
        }).ToList();

        return Ok(postDtos);
    }

    [HttpGet("{postId}")]
    public async Task<ActionResult<PostWithCommentsDto>> GetSinglePost(
        int postId)
    {
        var post = await _postRepository.GetSingleAsync(postId);

        var postComments = await _commentRepository
            .GetManyAsync()
            .Where(c => c.PostId == postId)
            .ToListAsync();

        var userIds = postComments.Select(c => c.UserId).Append(post.UserId)
            .Distinct();
        var users = await _userRepository
            .GetManyAsync()
            .Where(u => userIds.Contains(u.Id))
            .ToListAsync();

        var commentDtos = postComments.Select(c =>
        {
            var user = users.Single(u => u.Id == c.UserId);
            return new CommentDto(c.Id, c.Body, user.Username, c.PostId);
        }).ToList();

        var postWriter = users.Single(u => u.Id == post.UserId);
        var dto = new PostWithCommentsDto(
            new PostDto(post.Id, post.Title, post.Body, postWriter.Username),
            commentDtos
        );
        return Ok(dto);
    }
}