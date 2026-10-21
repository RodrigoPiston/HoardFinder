using System;
using System.Collections.Generic;

namespace HoardFinder.UI
{
    /// <summary>Wrap-around stepping for the toolbar settings.</summary>
    internal static class OptionCycling
    {
        public static T Step<T>(T value, int step) where T : Enum
        {
            int count = Enum.GetValues(typeof(T)).Length;
            int next = ((Convert.ToInt32(value) + step) % count + count) % count;
            return (T)Enum.ToObject(typeof(T), next);
        }

        /// <summary>Cycles null (all mods), then each mod present nearby, then back to null.</summary>
        public static string StepMod(List<string> mods, string current, int step)
        {
            int index = current == null ? -1 : mods.IndexOf(current);
            int next = index + step;
            if (next < -1)
            {
                next = mods.Count - 1;
            }
            return next < 0 || next >= mods.Count ? null : mods[next];
        }
    }
}
