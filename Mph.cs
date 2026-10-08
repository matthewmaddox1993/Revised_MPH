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
        private static readonly HashSet<int> ConvertedTextIds = new HashSet<int>();

        // Static map signs retain the SignDebug component in B99, now provided by
        // DV.SignPlacer. Their numeric TMP label is ready immediately after Awake.
        [HarmonyPatch(typeof(TextMeshPro), "Awake")]
        public static class StaticSpeedSignsPatch
        {
            public static void Postfix(TextMeshPro __instance)
            {
                if (__instance.GetComponentInParent<SignDebug>() != null)
                    MphSigns.ConvertSpeedLimitText(__instance);
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
                signType == SignType.SpeedLimitYellowOld;

            private static void SetSpeedLimitText(SignGenerator generator, int signIndex, int kmh)
            {
                var sign = generator.transform.Find($"[Sign{signIndex}]")?.GetComponent<BaseSign>();
                var text = sign?.GetTextObject();
                if (text != null)
                    MphSigns.ConvertSpeedLimitText(text, kmh);
            }
        }

        private static void ConvertSpeedLimitText(TextMeshPro text)
        {
            if (!ConvertedTextIds.Contains(text.GetInstanceID()) && int.TryParse(text.text, out var kmh))
                ConvertSpeedLimitText(text, kmh);
        }

        private static void ConvertSpeedLimitText(TextMeshPro text, int kmh)
        {
            var mph = Mathf.RoundToInt(kmh * 10f / Constants.KmPerMile / 5) * 5;
            text.text = mph.ToString();
            ConvertedTextIds.Add(text.GetInstanceID());
        }

        public static void ConvertLoadedSigns()
        {
            foreach (var sign in Object.FindObjectsOfType<SignDebug>())
            {
                foreach (var text in sign.GetComponentsInChildren<TextMeshPro>(true))
                    ConvertSpeedLimitText(text);
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
