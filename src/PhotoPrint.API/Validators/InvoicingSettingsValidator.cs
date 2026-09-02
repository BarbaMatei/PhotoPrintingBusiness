using Microsoft.Extensions.Options;
using PhotoPrint.API.Configuration;

namespace PhotoPrint.API.Validators;

/// <summary>
/// Shape-only validator for <see cref="InvoicingSettings"/>. Today the only
/// field is the dual-write rollout flag (<c>CustomerEmailAttachments:Enabled</c>),
/// which is a boolean — no runtime constraint to enforce. The
/// validator exists for the symmetry with the other intent-016 settings and
/// to provide a stable surface when more invoicing toggles are added.
/// </summary>
public sealed class InvoicingSettingsValidator : IValidateOptions<InvoicingSettings>
{
    public ValidateOptionsResult Validate(string? name, InvoicingSettings options)
        => ValidateOptionsResult.Success;
}
