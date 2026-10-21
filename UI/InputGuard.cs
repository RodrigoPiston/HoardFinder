using BepInEx;
using UnityEngine;

namespace HoardFinder.UI
{
    /// <summary>
    /// Keeps the game's input locked for a moment after the window closes. The Enter, Esc or click that closed it is
    /// still registered by ZInput (for the frame and for the next physics step, where attacks are processed), so
    /// releasing the lock immediately made chat open or the player swing. The lock stays until those keys and mouse
    /// buttons are released (at least <see cref="MinFrames"/> frames, at most <see cref="MaxSeconds"/>), then
    /// ZInput's button states are reset, as Valheim itself does when focus changes.
    /// </summary>
    internal static class InputGuard
    {
        private const int MinFrames = 2;
        private const float MaxSeconds = 1f;

        private static int _minFrame;
        private static float _deadline;

        public static bool Active { get; private set; }

        public static void Engage()
        {
            Active = true;
            _minFrame = Time.frameCount + MinFrames;
            _deadline = Time.unscaledTime + MaxSeconds;
            ZInput.ResetAllButtonStates();
        }

        /// <summary>Called every frame from the plugin's Update.</summary>
        public static void Tick()
        {
            if (!Active)
            {
                return;
            }
            bool expired = Time.unscaledTime > _deadline;
            if (!expired && (Time.frameCount <= _minFrame || ClosingInputHeld()))
            {
                return;
            }
            Active = false;
            ZInput.ResetAllButtonStates();
        }

        private static bool ClosingInputHeld()
        {
            IInputSystem input = UnityInput.Current;
            return input.GetMouseButton(0) || input.GetMouseButton(1) || input.GetMouseButton(2)
                || input.GetKey(KeyCode.Return) || input.GetKey(KeyCode.KeypadEnter) || input.GetKey(KeyCode.Escape);
        }
    }
}
