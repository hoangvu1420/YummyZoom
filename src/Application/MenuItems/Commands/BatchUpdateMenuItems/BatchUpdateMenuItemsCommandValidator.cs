using System.Text.Json;
using FluentValidation;

namespace YummyZoom.Application.MenuItems.Commands.BatchUpdateMenuItems;

public sealed class BatchUpdateMenuItemsCommandValidator : AbstractValidator<BatchUpdateMenuItemsCommand>
{
    private static readonly HashSet<string> AllowedFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "isAvailable",
        "price"
    };

    public BatchUpdateMenuItemsCommandValidator()
    {
        RuleFor(v => v.RestaurantId)
            .NotEmpty().WithMessage("Restaurant ID is required.");

        RuleFor(v => v.Operations)
            .NotNull().WithMessage("Operations collection is required.")
            .NotEmpty().WithMessage("At least one operation is required.")
            .Must(ops => ops.Count <= 50).WithMessage("A maximum of 50 operations is allowed per request.");

        RuleForEach(v => v.Operations).ChildRules(op =>
        {
            op.RuleFor(x => x.ItemId)
                .NotEmpty().WithMessage("ItemId is required.");

            op.RuleFor(x => x.Field)
                .NotEmpty().WithMessage("Field is required.")
                .Must(field => AllowedFields.Contains(field.Trim(), StringComparer.OrdinalIgnoreCase))
                .WithMessage("Field must be one of: isAvailable, price.");

            op.RuleFor(x => x.Value)
                .Must((operation, value) => ValueMatchesField(operation.Field, value))
                .WithMessage("Value does not match the required type or constraints for the specified field.");
        });
    }

    private static bool ValueMatchesField(string field, JsonElement value)
    {
        var normalized = Normalize(field);
        return normalized switch
        {
            "isavailable" => value.ValueKind is JsonValueKind.True or JsonValueKind.False,
            "price" => value.ValueKind == JsonValueKind.Number &&
                        value.TryGetDecimal(out _),
            _ => false
        };
    }

    private static string Normalize(string? field) => field?.Trim().ToLowerInvariant() ?? string.Empty;
}
