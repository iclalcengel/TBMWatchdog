using TBMWatchdog.Models;

namespace TBMWatchdog.Services;

public sealed record NumericCharacterOcrOptions(
    bool Enabled, string TemplateFolder, double MinimumScore,
    double AuthoritativeScore, double MinimumMargin, double MaximumAllowedInkLoss,
    int MinimumRecommendedTemplates, bool EnableGeometry);

public static class NumericFieldCharacterOcrService
{
    public static bool Supports(OcrFieldKind field) =>
        field is OcrFieldKind.Nominal or OcrFieldKind.Gate or OcrFieldKind.Scale1 or OcrFieldKind.Scale2 or OcrFieldKind.Advance
            or OcrFieldKind.StableRpm or OcrFieldKind.StableTorque or OcrFieldKind.StableThrustForce
            or OcrFieldKind.StableAdvanceSpeed or OcrFieldKind.StablePenetration
            or OcrFieldKind.NominalInjection or OcrFieldKind.RealInjection
            or OcrFieldKind.Epb1 or OcrFieldKind.Epb2 or OcrFieldKind.Epb3 or OcrFieldKind.Epb4 or OcrFieldKind.Epb5;
            

    public static NumericCharacterOcrOptions GetOptions(OcrProfile profile, OcrFieldKind field)
    {
        var folderName = field switch
        {
            OcrFieldKind.Nominal => "NominalDigits",
            OcrFieldKind.Gate => "GateDigits",
            OcrFieldKind.Scale1 => "Scale1Digits",
            OcrFieldKind.Scale2 => "Scale2Digits",
            OcrFieldKind.Advance => "AdvanceMmDigits",
            OcrFieldKind.StableRpm or OcrFieldKind.StableTorque or OcrFieldKind.StableThrustForce
                or OcrFieldKind.StableAdvanceSpeed or OcrFieldKind.StablePenetration
                or OcrFieldKind.NominalInjection or OcrFieldKind.RealInjection
                or OcrFieldKind.Epb1 or OcrFieldKind.Epb2 or OcrFieldKind.Epb3 or OcrFieldKind.Epb4 or OcrFieldKind.Epb5 => "SharedNumericDigits",
            _ => throw new ArgumentOutOfRangeException(nameof(field), field, "Alan karakter OCR desteklemiyor.")
        };
        var configured = field switch
        {
            OcrFieldKind.Nominal => "Templates/NominalDigits",
            OcrFieldKind.Gate => profile.GateDigitTemplateFolder,
            OcrFieldKind.Scale1 => profile.Scale1DigitTemplateFolder,
            OcrFieldKind.Scale2 => profile.Scale2DigitTemplateFolder,
            OcrFieldKind.Advance => "Templates/AdvanceMmDigits",
            OcrFieldKind.StableRpm or OcrFieldKind.StableTorque or OcrFieldKind.StableThrustForce
                or OcrFieldKind.StableAdvanceSpeed or OcrFieldKind.StablePenetration
                or OcrFieldKind.NominalInjection or OcrFieldKind.RealInjection
                or OcrFieldKind.Epb1 or OcrFieldKind.Epb2 or OcrFieldKind.Epb3 or OcrFieldKind.Epb4 or OcrFieldKind.Epb5 => "Templates/SharedNumericDigits",
            _ => "Templates"
        };
        var root = Path.IsPathRooted(configured) ? configured : Path.Combine(AppContext.BaseDirectory, configured);
        if (string.Equals(Path.GetFileName(root), folderName, StringComparison.OrdinalIgnoreCase))
            root = Directory.GetParent(root)?.FullName ?? root;
        var machineFolder = Path.Combine(root, GateTemplatePaths.Safe(profile.Name), folderName);
        return field switch
        {
            OcrFieldKind.Nominal => new(true, machineFolder,
                profile.GateTemplateMinimumScore, profile.GateTemplateAuthoritativeScore,
                profile.GateTemplateMinimumMargin, profile.GateMaximumAllowedInkLoss,
                profile.GateMinimumRecommendedTemplatesPerDigit, true),
            OcrFieldKind.Gate => new(profile.EnableGateDigitTemplateMatcher, machineFolder,
                profile.GateTemplateMinimumScore, profile.GateTemplateAuthoritativeScore,
                profile.GateTemplateMinimumMargin, profile.GateMaximumAllowedInkLoss,
                profile.GateMinimumRecommendedTemplatesPerDigit, profile.EnableGateCharacterGeometry),
            OcrFieldKind.Scale1 => new(profile.EnableScaleCharacterOcr, machineFolder,
                profile.Scale1TemplateMinimumScore, profile.Scale1TemplateAuthoritativeScore,
                profile.Scale1TemplateMinimumMargin, profile.ScaleMaximumAllowedInkLoss,
                profile.ScaleMinimumRecommendedTemplatesPerDigit, true),
            OcrFieldKind.Scale2 => new(profile.EnableScaleCharacterOcr, machineFolder,
                profile.Scale2TemplateMinimumScore, profile.Scale2TemplateAuthoritativeScore,
                profile.Scale2TemplateMinimumMargin, profile.ScaleMaximumAllowedInkLoss,
                profile.ScaleMinimumRecommendedTemplatesPerDigit, true),
            OcrFieldKind.Advance => new(true, machineFolder,
                profile.GateTemplateMinimumScore, profile.GateTemplateAuthoritativeScore,
                profile.GateTemplateMinimumMargin, profile.GateMaximumAllowedInkLoss,
                profile.GateMinimumRecommendedTemplatesPerDigit, true),
            OcrFieldKind.StableRpm or OcrFieldKind.StableTorque or OcrFieldKind.StableThrustForce
                or OcrFieldKind.StableAdvanceSpeed or OcrFieldKind.StablePenetration
                or OcrFieldKind.NominalInjection or OcrFieldKind.RealInjection
                or OcrFieldKind.Epb1 or OcrFieldKind.Epb2 or OcrFieldKind.Epb3 or OcrFieldKind.Epb4 or OcrFieldKind.Epb5 => new(true, machineFolder,
                profile.CharacterTemplateMinimumScore, profile.CharacterTemplateAuthoritativeScore,
                profile.CharacterTemplateMinimumMargin, profile.CharacterMaximumAllowedInkLoss,
                profile.GateMinimumRecommendedTemplatesPerDigit, true),
            _ => throw new ArgumentOutOfRangeException(nameof(field))
        };
    }
}
