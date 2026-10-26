using System.Globalization;
using System.Text;
using TBMWatchdog.Models;

namespace TBMWatchdog.Services;

public enum ProcessValueQuality { Reliable, Partial, TemporalSuspicious, StructureSuspicious, Unreadable }

public sealed record ProcessValueReliabilityInput(string MachineId, string FieldId, decimal? ObservedValue,
    string RawText, DateTime CapturedAt, bool CacheUsed, OcrConfidenceStatus OcrQuality);

public sealed record ProcessValueReliabilityResult(decimal? ObservedValue, DateTime ObservedAt,
    ProcessValueQuality ObservedQuality, decimal? TrustedValue, DateTime? TrustedAt,
    ProcessValueQuality TrustedQuality, bool TrustedAccepted, string StructureStatus,
    string TemporalStatus, string Reason);

/// <summary>OCR sonucunu değiştirmeden Tork/İtme için alan-yerel güvenilir değer tutar.</summary>
public sealed class ProcessValueReliabilityService
{
    private sealed class State
    {
        public decimal? Trusted; public DateTime? TrustedAt;
        public decimal? Pending; public int PendingCount; public DateTime? LastEvidenceAt;
    }

    private readonly object _sync = new();
    private readonly OcrProfile _profile;
    private readonly string _csvPath;
    private readonly Dictionary<string, State> _states = new(StringComparer.OrdinalIgnoreCase);

    public ProcessValueReliabilityService(OcrProfile profile, string? csvPath = null)
    {
        _profile = profile;
        _csvPath = csvPath ?? Path.Combine(AppContext.BaseDirectory, "Diagnostics", "ProcessValueReliability.csv");
    }

    public ProcessValueReliabilityResult Evaluate(ProcessValueReliabilityInput input)
    {
        lock (_sync)
        {
            var key = input.MachineId + "|" + input.FieldId;
            if (!_states.TryGetValue(key, out var state)) _states[key] = state = new State();
            if (!input.ObservedValue.HasValue)
                return Write(input, state, ProcessValueQuality.Unreadable, false, "Unreadable", "NotEvaluated", "Sayısal OCR sonucu yok.");

            var structure = Structure(input);
            var structureOk = structure == "Plausible";
            if (!state.Trusted.HasValue)
            {
                if (structureOk && !input.CacheUsed)
                {
                    var previous = state.Trusted; var previousAt = state.TrustedAt;
                    state.Trusted = input.ObservedValue; state.TrustedAt = input.CapturedAt;
                    return Write(input, state, Quality(input.OcrQuality), true, structure, "InitialAccepted", "İlk yapısal olarak geçerli gerçek okuma güvenilir değer oldu.", previous, previousAt);
                }
                return Write(input, state, ProcessValueQuality.StructureSuspicious, false, structure, "Held", "İlk okuma yapısal doğrulama bekliyor.");
            }

            var delta = input.ObservedValue.Value - state.Trusted.Value;
            var elapsed = Math.Max(0, (input.CapturedAt - state.TrustedAt!.Value).TotalSeconds);
            var relative = Math.Abs(delta) / Math.Max(1m, Math.Abs(state.Trusted.Value));
            var torque = input.FieldId.Equals(BuiltInFieldIds.StableTorque, StringComparison.OrdinalIgnoreCase);
            var maxRelative = torque ? _profile.TorqueMaximumRelativeChangePerReading : _profile.ThrustMaximumRelativeChangePerReading;
            var baseAbsolute = torque ? _profile.TorqueMaximumAbsoluteChangePerReading : _profile.ThrustMaximumAbsoluteChangePerReading;
            var required = Math.Max(2, torque ? _profile.TorqueRecoveryConfirmationCount : _profile.ThrustRecoveryConfirmationCount);
            var timeFactor = Math.Clamp((decimal)Math.Max(1d, elapsed / 30d), 1m, 3m);
            // Elapsed time may relax an ordinary process change, but it must not legitimize a
            // missing-leading-digit reading such as 15590 -> 1131 (or the inverse). Such a
            // magnitude-band transition always goes through independent recovery confirmation.
            var thrustExtremeMagnitudeChange = !torque && state.Trusted.Value > 0 && input.ObservedValue.Value > 0
                && (input.ObservedValue.Value / state.Trusted.Value < 0.35m
                    || input.ObservedValue.Value / state.Trusted.Value > (1m / 0.35m));
            var temporalOk = !thrustExtremeMagnitudeChange
                && (Math.Abs(delta) <= baseAbsolute * timeFactor || relative <= maxRelative);
            if (structureOk && temporalOk && !input.CacheUsed)
            {
                var previous = state.Trusted; var previousAt = state.TrustedAt;
                state.Trusted = input.ObservedValue; state.TrustedAt = input.CapturedAt;
                state.Pending = null; state.PendingCount = 0; state.LastEvidenceAt = input.CapturedAt;
                return Write(input, state, Quality(input.OcrQuality), true, structure, "Plausible", "Alan-yerel yapısal ve zamansal kontrol geçti.", previous, previousAt);
            }

            var independent = !input.CacheUsed && state.LastEvidenceAt != input.CapturedAt;
            var tolerance = torque ? 0.25m : Math.Max(100m, Math.Abs(input.ObservedValue.Value) * 0.03m);
            if (structureOk && independent)
            {
                if (state.Pending.HasValue && Math.Abs(state.Pending.Value - input.ObservedValue.Value) <= tolerance) state.PendingCount++;
                else { state.Pending = input.ObservedValue; state.PendingCount = 1; }
                state.LastEvidenceAt = input.CapturedAt;
                if (state.PendingCount >= required)
                {
                    var previous = state.Trusted; var previousAt = state.TrustedAt;
                    state.Trusted = input.ObservedValue; state.TrustedAt = input.CapturedAt;
                    state.Pending = null; state.PendingCount = 0;
                    return Write(input, state, Quality(input.OcrQuality), true, structure, "RecoveryConfirmed", "Bağımsız benzer okumalarla yeni çalışma seviyesi doğrulandı.", previous, previousAt);
                }
            }
            var quality = structureOk ? ProcessValueQuality.TemporalSuspicious : ProcessValueQuality.StructureSuspicious;
            var reason = !structureOk ? "Ondalık/hane yapısı şüpheli; observed korundu, trusted değiştirilmedi."
                : input.CacheUsed ? "Cache sonucu bağımsız doğrulama sayılmadı."
                : !independent ? "Aynı CapturedAt bağımsız doğrulama sayılmadı."
                : thrustExtremeMagnitudeChange
                    ? $"Aşırı büyüklük/hane seviyesi değişimi doğrulama bekliyor ({state.PendingCount}/{required})."
                    : $"Ani değişim doğrulama bekliyor ({state.PendingCount}/{required}).";
            return Write(input, state, quality, false, structure, temporalOk ? "Plausible" : "Suspicious", reason);
        }
    }

