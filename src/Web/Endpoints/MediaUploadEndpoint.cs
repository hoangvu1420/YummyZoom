using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YummyZoom.Application.Common.Caching;
using YummyZoom.Application.Common.Idempotency;
using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Models.Media;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Web.Endpoints;

public static class MediaUploadEndpoint
{
    private static readonly CachePolicy IdempotencyPolicy = CachePolicy.WithTtl(TimeSpan.FromHours(24), "media-upload");

    public static async Task<IResult> HandleAsync(
        HttpContext httpContext,
        IFormFile file,
        Guid? restaurantId,
        Guid? menuItemId,
        string? scope,
        Guid? correlationId,
        IAuthorizationService authorizationService,
        ICacheService cache,
        IMediaStorageService mediaStorage,
        IHostEnvironment environment,
        IUser currentUser,
        ILogger logger,
        CancellationToken ct)
    {
        // Require Idempotency-Key header
        var idemKeyValue = httpContext.Request.Headers["Idempotency-Key"].FirstOrDefault();
        var idemKey = IdempotencyKey.Create(idemKeyValue);
        if (idemKey.IsFailure)
        {
            return Results.BadRequest(new ProblemDetails
            {
                Title = idemKey.Error.Code,
                Detail = idemKey.Error.Description,
                Status = StatusCodes.Status400BadRequest
            });
        }

        var normalizedScope = NormalizeScope(scope);
        var isRegistration = normalizedScope == "restaurant-registration";
        var isMenuItemScope = normalizedScope == "menu-item";

        if (isRegistration)
        {
            // Require authentication for registration uploads, but not restaurant-scoped permission
            if (httpContext.User?.Identity?.IsAuthenticated != true)
            {
                return Results.Unauthorized();
            }

            if (correlationId is null || correlationId == Guid.Empty)
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Media.CorrelationRequired",
                    Detail = "correlationId is required for registration uploads.",
                    Status = StatusCodes.Status400BadRequest
                });
            }
        }
        else
        {
            if (restaurantId is null || restaurantId == Guid.Empty)
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Media.RestaurantIdRequired",
                    Detail = "restaurantId is required for this upload scope.",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            // Enforce restaurant-staff authorization with restaurant context as resource
            var resource = new RestaurantResource(RestaurantId.Create(restaurantId.Value));
            var authResult = await authorizationService.AuthorizeAsync(httpContext.User, resource, Policies.MustBeRestaurantStaff);
            if (!authResult.Succeeded)
            {
                return Results.Forbid();
            }

            if (isMenuItemScope && menuItemId is null)
            {
                if (correlationId is null || correlationId == Guid.Empty)
                {
                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "Media.CorrelationRequired",
                        Detail = "correlationId is required for menu-item uploads before the menu item exists.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }
            }
        }

        // Basic file validations
        if (file is null || file.Length == 0)
        {
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Media.FileMissing",
                Detail = "Upload file is required.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        if (!MediaValidation.IsAllowedContentType(file.ContentType))
        {
            return Results.Problem(new ProblemDetails
            {
                Title = "Media.UnsupportedType",
                Detail = $"Content type '{file.ContentType}' is not allowed.",
                Status = StatusCodes.Status415UnsupportedMediaType
            });
        }

        if (!MediaValidation.IsWithinSizeLimit(file.Length))
        {
            return Results.Problem(new ProblemDetails
            {
                Title = "Media.FileTooLarge",
                Detail = $"File exceeds maximum size of {MediaValidation.DefaultMaxBytes / (1024 * 1024)} MB.",
                Status = StatusCodes.Status413PayloadTooLarge
            });
        }

        var (folder, publicIdHint, overwrite) = BuildFolderAndPublicId(environment, restaurantId, menuItemId, normalizedScope, correlationId);

        // Idempotency cache lookup
        var cacheKey = BuildCacheKey(idemKey.Value, restaurantId, menuItemId, normalizedScope, currentUser.Id, correlationId);
        var cached = await cache.GetAsync<MediaUploadResult>(cacheKey, ct);
        if (cached is not null)
        {
            return Results.Ok(cached);
        }

        await using var stream = file.OpenReadStream();

        var uploadRequest = new MediaUploadRequest
        {
            Content = stream,
            FileName = file.FileName,
            ContentType = file.ContentType,
            Folder = folder,
            PublicIdHint = publicIdHint,
            Overwrite = overwrite,
            Tags = BuildTags(restaurantId, menuItemId, normalizedScope, correlationId),
            Scope = normalizedScope,
            IdempotencyKey = idemKey.Value.Value,
            Length = file.Length,
            RestaurantId = restaurantId,
            MenuItemId = menuItemId,
            CorrelationId = correlationId
        };

        var uploadResult = await mediaStorage.UploadAsync(uploadRequest, ct);
        if (uploadResult.IsFailure)
        {
            logger.LogWarning("Media upload failed for scope {Scope} restaurant {RestaurantId} correlation {CorrelationId}: {Code} - {Message}", normalizedScope, restaurantId, correlationId, uploadResult.Error.Code, uploadResult.Error.Description);
            return uploadResult.ToIResult();
        }

        await cache.SetAsync(cacheKey, uploadResult.Value, IdempotencyPolicy, ct);

        return Results.Ok(uploadResult.Value);
    }

    private static string NormalizeScope(string? scope)
    {
        return string.IsNullOrWhiteSpace(scope)
            ? "menu-item"
            : scope.Trim().ToLowerInvariant();
    }

    private static (string Folder, string? PublicIdHint, bool Overwrite) BuildFolderAndPublicId(
        IHostEnvironment environment,
        Guid? restaurantId,
        Guid? menuItemId,
        string scope,
        Guid? correlationId)
    {
        var env = (environment.EnvironmentName ?? "local").ToLowerInvariant();
        if (scope == "restaurant-registration")
        {
            var corr = correlationId?.ToString() ?? "registration";
            return ($"{env}/registrations/{corr}", corr, true);
        }

        if (scope == "menu-item" && !menuItemId.HasValue && correlationId.HasValue)
        {
            var corr = correlationId.Value.ToString();
            var baseFolderPending = $"{env}/restaurants/{restaurantId}/items/pending";
            return ($"{baseFolderPending}/{corr}", corr, true);
        }

        var baseFolder = $"{env}/restaurants/{restaurantId}";

        return scope switch
        {
            "restaurant-logo" => ($"{baseFolder}/branding", "logo", true),
            "restaurant-background" => ($"{baseFolder}/branding", "background", true),
            _ when menuItemId.HasValue => ($"{baseFolder}/items/{menuItemId}", menuItemId.Value.ToString(), true),
            _ => ($"{baseFolder}/misc", null, false)
        };
    }

    private static IReadOnlyDictionary<string, string> BuildTags(Guid? restaurantId, Guid? menuItemId, string scope, Guid? correlationId = null)
    {
        var tags = new Dictionary<string, string>
        {
            ["scope"] = scope
        };

        if (restaurantId.HasValue && restaurantId.Value != Guid.Empty)
        {
            tags["restaurantId"] = restaurantId.Value.ToString();
        }

        if (menuItemId.HasValue)
        {
            tags["menuItemId"] = menuItemId.Value.ToString();
        }

        if (correlationId.HasValue)
        {
            tags["correlationId"] = correlationId.Value.ToString();
        }

        return tags;
    }

    private static string BuildCacheKey(IdempotencyKey key, Guid? restaurantId, Guid? menuItemId, string scope, string? userId, Guid? correlationId)
    {
        var userPart = string.IsNullOrWhiteSpace(userId) ? "anonymous" : userId;
        var menuPart = menuItemId?.ToString() ?? "-";
        var restaurantPart = restaurantId?.ToString() ?? "pending";
        var corrPart = correlationId?.ToString() ?? "-";
        return $"idem:media:{restaurantPart}:{menuPart}:{scope}:{corrPart}:{userPart}:{key.Value}";
    }

    private sealed record RestaurantResource(RestaurantId RestaurantId) : IContextualCommand
    {
        public string ResourceType => "Restaurant";
        public string ResourceId => RestaurantId.Value.ToString();
    }
}
