namespace TBMWatchdog.Models;

public enum VerifiedFeedbackLevel { None, Candidate, Verified, Authoritative, Conflict }

public sealed record VerifiedFeedbackMatch(decimal? CandidateValue, double Similarity, double Confidence,
    int SupportingExamples, int ConflictCount, VerifiedFeedbackLevel Level, string Reason,
    string? MatchedFeedbackId = null)
{
    public static VerifiedFeedbackMatch None(string reason) => new(null, 0, 0, 0, 0, VerifiedFeedbackLevel.None, reason);
}

public sealed record FeedbackLearningReplaySummary(int Total, int BaselineCorrect, int AssistedCorrect,
    int FixedCount, int BrokenCount, bool AuthoritativeRecommended);

public sealed record FeedbackCalibrationOutcome(int Total, int BaselineCorrect, int AssistedCorrect,
    int Recovered, int Broken, int FalseAuthoritative, double SelectedThreshold, int SelectedSupport,
    double Accuracy, double Precision, bool LiveAuthoritativeAllowed);
