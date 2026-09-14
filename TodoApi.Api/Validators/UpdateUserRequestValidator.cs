using FluentValidation;
using TodoApi.Api.Dtos;

namespace TodoApi.Api.Validators;

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Role).IsInEnum();
        RuleFor(x => x.Password).MinimumLength(8).MaximumLength(128).When(x => x.Password is not null);
    }
}
