namespace TBMWatchdog.Services;

public static class OcrConfidenceCalculator
{
    public static float Calculate(float ocrConfidence, int matchingMethods, int totalMethods, bool lengthValid,
        bool segmentationSuccessful, double binaryQuality, double noiseRatio, double integrity)
    {
        var agreement = matchingMethods / (double)Math.Max(1, totalMethods);
        var score = Math.Clamp(ocrConfidence, 0, 1) * 0.35
            + agreement * 0.25
            + (lengthValid ? 1 : 0.25) * 0.15
            + (segmentationSuccessful ? 1 : 0.35) * 0.10
            + Math.Clamp(binaryQuality, 0, 1) * 0.08
            + (1 - Math.Clamp(noiseRatio, 0, 1)) * 0.04
            + Math.Clamp(integrity, 0, 1) * 0.03;
        return (float)Math.Clamp(score, 0, 1);
    }
}
