using HarmonyLib;
using UnityModManagerNet;
using UnityEngine.SceneManagement;

namespace DvMod.Mph
{
    [EnableReloading]
    public static class Main
    {
        public static UnityModManager.ModEntry? mod;

        static public bool Load(UnityModManager.ModEntry modEntry)
        {
            mod = modEntry;

            modEntry.OnToggle = OnToggle;

            return true;
        }

        static private bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            var harmony = new Harmony(modEntry.Info.Id);
            if (value)
            {
                SceneManager.sceneLoaded += OnSceneLoaded;
                harmony.PatchAll();
            }
            else
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                harmony.UnpatchAll(modEntry.Info.Id);
            }
            return true;
        }

        private static void OnSceneLoaded(Scene _, LoadSceneMode __)
        {
            Mph.MphSigns.ConvertLoadedSigns();
        }

        static public void DebugLog(string s)
        {
            mod?.Logger.Log(s);
        }
    }
}
