using BespokeDuaApi.Auth;
using BespokeDuaApi.Data;
using BespokeDuaApi.DTO;
using BespokeDuaApi.Models;
using BespokeDuaApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BespokeDuaApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlanController : ControllerBase
{
    /// <summary>Must match <c>SubscriptionManager.plusMonthlyProductID</c> on iOS.</summary>
    public const string PlusMonthlyProductId = "com.Stylistic.bespokeDua.subscription.monthly";

    private readonly BespokeDuaDbContext _context;
    private readonly AppUserService _appUsers;

    public PlanController(BespokeDuaDbContext context, AppUserService appUsers)
    {
        _context = context;
        _appUsers = appUsers;
    }

    /// <summary>
    /// Links an Apple subscription (original transaction id) to the signed-in user and sets <see cref="PlanType.Subscribed"/>.
    /// </summary>
    [Authorize(AuthenticationSchemes = AppAuthenticationExtensions.CombinedScheme)]
    [HttpPatch("subscribe")]
    public async Task<ActionResult<GetUserDto>> Subscribe([FromBody] SubscribePlanDto dto)
    {
        var user = await _appUsers.GetCurrentUserAsync(User);
        if (user is null)
            return Unauthorized(new { message = "Profile not found." });

        var userId = user.UserId;

        if (string.IsNullOrWhiteSpace(dto.OriginalTransactionId))
            return BadRequest(new { message = "originalTransactionId is required." });

        if (string.IsNullOrWhiteSpace(dto.ProductId) ||
            !string.Equals(dto.ProductId.Trim(), PlusMonthlyProductId, StringComparison.Ordinal))
            return BadRequest(new { message = "Unknown or missing productId." });

        var transactionId = dto.OriginalTransactionId.Trim();

        var ownership = await _context.SubscriptionOwnerships
            .FirstOrDefaultAsync(o => o.OriginalTransactionId == transactionId);

        if (ownership != null && ownership.UserId != userId)
        {
            if (!dto.ConfirmTransfer)
            {
                return Conflict(new
                {
                    message =
                        "This Apple subscription is linked to another account. Confirm transfer to move it here."
                });
            }

            var previousUser = await _context.Users.FirstOrDefaultAsync(u => u.UserId == ownership.UserId);
            if (previousUser != null)
                previousUser.Plan = PlanType.Free;

            ownership.UserId = userId;
            ownership.ProductId = dto.ProductId!.Trim();
        }
        else if (ownership == null)
        {
            _context.SubscriptionOwnerships.Add(new SubscriptionOwnership
            {
                OriginalTransactionId = transactionId,
                ProductId = dto.ProductId!.Trim(),
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            });
        }
        else
        {
            ownership.ProductId = dto.ProductId!.Trim();
        }

        user.Plan = PlanType.Subscribed;
        await _context.SaveChangesAsync();

        return Ok(new GetUserDto
        {
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            Plan = user.Plan.ToString(),
            LastRequestDate = user.LastRequestDate
        });
    }
}
