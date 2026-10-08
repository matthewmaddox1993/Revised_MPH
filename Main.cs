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
                EnsureSignConversionRunner();
                SceneManager.sceneLoaded += OnSceneLoaded;
                harmony.PatchAll();
                DebugLog("Build 99.7 MPH conversion enabled.");
            }
            else
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                harmony.UnpatchAll(modEntry.Info.Id);
                if (signConversionRunner != null)
                    Object.Destroy(signConversionRunner.gameObject);
                signConversionRunner = null;
            }
            return true;
        }

        private static void OnSceneLoaded(Scene _, LoadSceneMode __)
        {
            Mph.MphSigns.ConvertLoadedSigns();
            signConversionRunner?.QueueScans();
        }

        private static void EnsureSignConversionRunner()
        {
            if (signConversionRunner != null)
                return;

            var runnerObject = new GameObject("[Revised MPH sign conversion]");
            Object.DontDestroyOnLoad(runnerObject);
            signConversionRunner = runnerObject.AddComponent<SceneSignConversionRunner>();
            signConversionRunner.QueueScans();
        }

        static public void DebugLog(string s)
        {
            mod?.Logger.Log(s);
        }
    }

    internal sealed class SceneSignConversionRunner : MonoBehaviour
    {
        // B99 streams and populates map signs well after the initial scene-load
        // event. A small periodic scan keeps late-created signs in sync without
        // requiring the player to reload the mod in UMM.
        private const float ScanIntervalSeconds = 5f;

        private float nextScanAt;

        public void QueueScans()
        {
            nextScanAt = 0f;
        }

        private void Update()
        {
            if (Time.unscaledTime < nextScanAt)
                return;

            Mph.MphSigns.ConvertLoadedSigns();
            nextScanAt = Time.unscaledTime + ScanIntervalSeconds;
        }
    }
}
