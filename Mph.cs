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
        // A sign's TMP object is created before SignPlacer assigns its final
        // number. Track the converted value rather than just the instance ID,
        // so a later change from its default text to the actual limit is
        // converted once, while repeated periodic scans remain idempotent.
        private static readonly Dictionary<int, string> ConvertedTextValues = new Dictionary<int, string>();
        private static int lastBaseSignCount = -1;
        private static int lastSignDebugCount = -1;

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
                    ConvertSpeedLimitText(__instance);
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
                ConvertedTextValues[id] = text.text;
                return true;
            }

            return false;
        }

        private static void ConvertSpeedLimitText(TMP_Text text, int kmh)
        {
            var mph = Mathf.RoundToInt(kmh * 10f / Constants.KmPerMile / 5) * 5;
            text.text = mph.ToString();
        }

        // Existing map signs are already present when UMM enables this mod, so
        // they do not necessarily pass through the creation-time patches above.
        // BaseSign limits the scan to physical trackside signs, not menu/HUD text.
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

            if (baseSignCount != lastBaseSignCount || signDebugCount != lastSignDebugCount || convertedCount > 0)
            {
                Main.DebugLog($"Build 99.7 sign scan: BaseSign={baseSignCount}, SignDebug={signDebugCount}, TMP={textCount}, numeric={numericTextCount}, converted={convertedCount}.");
                lastBaseSignCount = baseSignCount;
                lastSignDebugCount = signDebugCount;
            }
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
