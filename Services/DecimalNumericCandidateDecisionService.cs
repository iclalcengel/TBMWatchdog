using TBMWatchdog.Models;

namespace TBMWatchdog.Services;

internal sealed record DecimalCandidateDecision(
    OcrReadResult Result,
    OcrCandidateResult SelectedCandidate,
    decimal FullLineConsensusValue,
    int FullLineConsensusCount,
    int ZeroCandidateCount,
    decimal? CharacterValue,
    bool CharacterAuthoritative,
    string CharacterAuthorityReason);

internal static class DecimalNumericCandidateDecisionService
{
    public static bool TrySelect(IReadOnlyList<OcrCandidateResult> candidates, OcrFieldKind field,
        out DecimalCandidateDecision? decision)
    {
        decision = null;
        var decimalPlaces = OcrService.ExpectedDecimalPlaces(field);
        if (decimalPlaces <= 0) return false;

        if (field == OcrFieldKind.StableTorque)
        {
            var leadingOneCharacterEvidence = candidates.Any(candidate =>
                candidate.UsesCharacterBasedOcr
                && candidate.CleanedText == "1"
                && candidate.CharacterCount == 1);
            foreach (var candidate in candidates)
            {
                TryRemoveTorqueDecimalArtifact(candidate, decimalPlaces);
                TryCorrectTorqueLeadingOne(candidate, decimalPlaces, leadingOneCharacterEvidence);
                TryCorrectTorqueGlyphConfusions(candidate, decimalPlaces);
            }
        }

        var fullLine = candidates
            .Where(candidate => !candidate.UsesCharacterBasedOcr && candidate.IsValid
                && OcrService.HasExpectedDecimalStructure(candidate.CleanedText, decimalPlaces))
            .ToArray();
        if (fullLine.Length == 0) return false;

        var groups = fullLine.GroupBy(candidate => candidate.ParsedValue!.Value)
            .Select(group => new
            {
                Value = group.Key,
                Candidates = group.ToArray(),
                IndependentPipelines = group.Select(candidate => BasePipeline(candidate.ProcessingMethod)).Distinct().Count(),
                ColourPreservingAgreement = field == OcrFieldKind.StableTorque
                    && group.Count(candidate => candidate.ProcessingMethod.StartsWith("Torque colour-preserving fallback", StringComparison.Ordinal)) >= 2,
                GeometryCorrectedAgreement = field == OcrFieldKind.StableTorque
                    && group.Any(candidate => candidate.CorrectedFullLineResult.HasValue
                        && candidate.ExtraCharacterEliminationReason.StartsWith("Tork renk maskesi", StringComparison.Ordinal)),
                AverageConfidence = group.Average(candidate => candidate.OcrConfidence)
            })
            .OrderByDescending(group => group.GeometryCorrectedAgreement)
            .ThenByDescending(group => group.ColourPreservingAgreement)
            .ThenByDescending(group => group.IndependentPipelines)
            .ThenByDescending(group => group.AverageConfidence)
            .ToArray();
        var consensus = groups[0];
        var character = candidates.Where(candidate => candidate.UsesCharacterBasedOcr && candidate.IsValid)
            .OrderByDescending(candidate => candidate.AllCharactersTemplateAuthoritative)
            .ThenByDescending(candidate => candidate.CharacterDecisionReliable)
            .ThenByDescending(candidate => candidate.OcrConfidence)
            .FirstOrDefault();
        var characterStructureValid = character is not null
            && OcrService.HasExpectedDecimalStructure(character.CleanedText, decimalPlaces);
        var characterAuthoritative = character is not null
            && character.SegmentMatch == true
            && character.CharacterDecisionReliable
            && character.AllCharactersTemplateAuthoritative
            && characterStructureValid
            && character.CharacterDiagnostics.Count > 0
            && character.CharacterDiagnostics.Where(item => item.FinalDigit != ".").All(item =>
                item.IsTemplateAuthoritative && item.TemplateContributed && item.TemplateResult is not null);
        var authorityReason = character is null ? "CharacterUnavailable"
            : characterAuthoritative ? "VerifiedTemplateSupport"
            : character.CharacterDiagnostics.Count == 0 || character.CharacterDiagnostics.All(item => item.TemplateResult is null)
                ? "NoVerifiedTemplateSupport"
                : !characterStructureValid ? "DecimalStructureInvalid"
                : !character.CharacterDecisionReliable || character.SegmentMatch != true ? "IncompleteCharacterDecision"
                : "TemplateNotAuthoritative";

        var selectedValue = characterAuthoritative ? character!.ParsedValue!.Value : consensus.Value;
        var selected = characterAuthoritative
            ? character!
            : consensus.Candidates.OrderByDescending(candidate => candidate.OcrConfidence).First();
        var agrees = character?.ParsedValue == consensus.Value;
        var warning = character is not null && !agrees && !characterAuthoritative
            ? "Character OCR full-line result ile uyuşmadı; doğrulanmış template desteği yok."
            : consensus.Candidates.Select(candidate => candidate.CharacterValidationWarning)
                .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
        var source = characterAuthoritative && character!.ParsedValue != consensus.Value
            ? "AuthoritativeCharacterTemplate"
            : consensus.GeometryCorrectedAgreement ? "TorqueGlyphGeometryCorrection"
            : consensus.ColourPreservingAgreement ? "TorqueColourPreservingConsensus"
            : consensus.IndependentPipelines > 1 ? "FullLineConsensus" : "StructurallyValidFullLine";
        var reason = source == "AuthoritativeCharacterTemplate"
            ? $"Otoriter kullanıcı şablonu ve eksiksiz segmentasyon sonucu seçildi: {selectedValue}."
            : source == "TorqueGlyphGeometryCorrection"
                ? $"Tork renk maskesindeki rakam geometrisiyle doğrulandı: {selectedValue}."
            : source == "TorqueColourPreservingConsensus"
                ? $"Tork renk korumalı OCR uzlaşısı ince baştaki rakamı korudu: {consensus.Value}."
            : consensus.IndependentPipelines > 1
                ? $"Full-line decimal consensus: {consensus.Value} ({consensus.IndependentPipelines} bağımsız pipeline)."
                : $"Yapısal olarak geçerli full-line decimal aday seçildi: {consensus.Value}.";
        if (selected.ExtraCharacterDetected && selected.CorrectedFullLineResult.HasValue)
            reason = $"Ondalık ayıracı kaynaklı fazladan rakam elendi; {selected.CleanedText} → {selected.CorrectedFullLineResult.Value}. "
                + selected.ExtraCharacterEliminationReason;
        if (warning is not null) reason += " " + warning;

        var zeroCount = candidates.Count(candidate => !candidate.UsesCharacterBasedOcr
            && candidate.ParsedValue == 0m && candidate.Error is null);
        var confidence = characterAuthoritative
            ? character!.SystemConfidence
            : (float)Math.Clamp(consensus.AverageConfidence + (consensus.IndependentPipelines > 1 ? .05 : 0), 0, 1);
        decision = new DecimalCandidateDecision(
            new OcrReadResult(selectedValue, selected.RawText, selected.CleanedText, confidence, null)
            {
                Warning = warning,
                SelectionReason = reason,
                FullLineConsensusValue = consensus.Value,
                FullLineConsensusCount = consensus.IndependentPipelines,
                FullLineConsensusLevel = consensus.IndependentPipelines >= 2 ? "StrongConsensus" : "SingleCandidate",
                CharacterResult = character?.ParsedValue,
                CharacterStatus = characterAuthoritative ? "Authoritative" : authorityReason,
                CharacterAuthoritative = characterAuthoritative,
                CharacterAuthorityReason = authorityReason,
                ZeroCandidateCount = zeroCount,
                DecisionSource = source
            }, selected, consensus.Value, consensus.IndependentPipelines, zeroCount,
            character?.ParsedValue, characterAuthoritative, authorityReason);
        return true;
    }

