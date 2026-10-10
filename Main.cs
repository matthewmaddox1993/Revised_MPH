using HarmonyLib;
using UnityModManagerNet;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DvMod.Mph
{
    [EnableReloading]
    public static class Main
    {
        public static UnityModManager.ModEntry? mod;
        private static SceneSignConversionRunner? signConversionRunner;

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
                harmony.PatchAll();
                Mph.MphSigns.ConvertLoadedSigns();
                EnsureSignConversionRunner();
                DebugLog("Build 99.7 MPH conversion enabled.");
            }
            else
            {
                harmony.UnpatchAll(modEntry.Info.Id);
                if (signConversionRunner != null)
                    Object.Destroy(signConversionRunner.gameObject);
                signConversionRunner = null;
                Mph.MphSigns.Reset();
            }
            return true;
        }

        private static void EnsureSignConversionRunner()
        {
            if (signConversionRunner != null)
                return;

            var runnerObject = new GameObject("[Revised MPH sign conversion]");
            Object.DontDestroyOnLoad(runnerObject);
            signConversionRunner = runnerObject.AddComponent<SceneSignConversionRunner>();
        }

        static public void DebugLog(string s)
        {
            mod?.Logger.Log(s);
        }
    }

    internal sealed class SceneSignConversionRunner : MonoBehaviour
    {
        private void Update()
        {
            // B99 streams signs as the player moves. TextMeshPro.Awake queues each
            // sign text, and this time-bounded drain prevents a burst from causing
            // a single frame-time spike.
            Mph.MphSigns.ProcessPendingTexts(0.75);
        }
    }
}
