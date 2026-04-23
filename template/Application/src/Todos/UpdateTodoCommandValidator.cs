namespace TodoSample.Application.Todos;

using global::FluentValidation;

/// <summary>
/// FluentValidation rules for <see cref="UpdateTodoCommand"/>. The Trellis FluentValidation
/// adapter runs this in the Mediator validation stage and short-circuits to an
/// <c>Error.UnprocessableContent</c> (HTTP 422) before the handler ever sees the command.
/// </summary>
public sealed class UpdateTodoCommandValidator : AbstractValidator<UpdateTodoCommand>
{
    public UpdateTodoCommandValidator(TimeProvider timeProvider)
    {
        // Property name override -> JSON Pointer "/dueDate" in the FieldViolation.
        RuleFor(c => c.DueDate)
            .Must(dueDate => dueDate.Value > timeProvider.GetUtcNow().UtcDateTime)
            .WithErrorCode("due-date.in-future")
            .WithMessage("Due date must be in the future.")
            .OverridePropertyName("dueDate");
    }
}