    internal static bool TryRemoveTorqueDecimalArtifact(OcrCandidateResult candidate, int decimalPlaces)
    {
        if (candidate.UsesCharacterBasedOcr || !candidate.ParsedValue.HasValue
            || candidate.CleanedText.Length < decimalPlaces + 3)
            return false;
        var normalized = candidate.CleanedText.Replace(',', '.');
        var separator = normalized.LastIndexOf('.');
        if (separator <= 1 || normalized[0] != '1'
            || normalized.Count(char.IsDigit) != decimalPlaces + 2)
            return false;

        // 1.207 saha örneğinde noktanın kenar izi ayrıca '4' okunarak 14.207
        // üretiliyor. Segmentasyon dört gerçek rakam görürken full-line beş rakam
        // görüyorsa, ondalık ayıracından hemen önce eklenen ikinci rakam sahtedir.
        var segmentedDigits = candidate.SegmentCount > 0 ? candidate.SegmentCount - 1 : 0;
        if (candidate.SegmentMatch != false || segmentedDigits <= 0
            || candidate.CharacterCount != segmentedDigits + 1)
            return false;

        var correctedText = normalized.Remove(1, 1);
        if (!decimal.TryParse(correctedText, System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture, out var corrected)
            || corrected < 0 || corrected > 20)
            return false;

        candidate.ExtraCharacterDetected = true;
        candidate.RemovedNoiseCharacter = normalized[1].ToString();
        candidate.ExtraCharacterEliminationReason =
            $"Ondalık ayıracı çevresindeki iz fazladan '{normalized[1]}' olarak okundu; "
            + $"OCR rakam sayısı {candidate.CharacterCount}, segmentasyon rakam sayısı {segmentedDigits}.";
        candidate.CorrectedFullLineResult = corrected;
        candidate.ParsedValue = corrected;
        return true;
    }