    public void Reset() { lock (_sync) _states.Clear(); }

    private static ProcessValueQuality Quality(OcrConfidenceStatus quality) => quality switch
    { OcrConfidenceStatus.Reliable => ProcessValueQuality.Reliable, OcrConfidenceStatus.Partial => ProcessValueQuality.Partial, _ => ProcessValueQuality.Unreadable };

    private static string Structure(ProcessValueReliabilityInput input)
    {
        var normalized = (input.RawText ?? "").Trim().Replace(',', '.');
        if (input.FieldId.Equals(BuiltInFieldIds.StableTorque, StringComparison.OrdinalIgnoreCase))
        {
            var separator = normalized.IndexOf('.');
            return separator >= 0 && normalized[(separator + 1)..].Count(char.IsDigit) == 3 ? "Plausible" : "DecimalStructureIncomplete";
        }
        var digits = normalized.Count(char.IsDigit);
        return digits is 4 or 5 ? "Plausible" : "DigitStructureSuspicious";
    }

    private ProcessValueReliabilityResult Write(ProcessValueReliabilityInput input, State state,
        ProcessValueQuality observedQuality, bool accepted, string structure, string temporal, string reason,
        decimal? previousTrusted = null, DateTime? previousTrustedAt = null)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_csvPath)!);
        var exists = File.Exists(_csvPath);
        using var writer = new StreamWriter(_csvPath, true, new UTF8Encoding(false));
        if (!exists) writer.WriteLine("Timestamp,Field,ObservedValue,PreviousTrustedValue,Delta,ElapsedSeconds,RelativeChange,StructureStatus,TemporalStatus,TrustedAccepted,NewTrustedValue,Reason");
        var previous = previousTrusted ?? state.Trusted;
        var delta = input.ObservedValue.HasValue && previous.HasValue ? input.ObservedValue - previous : null;
        var referenceAt = previousTrustedAt ?? state.TrustedAt;
        var elapsed = referenceAt.HasValue ? Math.Max(0, (input.CapturedAt - referenceAt.Value).TotalSeconds) : 0;
        var relative = delta.HasValue && previous.HasValue ? Math.Abs(delta.Value) / Math.Max(1m, Math.Abs(previous.Value)) : (decimal?)null;
        writer.WriteLine(string.Join(',', Q(input.CapturedAt.ToString("O")), Q(input.FieldId), N(input.ObservedValue), N(previous), N(delta),
            elapsed.ToString("0.###", CultureInfo.InvariantCulture), N(relative), Q(structure), Q(temporal), accepted, N(state.Trusted), Q(reason)));
        return new(input.ObservedValue, input.CapturedAt, observedQuality, state.Trusted, state.TrustedAt,
            state.Trusted.HasValue ? ProcessValueQuality.Reliable : ProcessValueQuality.Unreadable, accepted, structure, temporal, reason);
    }

    private static string N(decimal? value) => value?.ToString(CultureInfo.InvariantCulture) ?? "";
    private static string Q(string value) => '"' + value.Replace("\"", "\"\"") + '"';
}
