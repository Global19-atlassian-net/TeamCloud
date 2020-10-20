/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using FluentValidation;
using TeamCloud.Model.Data;

namespace TeamCloud.Model.Validation.Data
{
    public sealed class ComponentOfferValidator : AbstractValidator<ComponentOffer>
    {
        public ComponentOfferValidator()
        {
            RuleFor(obj => obj.ProviderId)
                .MustBeProviderId();

            RuleFor(obj => obj.Id)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .Must((obj, id) => id.StartsWith($"{obj.ProviderId}.", StringComparison.Ordinal))
                .WithMessage(obj => "'{PropertyName}' must begin with the providerId followed by a period " + $"({obj.ProviderId}.)");

            RuleFor(obj => obj.InputJsonSchema)
                // .Cascade(CascadeMode.Stop)
                .NotEmpty();
            // .Must(schema => JsonSchema.Parse(schema))

        }
    }
}
