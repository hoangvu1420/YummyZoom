using YummyZoom.Domain.TagEntity.Enums;
using YummyZoom.Domain.TagEntity.Errors;
using YummyZoom.Domain.TagEntity.Events;
using YummyZoom.Domain.TagEntity.ValueObjects;
using YummyZoom.Domain.Common.Models;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.TagEntity;

public sealed class Tag : Entity<TagId>, IAuditableEntity, ISoftDeletableEntity
{
    public string TagName { get; private set; }
    public string? TagDescription { get; private set; }
    public TagCategory TagCategory { get; private set; }

    // Properties from IAuditableEntity
    public DateTimeOffset Created { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public string? LastModifiedBy { get; set; }

    // Properties from ISoftDeletableEntity
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedOn { get; set; }
    public string? DeletedBy { get; set; }

    private Tag(
        TagId tagId,
        string tagName,
        TagCategory tagCategory,
        string? tagDescription)
        : base(tagId)
    {
        TagName = tagName;
        TagCategory = tagCategory;
        TagDescription = tagDescription;
    }

    public static Result<Tag> Create(
        string tagName,
        TagCategory tagCategory,
        string? tagDescription = null)
    {
        // Validate tag name
        if (string.IsNullOrWhiteSpace(tagName))
        {
            return Result.Failure<Tag>(TagErrors.NameIsRequired);
        }

        if (tagName.Length > 100)
        {
            return Result.Failure<Tag>(TagErrors.NameTooLong);
        }

        // Category validation not needed - enum ensures valid values

        var tag = new Tag(
            TagId.CreateUnique(),
            tagName.Trim(),
            tagCategory,
            string.IsNullOrWhiteSpace(tagDescription) ? null : tagDescription.Trim());

        tag.AddDomainEvent(new TagCreated((TagId)tag.Id, tag.TagName, tag.TagCategory.ToStringValue()));

        return Result.Success(tag);
    }

    public Result UpdateDetails(string tagName, string? tagDescription)
    {
        // Validate tag name
        if (string.IsNullOrWhiteSpace(tagName))
        {
            return Result.Failure(TagErrors.NameIsRequired);
        }

        if (tagName.Length > 100)
        {
            return Result.Failure(TagErrors.NameTooLong);
        }

        var oldName = TagName;
        TagName = tagName.Trim();
        TagDescription = string.IsNullOrWhiteSpace(tagDescription) ? null : tagDescription.Trim();

        // Only raise event if the name actually changed
        if (oldName != TagName)
        {
            AddDomainEvent(new TagUpdated((TagId)Id, TagName, TagCategory.ToStringValue()));
        }

        return Result.Success();
    }

    public Result ChangeCategory(TagCategory newTagCategory)
    {
        var oldCategory = TagCategory;
        TagCategory = newTagCategory;

        // Only raise event if the category actually changed
        if (oldCategory != TagCategory)
        {
            AddDomainEvent(new TagCategoryChanged((TagId)Id, oldCategory.ToStringValue(), TagCategory.ToStringValue()));
        }

        return Result.Success();
    }

    public Result MarkAsDeleted(DateTimeOffset deletedOn, string? deletedBy = null)
    {
        if (IsDeleted)
        {
            return Result.Success();
        }

        IsDeleted = true;
        DeletedOn = deletedOn;
        DeletedBy = deletedBy;

        AddDomainEvent(new TagDeleted((TagId)Id));

        return Result.Success();
    }

#pragma warning disable CS8618
    private Tag() { } // Required by EF Core
#pragma warning restore CS8618
}
