using FluentValidation;
using TodoApi.Api.Dtos;

namespace TodoApi.Api.Validators;

public class UpdateTodoRequestValidator : AbstractValidator<UpdateTodoRequest>
{
    public UpdateTodoRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}
