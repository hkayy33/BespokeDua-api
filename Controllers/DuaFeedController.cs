using BespokeDuaApi.Data;
using BespokeDuaApi.DTO;
using BespokeDuaApi.Models;
using BespokeDuaApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BespokeDuaApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DuaFeedController : ControllerBase
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 50;
    private const int MaxPostsPerDay = 3;
    private static readonly TimeSpan PostLifetime = TimeSpan.FromHours(24);

    private readonly BespokeDuaDbContext _context;
    private readonly ApnsPushService _push;
    private readonly ILogger<DuaFeedController> _logger;

    public DuaFeedController(BespokeDuaDbContext context, ApnsPushService push, ILogger<DuaFeedController> logger)
    {
        _context = context;
        _push = push;
        _logger = logger;
    }

    /// <summary>
    /// Paginated feed of active (non-expired) posts, newest first.
    /// Pass <paramref name="userId"/> so each item includes whether that user has already made dua or owns the post.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<DuaFeedPageDto>> GetFeed(
        [FromQuery] int? userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = DefaultPageSize)
    {
        if (page < 1)
            return BadRequest("page must be at least 1.");

        if (pageSize < 1 || pageSize > MaxPageSize)
            return BadRequest($"pageSize must be between 1 and {MaxPageSize}.");

        if (userId is int uid)
        {
            var userExists = await _context.Users.AnyAsync(u => u.UserId == uid);
            if (!userExists)
                return NotFound("User not found.");
        }

        var now = DateTime.UtcNow;
        var query = _context.DuaFeedPosts
            .AsNoTracking()
            .Where(p => p.ExpiresAt > now);

        var totalCount = await query.CountAsync();

        var posts = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new
            {
                p.PostId,
                p.User.Username,
                p.IsAnonymous,
                p.Content,
                p.CreatedAt,
                p.ExpiresAt,
                DuaCount = p.Likes.Count,
                HasUserMadeDua = userId != null && p.Likes.Any(a => a.UserId == userId),
                IsOwnPost = userId != null && p.UserId == userId
            })
            .ToListAsync();

        var items = posts
            .Select(p => new DuaFeedPostDto
            {
                PostId = p.PostId,
                AuthorUsername = p.IsAnonymous ? null : p.Username,
                IsAnonymous = p.IsAnonymous,
                Content = p.Content,
                CreatedAt = p.CreatedAt,
                ExpiresAt = p.ExpiresAt,
                DuaCount = p.DuaCount,
                HasUserMadeDua = p.HasUserMadeDua,
                IsOwnPost = p.IsOwnPost
            })
            .ToList();

        return Ok(new DuaFeedPageDto
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            HasMore = page * pageSize < totalCount
        });
    }

    /// <summary>Returns a single active post, or 404 if missing or expired.</summary>
    [HttpGet("{postId:guid}")]
    public async Task<ActionResult<DuaFeedPostDto>> GetPost(Guid postId, [FromQuery] int? userId)
    {
        if (userId is int uid)
        {
            var userExists = await _context.Users.AnyAsync(u => u.UserId == uid);
            if (!userExists)
                return NotFound("User not found.");
        }

        var now = DateTime.UtcNow;
        var post = await _context.DuaFeedPosts
            .AsNoTracking()
            .Where(p => p.PostId == postId && p.ExpiresAt > now)
            .Select(p => new DuaFeedPostDto
            {
                PostId = p.PostId,
                AuthorUsername = p.IsAnonymous ? null : p.User.Username,
                IsAnonymous = p.IsAnonymous,
                Content = p.Content,
                CreatedAt = p.CreatedAt,
                ExpiresAt = p.ExpiresAt,
                DuaCount = p.Likes.Count,
                HasUserMadeDua = userId != null && p.Likes.Any(a => a.UserId == userId),
                IsOwnPost = userId != null && p.UserId == userId
            })
            .FirstOrDefaultAsync();

        if (post is null)
            return NotFound();

        return Ok(post);
    }

    /// <summary>
    /// Returns all of the caller's current active (non-expired) posts, newest first.
    /// </summary>
    [HttpGet("user/{userId:int}/active")]
    public async Task<ActionResult<List<DuaFeedPostDto>>> GetActivePostsForUser(int userId)
    {
        var userExists = await _context.Users.AnyAsync(u => u.UserId == userId);
        if (!userExists)
            return NotFound("User not found.");

        var now = DateTime.UtcNow;
        var posts = await _context.DuaFeedPosts
            .AsNoTracking()
            .Where(p => p.UserId == userId && p.ExpiresAt > now)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new DuaFeedPostDto
            {
                PostId = p.PostId,
                AuthorUsername = p.IsAnonymous ? null : p.User.Username,
                IsAnonymous = p.IsAnonymous,
                Content = p.Content,
                CreatedAt = p.CreatedAt,
                ExpiresAt = p.ExpiresAt,
                DuaCount = p.Likes.Count,
                HasUserMadeDua = p.Likes.Any(a => a.UserId == userId),
                IsOwnPost = true
            })
            .ToListAsync();

        return Ok(posts);
    }

    /// <summary>
    /// How many feed posts the user has created in the current UTC day (source of truth for daily limits).
    /// </summary>
    [HttpGet("user/{userId:int}/posting-quota")]
    public async Task<ActionResult<DuaFeedPostingQuotaDto>> GetPostingQuota(int userId)
    {
        var userExists = await _context.Users.AnyAsync(u => u.UserId == userId);
        if (!userExists)
            return NotFound("User not found.");

        var startOfUtcDay = DateTime.UtcNow.Date;
        var usedToday = await _context.DuaFeedPosts
            .CountAsync(p => p.UserId == userId && p.CreatedAt >= startOfUtcDay);

        return Ok(new DuaFeedPostingQuotaDto
        {
            UsedToday = usedToday,
            DailyLimit = MaxPostsPerDay,
            RemainingToday = Math.Max(0, MaxPostsPerDay - usedToday)
        });
    }

    /// <summary>
    /// Share one of the user's saved duas on the feed. Limited to <see cref="MaxPostsPerDay"/> posts per UTC day.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<DuaFeedPostDto>> CreatePost(CreateDuaFeedPostDto dto)
    {
        var userExists = await _context.Users.AnyAsync(u => u.UserId == dto.UserId);
        if (!userExists)
            return NotFound("User not found.");

        var now = DateTime.UtcNow;
        var startOfUtcDay = now.Date;
        var postsToday = await _context.DuaFeedPosts
            .CountAsync(p => p.UserId == dto.UserId && p.CreatedAt >= startOfUtcDay);
        if (postsToday >= MaxPostsPerDay)
            return Conflict($"You can only post {MaxPostsPerDay} times per day.");

        var savedDua = await _context.SavedDuas
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.DuaId == dto.SavedDuaId && d.UserId == dto.UserId);

        Guid? savedDuaId = null;
        Guid? savedSunnahDuaId = null;
        string content;

        if (savedDua is not null)
        {
            savedDuaId = savedDua.DuaId;
            content = savedDua.Dua;
        }
        else
        {
            var savedSunnahDua = await _context.SavedSunnahDuas
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.SunnahDuaId == dto.SavedDuaId && d.UserId == dto.UserId);

            if (savedSunnahDua is null)
                return BadRequest("Saved dua not found for this user.");

            savedSunnahDuaId = savedSunnahDua.SunnahDuaId;
            content = savedSunnahDua.SunnahDua;
        }

        var post = new DuaFeedPost
        {
            PostId = Guid.NewGuid(),
            UserId = dto.UserId,
            SavedDuaId = savedDuaId,
            SavedSunnahDuaId = savedSunnahDuaId,
            Content = content,
            IsAnonymous = dto.IsAnonymous,
            CreatedAt = now,
            ExpiresAt = now.Add(PostLifetime)
        };

        _context.DuaFeedPosts.Add(post);
        await _context.SaveChangesAsync();

        var authorUsername = dto.IsAnonymous
            ? null
            : await _context.Users
                .AsNoTracking()
                .Where(u => u.UserId == dto.UserId)
                .Select(u => u.Username)
                .FirstAsync();

        var response = new DuaFeedPostDto
        {
            PostId = post.PostId,
            AuthorUsername = authorUsername,
            IsAnonymous = post.IsAnonymous,
            Content = post.Content,
            CreatedAt = post.CreatedAt,
            ExpiresAt = post.ExpiresAt,
            DuaCount = 0,
            HasUserMadeDua = false,
            IsOwnPost = true
        };

        return CreatedAtAction(nameof(GetPost), new { postId = post.PostId }, response);
    }

    /// <summary>Remove the caller's post from the feed.</summary>
    [HttpDelete("{postId:guid}")]
    public async Task<IActionResult> DeletePost(Guid postId, [FromQuery] int userId)
    {
        var post = await _context.DuaFeedPosts.FindAsync(postId);
        if (post is null)
            return NotFound();

        if (post.UserId != userId)
            return Forbid();

        _context.DuaFeedPosts.Remove(post);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Toggle "make dua" for the current user on a post. Authors cannot like their own post.
    /// </summary>
    [HttpPost("{postId:guid}/make-dua")]
    public async Task<ActionResult<MakeDuaResultDto>> ToggleMakeDua(Guid postId, MakeDuaDto dto)
    {
        var userExists = await _context.Users.AnyAsync(u => u.UserId == dto.UserId);
        if (!userExists)
            return NotFound("User not found.");

        var now = DateTime.UtcNow;
        var post = await _context.DuaFeedPosts
            .Include(p => p.Likes)
            .FirstOrDefaultAsync(p => p.PostId == postId && p.ExpiresAt > now);

        if (post is null)
            return NotFound("Post not found or expired.");

        if (post.UserId == dto.UserId)
            return BadRequest("You cannot make dua on your own post.");

        var existing = post.Likes.FirstOrDefault(a => a.UserId == dto.UserId);
        var rememberedAuthor = false;
        if (existing is not null)
        {
            _context.DuaFeedLikes.Remove(existing);
        }
        else
        {
            _context.DuaFeedLikes.Add(new DuaFeedLike
            {
                LikeId = Guid.NewGuid(),
                PostId = postId,
                UserId = dto.UserId,
                CreatedAt = now
            });
            rememberedAuthor = true;
        }

        await _context.SaveChangesAsync();

        if (rememberedAuthor)
        {
            try
            {
                await _push.NotifyRememberedInDuaAsync(_context, post.UserId);
            }
            catch (Exception ex)
            {
                // The reaction is already saved. A push failure should not fail the request.
                _logger.LogWarning(ex, "Failed to send dua-feed reaction push for post {PostId}.", postId);
            }
        }

        var duaCount = await _context.DuaFeedLikes.CountAsync(a => a.PostId == postId);
        var hasUserMadeDua = await _context.DuaFeedLikes
            .AnyAsync(a => a.PostId == postId && a.UserId == dto.UserId);

        return Ok(new MakeDuaResultDto
        {
            DuaCount = duaCount,
            HasUserMadeDua = hasUserMadeDua
        });
    }
}