    internal static bool TryCorrectTorqueLeadingOne(OcrCandidateResult candidate, int decimalPlaces,
        bool leadingOneCharacterEvidence = false)
    {
        if (candidate.UsesCharacterBasedOcr || !candidate.ParsedValue.HasValue
            || (!candidate.LeadingNarrowDigitGeometry && !leadingOneCharacterEvidence)
            || !IsTorqueColourPreservingCandidate(candidate.ProcessingMethod))
            return false;
        var normalized = candidate.CleanedText.Replace(',', '.');
        if (!normalized.StartsWith("4.", StringComparison.Ordinal)
            || normalized.Length != decimalPlaces + 2)
            return false;
        var correctedText = "1" + normalized[1..];
        if (!decimal.TryParse(correctedText, System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture, out var corrected)) return false;
        candidate.ExtraCharacterDetected = true;
        candidate.RemovedNoiseCharacter = "4→1";
        candidate.ExtraCharacterEliminationReason =
            "İlk glif dar ve baskın dikey çizgi geometrisinde; Tesseract 4 sonucu 1 olarak düzeltildi.";
        candidate.CorrectedFullLineResult = corrected;
        candidate.ParsedValue = corrected;
        return true;
    }

    internal static bool TryCorrectTorqueGlyphConfusions(OcrCandidateResult candidate, int decimalPlaces)
    {
        if (candidate.UsesCharacterBasedOcr || !candidate.ParsedValue.HasValue
            || !IsTorqueColourPreservingCandidate(candidate.ProcessingMethod)
            || candidate.TorqueGlyphs.Count != decimalPlaces + 1)
            return false;
        var normalized = candidate.CleanedText.Replace(',', '.');
        if (normalized.Length != decimalPlaces + 2 || normalized[1] != '.'
            || normalized.Count(char.IsDigit) != decimalPlaces + 1)
            return false;
        var digits = normalized.Where(char.IsDigit).ToArray();
        var changes = new List<string>();
        for (var i = 0; i < digits.Length; i++)
        {
            var hint = candidate.TorqueGlyphs[i];
            if (hint.Confidence < .90 || hint.SuggestedDigit is null || hint.SuggestedDigit == digits[i]) continue;
            var allowed = (digits[i], hint.SuggestedDigit.Value) is
                ('4', '1') or ('8', '5') or ('9', '5') or ('6', '0');
            if (!allowed) continue;
            changes.Add($"{i + 1}. rakam {digits[i]}→{hint.SuggestedDigit} ({hint.Reason})");
            digits[i] = hint.SuggestedDigit.Value;
        }
        if (changes.Count == 0) return false;
        var correctedText = digits[0] + "." + new string(digits, 1, decimalPlaces);
        if (!decimal.TryParse(correctedText, System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture, out var corrected)
            || corrected < 0 || corrected > 20) return false;
        candidate.ExtraCharacterDetected = true;
        candidate.RemovedNoiseCharacter = string.Join(", ", changes);
        candidate.ExtraCharacterEliminationReason = "Tork renk maskesindeki glif geometrisi OCR rakam karışıklığını düzeltti: " + string.Join("; ", changes);
        candidate.CorrectedFullLineResult = corrected;
        candidate.ParsedValue = corrected;
        return true;
    }

    private static bool IsTorqueColourPreservingCandidate(string processingMethod)
    {
        return processingMethod.StartsWith("Torque tight colour-preserving fallback", StringComparison.Ordinal)
            || processingMethod.StartsWith("Torque colour-preserving fallback", StringComparison.Ordinal);
    }

    private static string BasePipeline(string method)
    {
        if (method.StartsWith("Segment Crop", StringComparison.Ordinal)) return "Segment Crop";
        var slash = method.IndexOf('/');
        return (slash > 0 ? method[..slash] : method)
            .Replace(" + Segmentasyon", "", StringComparison.Ordinal)
            .Trim();
    }
}
