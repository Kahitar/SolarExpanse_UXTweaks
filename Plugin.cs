#nullable disable
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace SolarExpanseUXTweaks
{
    [BepInPlugin("com.mod.solarexpanse.uxtweaks", "UXTweaks", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            new Harmony("com.mod.solarexpanse.uxtweaks").PatchAll();
            Logger.LogInfo("UXTweaks loaded");
        }
    }
}
