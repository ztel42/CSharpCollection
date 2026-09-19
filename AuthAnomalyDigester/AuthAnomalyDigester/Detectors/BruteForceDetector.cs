namespace AuthAnomalyDigester.Detectors;

/// <summary>Brute-force detection: many failures from same IP or account in a window.</summary>
public static class BruteForceDetector
{
    private static (DateTime First, DateTime Last, int Count)? SlidingMax(
        List<DateTime> timestamps, TimeSpan window, int threshold)
    {
        if (timestamps.Count < threshold) return null;
        var ts = timestamps.OrderBy(t => t).ToList();
        (DateTime First, DateTime Last, int Count)? best = null;
        var left = 0;
        for (var right = 0; right < ts.Count; right++)
        {
            while (ts[right] - ts[left] > window)
                left++;
            var count = right - left + 1;
            if (count >= threshold)
            {
                var cand = (ts[left], ts[right], count);
                if (best is null || cand.Item3 > best.Value.Count)
                    best = cand;
            }
        }
        return best;
    }

    public static List<Finding> Detect(
        IReadOnlyList<AuthEvent> events,
        int threshold = 10,
        int windowMinutes = 10)
    {
        var window = TimeSpan.FromMinutes(windowMinutes);
        var byIp = new Dictionary<string, List<DateTime>>(StringComparer.Ordinal);
        var byUser = new Dictionary<string, List<DateTime>>(StringComparer.Ordinal);
        var syntheticBase = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);

        for (var i = 0; i < events.Count; i++)
        {
            var ev = events[i];
            if (ev.Outcome != "failure") continue;
            var ts = ev.Timestamp ?? syntheticBase.AddMinutes(i);
            if (!string.IsNullOrEmpty(ev.SourceIp))
            {
                if (!byIp.TryGetValue(ev.SourceIp, out var list))
                {
                    list = new List<DateTime>();
                    byIp[ev.SourceIp] = list;
                }
                list.Add(ts);
            }
            if (!string.IsNullOrEmpty(ev.Username))
            {
                if (!byUser.TryGetValue(ev.Username, out var list))
                {
                    list = new List<DateTime>();
                    byUser[ev.Username] = list;
                }
                list.Add(ts);
            }
        }

        var findings = new List<Finding>();
        var seenKeys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var (ip, stamps) in byIp)
        {
            var hit = SlidingMax(stamps, window, threshold);
            if (hit is null) continue;
            var key = $"ip:{ip}";
            if (!seenKeys.Add(key)) continue;
            var (first, last, count) = hit.Value;
            findings.Add(new Finding
            {
                Detector = "brute_force",
                Severity = "high",
                Summary = $"Brute-force pattern from IP {ip}: {count} failures",
                Detail =
                    $"{count} failed auth events from {ip} within " +
                    $"{windowMinutes} minutes " +
                    $"(threshold={threshold}).",
                Count = count,
                SourceIp = ip,
                FirstSeen = first,
                LastSeen = last,
                RelatedEvents = count,
            });
        }

        foreach (var (user, stamps) in byUser)
        {
            var hit = SlidingMax(stamps, window, threshold);
            if (hit is null) continue;
            var key = $"user:{user}";
            if (!seenKeys.Add(key)) continue;
            var (first, last, count) = hit.Value;
            findings.Add(new Finding
            {
                Detector = "brute_force",
                Severity = "high",
                Summary = $"Brute-force pattern against account {user}: {count} failures",
                Detail =
                    $"{count} failed auth events for account '{user}' within " +
                    $"{windowMinutes} minutes " +
                    $"(threshold={threshold}).",
                Count = count,
                Username = user,
                FirstSeen = first,
                LastSeen = last,
                RelatedEvents = count,
            });
        }

        return findings;
    }
}
