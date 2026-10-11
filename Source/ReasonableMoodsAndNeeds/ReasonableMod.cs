using System;
using System.Collections.Generic;
using System.Diagnostics;
using HarmonyLib;
using ReasonableMoodsAndNeeds.Core;
using ReasonableMoodsAndNeeds.Game;
using ReasonableMoodsAndNeeds.UI;
using UnityEngine;
using Verse;

namespace ReasonableMoodsAndNeeds
{
    public sealed class RmnSettings : ModSettings
    {
        /// <summary>The profile as ProfileCodec lines. Null until the player first closes the settings window.</summary>
        public List<string>? profile;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref profile, "profile", LookMode.Value);
        }
    }

    /// <summary>Everything that exists only once defs are loaded.</summary>
    public sealed class Catalogs
    {
        public Catalogs(ThoughtCatalog thoughts, BreakCatalog breaks, NeedCatalog needs)
        {
            Thoughts = thoughts;
            Breaks = breaks;
            Needs = needs;
        }

        public ThoughtCatalog Thoughts { get; }

        public BreakCatalog Breaks { get; }

        public NeedCatalog Needs { get; }
    }

    /// <summary>
    /// Composition root. Loads the saved profile, snapshots defs once every mod has loaded,
    /// and rewrites defs whenever the settings window closes. Nothing here runs per tick.
    /// </summary>
    public sealed class ReasonableMod : Mod
    {
        public const string LogPrefix = "[Reasonable Moods and Needs] ";

        private readonly RmnSettings settings;
        private readonly Harmony harmony = new Harmony("AnExiledDev.ReasonableMoodsAndNeeds");
        private SettingsWindow? window;

        public ReasonableMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<RmnSettings>();
            Profile = LoadProfile(settings);
        }

        public TuningProfile Profile { get; set; }

        public Catalogs? Catalogs { get; private set; }

        public override string SettingsCategory() => "RMN.ModName".Translate();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            window ??= new SettingsWindow(this);
            window.Draw(inRect);
        }

        public override void WriteSettings()
        {
            settings.profile = new List<string>(ProfileCodec.Encode(Profile).Split('\n'));
            base.WriteSettings();
            Apply();
        }

        /// <summary>Called once, after every mod's static constructors, so other mods' def edits are part of the vanilla snapshot.</summary>
        public void Initialize()
        {
            var timer = Stopwatch.StartNew();

            Catalogs = new Catalogs(new ThoughtCatalog(), new BreakCatalog(), new NeedCatalog());
            Apply();
            timer.Stop();
            Log.Message(LogPrefix + "Tuned " + Catalogs.Thoughts.Entries.Count + " thoughts in " + timer.ElapsedMilliseconds + " ms.");
        }

        private void Apply()
        {
            if (Catalogs == null)
                return;

            Catalogs.Thoughts.Apply(Profile);
            Catalogs.Breaks.Apply(Profile.Breaks);
            Catalogs.Needs.Apply(Profile.Needs);
            BattleFocusPatch.Configure(harmony, Catalogs.Thoughts, Profile.BattleFocus);
        }

        private static TuningProfile LoadProfile(RmnSettings saved)
        {
            if (saved.profile == null)
                return Presets.Build(PresetId.Reasonable);

            var result = ProfileCodec.Decode(string.Join("\n", saved.profile));

            if (result.Problems > 0)
                Log.Warning(LogPrefix + result.Problems + " saved setting(s) could not be read and were reset or clamped.");

            return result.Profile;
        }
    }

    [StaticConstructorOnStartup]
    public static class Startup
    {
        static Startup()
        {
            LongEventHandler.ExecuteWhenFinished(InitializeSafely);
        }

        private static void InitializeSafely()
        {
            try
            {
                LoadedModManager.GetMod<ReasonableMod>().Initialize();
            }
            catch (Exception e)
            {
                Log.Error(ReasonableMod.LogPrefix + "Startup failed, some settings may not be applied: " + e);
            }
        }
    }
}
