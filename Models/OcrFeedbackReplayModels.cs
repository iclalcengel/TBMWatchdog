namespace TBMWatchdog.Models;

public enum FeedbackReplayMode { LiveFast, Diagnostic }
public enum FeedbackFailureClass
{
    None, MissingLeadingDigit, MissingTrailingDigit, MissingMiddleDigit,
    ExtraLeadingDigit, ExtraTrailingDigit, WrongDigit, DecimalShift,
    DecimalDigitCountError, CharacterSegmentationFailure, FullLineFailure,
    TemplateConflict, Unreadable, Unknown
}
public enum FeedbackTemplatePromotionMode { CandidateOnly, UserVerifiedPromotion }

public sealed record OcrFeedbackReplaySample(OcrUserFeedback Feedback, decimal? ExpectedValue,
    bool IsNegativeOnly, string RawImagePath);

public sealed record OcrFeedbackReplayOutcome(decimal? Value, string RawText, string DecisionSource,
    string BestCorrectPipeline, int CorrectPipelineCount, bool FullLineCorrect, bool CharacterCorrect,
    bool TemplateCorrect, int SegmentationCount, int ExpectedDigitCount, string Notes,
    bool TemplateCandidateEligible, string TrainingBlockedReason,
    IReadOnlyList<FeedbackTemplateCandidate> TemplateCandidates);

public sealed record FeedbackTemplateCandidate(int Digit, Rectangle Bounds,
    string Quality, double QualityScore, string Source = "UserFeedback");

public sealed record OcrFeedbackReplayRow(OcrFeedbackReplaySample Sample,
    OcrFeedbackReplayOutcome LiveFast, OcrFeedbackReplayOutcome Diagnostic,
    FeedbackFailureClass FailureClass, bool WrongValueWasTrusted,
    decimal? AbsoluteError, decimal? RelativeError);

public sealed record FeedbackAccuracySummary(string Field, int TotalLabeled, int CorrectSamples,
    int CorrectedSamples, double OriginalExactAccuracy, double ReplayLiveFastExactAccuracy,
    double ReplayDiagnosticExactAccuracy, double UnreadableRate, double MissingDigitRate,
    double WrongDigitRate, double DecimalErrorRate);
