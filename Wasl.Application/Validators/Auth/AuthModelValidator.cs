using FluentValidation;
using Wasl.Application.Dtos.AuthModel;
using System;
using System.Collections.Generic;
using System.Text;
using Wasl.Application.Resources;
using Microsoft.Extensions.Localization;

namespace Wasl.Application.Validators.Auth
{
    public class AuthModelValidator : AbstractValidator<AuthModel>
    {
        private readonly IStringLocalizer<SharedResource> _localizer;
        public AuthModelValidator(IStringLocalizer<SharedResource> localizer)
        {
            _localizer = localizer;
            RuleFor(x => x.UserName).
                NotEmpty().WithMessage("User Name Is Required")
                .Length(4, 10).WithMessage("The User Name Length Must Be Between 3 And 10");

            RuleFor(x => x.Email).
                NotEmpty().WithMessage("Email Is Required")
                .EmailAddress().WithMessage("Invalid Email Format");

        }
    }
}
