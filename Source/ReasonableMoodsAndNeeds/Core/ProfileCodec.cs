using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ReasonableMoodsAndNeeds.Core
{
    public sealed class DecodeResult
    {
        public DecodeResult(TuningProfile profile, int problems, bool isProfile)
        {
            Profile = profile;
            Problems = problems;
            IsProfile = isProfile;
        }

        public TuningProfile Profile { get; }

        /// <summary>False when the text has no "v=" line, so it was never a profile (a URL, a chat message) and must not replace one.</summary>
        public bool IsProfile { get; }

        /// <summary>Lines that were unreadable, unknown, or out of range. Out-of-range values are clamped, not dropped.</summary>
        public int Problems { get; }
    }

    /// <summary>
    /// Plain "key=value" lines. The same text is what the mod saves and what the clipboard export shares,
    /// so a profile pasted from a friend goes through exactly the code path the save file does.
    /// Only values that differ from vanilla are written, which keeps saves short and diffable.
    /// </summary>
    public static class ProfileCodec
    {
        public const int Version = 1;

        private const string On = "1";
        private const string Off = "0";
        private const string Muted = "muted";
        private const string Custom = "custom";
        private const string Anyone = "none";

        public static string Encode(TuningProfile profile)
        {
            var lines = new List<string> { "v=" + Version };
            var vanilla = new TuningProfile();

            foreach (var id in Sorted(profile.Categories.Keys))
            {
                var m = profile.Categories[id];

                if (!m.IsIdentity)
                    WriteMultipliers(lines, "cat." + id, m);
            }

            foreach (var defName in Sorted(profile.Thoughts.Keys))
                WriteOverride(lines, "thought." + defName, profile.Thoughts[defName]);

            WriteBreaks(lines, profile.Breaks, vanilla.Breaks);
            WriteNeeds(lines, profile.Needs, vanilla.Needs);
            WriteBattle(lines, profile.BattleFocus, vanilla.BattleFocus);

            if (profile.ShowVanillaInTooltips != vanilla.ShowVanillaInTooltips)
                lines.Add("ui.vanillaTips=" + Bool(profile.ShowVanillaInTooltips));

            return string.Join("\n", lines);
        }

        public static DecodeResult Decode(string text)
        {
            var profile = new TuningProfile();
            var problems = 0;
            var hasVersion = false;

            foreach (var raw in text.Split('\n'))
            {
                var line = raw.Trim();

                if (line.Length == 0 || line[0] == '#')
                    continue;

                var eq = line.IndexOf('=');
                var key = eq > 0 ? line.Substring(0, eq).Trim() : "";
                var isRead = eq > 0 && ReadLine(profile, key, line.Substring(eq + 1).Trim());

                if (!isRead)
                    problems++;
                else if (key == "v")
                    hasVersion = true;
            }

            return new DecodeResult(profile, problems, hasVersion);
        }

        private static bool ReadLine(TuningProfile profile, string key, string value)
        {
            if (key == "v")
                return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var version) && version == Version;

            if (key == "ui.vanillaTips")
                return ReadBool(value, b => profile.ShowVanillaInTooltips = b);

            var lastDot = key.LastIndexOf('.');
            var firstDot = key.IndexOf('.');

            if (firstDot <= 0)
                return false;

            var section = key.Substring(0, firstDot);
            var middle = lastDot > firstDot ? key.Substring(firstDot + 1, lastDot - firstDot - 1) : "";
            var field = key.Substring(lastDot + 1);

            switch (section)
            {
                case "cat":
                    return middle.Length > 0 && ReadMultiplier(profile.EditCategory(middle), field, value);
                case "thought":
                    return middle.Length > 0 && ReadThought(profile, middle, field, value);
                case "break":
                    return ReadBreak(profile.Breaks, middle, field, value);
                case "need":
                    return ReadNeed(profile, middle, field, value);
                case "battle":
                    return middle.Length == 0 && ReadBattle(profile.BattleFocus, field, value);
                default:
                    return false;
            }
        }

        private static bool ReadMultiplier(Multipliers m, string field, string value)
        {
            switch (field)
            {
                case "mn":
                    return ReadFloat(value, Limits.Multiplier, f => m.MoodNegative = f);
                case "mp":
                    return ReadFloat(value, Limits.Multiplier, f => m.MoodPositive = f);
                case "on":
                    return ReadFloat(value, Limits.Multiplier, f => m.OpinionNegative = f);
                case "op":
                    return ReadFloat(value, Limits.Multiplier, f => m.OpinionPositive = f);
                case "d":
                    return ReadFloat(value, Limits.Duration, f => m.Duration = f);
                default:
                    return false;
            }
        }

        private static bool ReadThought(TuningProfile profile, string defName, string field, string value)
        {
            if (field == "mode")
            {
                switch (value)
                {
                    case Muted:
                        profile.Thoughts[defName] = ThoughtOverride.Muted.Instance;
                        return true;
                    case Custom:
                        if (!(profile.Thoughts.TryGetValue(defName, out var existing) && existing is ThoughtOverride.Custom))
                            profile.Thoughts[defName] = new ThoughtOverride.Custom(new Multipliers());

                        return true;
                    default:
                        return false;
                }
            }

            if (!profile.Thoughts.TryGetValue(defName, out var current))
            {
                current = new ThoughtOverride.Custom(new Multipliers());
                profile.Thoughts[defName] = current;
            }

            return current is ThoughtOverride.Custom custom && ReadMultiplier(custom.Values, field, value);
        }

        private static bool ReadBreak(BreakTuning breaks, string defName, string field, string value)
        {
            if (defName.Length == 0)
            {
                switch (field)
                {
                    case "threshold":
                        return ReadFloat(value, Limits.BreakThreshold, f => breaks.ThresholdMultiplier = f);
                    case "duration":
                        return ReadFloat(value, Limits.StateDuration, f => breaks.StateDurationMultiplier = f);
                    default:
                        return false;
                }
            }

            switch (field)
            {
                case "c":
                    return ReadFloat(value, Limits.Commonality, f => breaks.Commonality[defName] = f);
                case "trait":
                    if (value.Length == 0)
                        return false;

                    breaks.TraitGates[defName] = value == Anyone ? (TraitGate)TraitGate.Anyone.Instance : new TraitGate.Requires(value);
                    return true;
                default:
                    return false;
            }
        }

        private static bool ReadNeed(TuningProfile profile, string defName, string field, string value)
        {
            if (defName.Length == 0)
                return field == "baseline" && ReadFloat(value, Limits.MoodBaseline, f => profile.Needs.MoodBaselineShift = f);

            switch (field)
            {
                case "rise":
                    return ReadFloat(value, Limits.SeekerRate, f => profile.EditSeeker(defName).Rise = f);
                case "fall":
                    return ReadFloat(value, Limits.SeekerRate, f => profile.EditSeeker(defName).Fall = f);
                default:
                    return false;
            }
        }

        private static bool ReadBattle(BattleFocusSettings battle, string field, string value)
        {
            switch (field)
            {
                case "on":
                    return ReadBool(value, b => battle.Enabled = b);
                case "max":
                    return ReadFloat(value, Limits.BattleMaxMood, f => battle.MaxMoodMagnitude = f);
                case "hours":
                    return ReadFloat(value, Limits.BattleWindowHours, f => battle.CombatWindowHours = f);
                case "cats":
                    battle.Categories.Clear();

                    foreach (var id in value.Split(','))
                    {
                        var trimmed = id.Trim();

                        if (trimmed.Length > 0)
                            battle.Categories.Add(trimmed);
                    }

                    return true;
                default:
                    return false;
            }
        }

        /// <summary>Stores the value clamped to its range. An out-of-range value still counts as a problem so the user hears about it.</summary>
        private static bool ReadFloat(string value, Range range, Action<float> store)
        {
            if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) || float.IsNaN(parsed))
                return false;

            store(range.Clamp(parsed));

            return range.Contains(parsed);
        }

        private static bool ReadBool(string value, Action<bool> store)
        {
            if (value != On && value != Off)
                return false;

            store(value == On);

            return true;
        }

        private static void WriteMultipliers(List<string> lines, string prefix, Multipliers m)
        {
            var identity = Multipliers.Identity;

            WriteIfChanged(lines, prefix + ".mn", m.MoodNegative, identity.MoodNegative);
            WriteIfChanged(lines, prefix + ".mp", m.MoodPositive, identity.MoodPositive);
            WriteIfChanged(lines, prefix + ".on", m.OpinionNegative, identity.OpinionNegative);
            WriteIfChanged(lines, prefix + ".op", m.OpinionPositive, identity.OpinionPositive);
            WriteIfChanged(lines, prefix + ".d", m.Duration, identity.Duration);
        }

        private static void WriteOverride(List<string> lines, string prefix, ThoughtOverride thoughtOverride)
        {
            switch (thoughtOverride)
            {
                case ThoughtOverride.Muted _:
                    lines.Add(prefix + ".mode=" + Muted);
                    break;
                case ThoughtOverride.Custom custom:
                    lines.Add(prefix + ".mode=" + Custom);
                    WriteMultipliers(lines, prefix, custom.Values);
                    break;
                default:
                    throw new InvalidOperationException("Unknown thought override " + thoughtOverride.GetType().Name);
            }
        }

        private static void WriteBreaks(List<string> lines, BreakTuning b, BreakTuning vanilla)
        {
            WriteIfChanged(lines, "break.threshold", b.ThresholdMultiplier, vanilla.ThresholdMultiplier);
            WriteIfChanged(lines, "break.duration", b.StateDurationMultiplier, vanilla.StateDurationMultiplier);

            foreach (var defName in Sorted(b.Commonality.Keys))
                WriteIfChanged(lines, "break." + defName + ".c", b.Commonality[defName], 1f);

            foreach (var defName in Sorted(b.TraitGates.Keys))
                lines.Add("break." + defName + ".trait=" + TraitValue(b.TraitGates[defName]));
        }

        private static string TraitValue(TraitGate gate)
        {
            switch (gate)
            {
                case TraitGate.Anyone _:
                    return Anyone;
                case TraitGate.Requires requires:
                    return requires.TraitDefName;
                default:
                    throw new InvalidOperationException("Unknown trait gate " + gate.GetType().Name);
            }
        }

        private static void WriteNeeds(List<string> lines, NeedTuning n, NeedTuning vanilla)
        {
            WriteIfChanged(lines, "need.baseline", n.MoodBaselineShift, vanilla.MoodBaselineShift);

            foreach (var defName in Sorted(n.Seekers.Keys))
            {
                var rates = n.Seekers[defName];

                WriteIfChanged(lines, "need." + defName + ".rise", rates.Rise, 1f);
                WriteIfChanged(lines, "need." + defName + ".fall", rates.Fall, 1f);
            }
        }

        private static void WriteBattle(List<string> lines, BattleFocusSettings b, BattleFocusSettings vanilla)
        {
            if (b.Enabled != vanilla.Enabled)
                lines.Add("battle.on=" + Bool(b.Enabled));

            WriteIfChanged(lines, "battle.max", b.MaxMoodMagnitude, vanilla.MaxMoodMagnitude);
            WriteIfChanged(lines, "battle.hours", b.CombatWindowHours, vanilla.CombatWindowHours);

            if (b.Categories.Count > 0)
                lines.Add("battle.cats=" + string.Join(",", Sorted(b.Categories)));
        }

        private static void WriteIfChanged(List<string> lines, string key, float value, float vanilla)
        {
            if (value != vanilla)
                lines.Add(key + "=" + Number(value));
        }

        private static string Number(float value) => value.ToString("0.####", CultureInfo.InvariantCulture);

        private static string Bool(bool value) => value ? On : Off;

        private static List<string> Sorted(IEnumerable<string> keys)
        {
            var list = new List<string>(keys);

            list.Sort(StringComparer.Ordinal);

            return list;
        }

        /// <summary>Tab-separated thought list for the "copy thought list" developer export.</summary>
        public static string ThoughtListTsv(IEnumerable<KeyValuePair<ThoughtFacts, string>> classified)
        {
            var sb = new StringBuilder("defName\tcategory\tmod\tfile\tsocial\tmemory\tprecept\ttraitGated\tdurationDays\tstages\n");

            foreach (var pair in classified)
            {
                var t = pair.Key;

                sb.Append(t.DefName).Append('\t')
                    .Append(pair.Value).Append('\t')
                    .Append(t.ModName).Append('\t')
                    .Append(t.FileName).Append('\t')
                    .Append(Bool(t.IsSocial)).Append('\t')
                    .Append(Bool(t.IsMemory)).Append('\t')
                    .Append(Bool(t.IsPrecept)).Append('\t')
                    .Append(Bool(t.HasRequiredTraits)).Append('\t')
                    .Append(Number(t.DurationDays)).Append('\t');

                for (var i = 0; i < t.Stages.Count; i++)
                {
                    if (i > 0)
                        sb.Append(' ');

                    sb.Append(Number(t.Stages[i].Mood)).Append('/').Append(Number(t.Stages[i].Opinion));
                }

                sb.Append('\n');
            }

            return sb.ToString();
        }
    }
}
