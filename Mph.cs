using DV.HUD;
using DV.Signs;
using HarmonyLib;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace DvMod.Mph
{
    public static class Constants
    {
        public const float KmPerMile = 1.609344f;
    }

    public static class MphSigns
    {
        // Sign text can be created before SignPlacer assigns its final number.
        // Queue those few text objects and process them on the next frame rather
        // than repeatedly scanning every object in the scene.
        private static readonly Dictionary<int, string> ConvertedTextValues = new Dictionary<int, string>();
        private static readonly Queue<TMP_Text> PendingTexts = new Queue<TMP_Text>();
        private static readonly HashSet<int> PendingTextIds = new HashSet<int>();

        // The permanent trackside boards are not all created by SignGenerator.
        // Their text belongs to BaseSign; limiting the hook to sign components
        // keeps HUD and menu text out of this conversion.
        [HarmonyPatch(typeof(TextMeshPro), "Awake")]
        public static class StaticSpeedSignsPatch
        {
            public static void Postfix(TextMeshPro __instance)
            {
                if (__instance.GetComponentInParent<BaseSign>() != null ||
                    __instance.GetComponentInParent<SignDebug>() != null)
                    QueueText(__instance);
            }
        }

        [HarmonyPatch(typeof(SignGenerator), nameof(SignGenerator.GenerateSign))]
        public static class GenerateSpeedSignsPatch
        {
            public static void Postfix(SignGenerator __instance)
            {
                if (__instance.data?.signParameters == null)
                    return;

                var signIndex = 0;
                foreach (var parameters in __instance.data.signParameters)
                {
                    if (parameters.sign == null)
                        continue;

                    if (IsSpeedLimit(parameters.type) && int.TryParse(parameters.signText, out var kmh))
                        SetSpeedLimitText(__instance, signIndex, kmh);

                    signIndex++;
                }
            }

            private static bool IsSpeedLimit(SignType signType) =>
                signType == SignType.SpeedLimit ||
                signType == SignType.SpeedLimitOld ||
                signType == SignType.SpeedLimitYellow ||
                signType == SignType.SpeedLimitYellowOld ||
                signType == SignType.UpcomingSpeedUp ||
                signType == SignType.UpcomingSpeedUpOld ||
                signType == SignType.UpcomingSpeedDown ||
                signType == SignType.UpcomingSpeedDownOld;

            private static void SetSpeedLimitText(SignGenerator generator, int signIndex, int kmh)
            {
                var sign = generator.transform.Find($"[Sign{signIndex}]")?.GetComponent<BaseSign>();
                var text = sign?.GetTextObject();
                if (text != null)
                    MphSigns.ConvertSpeedLimitText(text, kmh);
            }
        }

        private static bool ConvertSpeedLimitText(TMP_Text text)
        {
            if (int.TryParse(text.text, out var speedSignValue))
            {
                var id = text.GetInstanceID();
                if (ConvertedTextValues.TryGetValue(id, out var convertedValue) && convertedValue == text.text)
                    return false;

                ConvertSpeedLimitText(text, speedSignValue);
                return true;
            }

            return false;
        }

        private static void ConvertSpeedLimitText(TMP_Text text, int kmh)
        {
            var mph = Mathf.RoundToInt(kmh / Constants.KmPerMile / 5) * 5;
            var convertedText = mph.ToString();
            if (text.text != convertedText)
                text.text = convertedText;
            ConvertedTextValues[text.GetInstanceID()] = convertedText;
        }

        private static void QueueText(TMP_Text text)
        {
            var id = text.GetInstanceID();
            if (PendingTextIds.Add(id))
                PendingTexts.Enqueue(text);
        }

        // Called from the runner once per frame. The work is bounded so a burst
        // of streamed signs cannot turn into a single large frame-time spike.
        public static void ProcessPendingTexts(int maxTexts)
        {
            while (maxTexts-- > 0 && PendingTexts.Count > 0)
            {
                var text = PendingTexts.Dequeue();
                PendingTextIds.Remove(text != null ? text.GetInstanceID() : 0);
                if (text != null)
                    ConvertSpeedLimitText(text);
            }
        }

        // Existing map signs are already present when UMM enables this mod, so
        // they do not necessarily pass through the creation-time patches above.
        // This full scan is intentionally only used at enable/scene-load time;
        // streamed signs are handled by TextMeshPro.Awake and SignGenerator.
        public static void ConvertLoadedSigns()
        {
            var baseSignCount = 0;
            var signDebugCount = 0;
            var textCount = 0;
            var numericTextCount = 0;
            var convertedCount = 0;

            foreach (var sign in Object.FindObjectsOfType<BaseSign>())
            {
                baseSignCount++;
                var text = sign.GetTextObject();
                if (text != null)
                {
                    textCount++;
                    if (int.TryParse(text.text, out _))
                        numericTextCount++;
                    if (ConvertSpeedLimitText(text))
                        convertedCount++;
                }
            }

            // Map-authored signs retain SignDebug rather than BaseSign in B99.
            // This is intentionally separate from the BaseSign pass above.
            foreach (var sign in Object.FindObjectsOfType<SignDebug>())
            {
                signDebugCount++;
                foreach (var text in sign.GetComponentsInChildren<TMP_Text>(true))
                {
                    textCount++;
                    if (int.TryParse(text.text, out _))
                        numericTextCount++;
                    if (ConvertSpeedLimitText(text))
                        convertedCount++;
                }
            }

            Main.DebugLog($"Build 99.7 sign scan: BaseSign={baseSignCount}, SignDebug={signDebugCount}, TMP={textCount}, numeric={numericTextCount}, converted={convertedCount}.");
        }

        public static void Reset()
        {
            ConvertedTextValues.Clear();
            PendingTexts.Clear();
            PendingTextIds.Clear();
        }
    }

    public static class Speedometers
    {
        // B99 uses one gauge component for all locomotive types. LocoIndicatorReader
        // identifies which gauge is the speedometer, avoiding changes to pressure and
        // temperature gauges in the same cab.
        [HarmonyPatch(typeof(IndicatorGauge), "Start")]
        public static class SpeedometerRangePatch
        {
            public static void Postfix(IndicatorGauge __instance)
            {
                var reader = __instance.GetComponentInParent<LocoIndicatorReader>();
                if (reader?.speed == __instance)
                    __instance.maxValue *= Constants.KmPerMile;
            }
        }
    }
}
