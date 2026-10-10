using DV.HUD;
using DV.Signs;
using DV.Customization.Gadgets.Implementations;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

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
            // Keep this hook as cheap as possible: it runs for every TextMeshPro
            // in the game. The parent-hierarchy check is deferred to the budgeted
            // queue drain, where it only runs for numeric text.
            public static void Postfix(TextMeshPro __instance)
            {
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

        private static void ConvertSpeedLimitText(TMP_Text text, int signValue)
        {
            // Sign text stores the speed in tens of km/h (for example, "6" means
            // 60 km/h), so restore that scale before converting to mph.
            var kmh = signValue * 10f;
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

        // Called from the runner once per frame. The work is bounded by a time
        // budget so a burst of streamed signs cannot turn into a single large
        // frame-time spike. Remaining texts are simply handled on later frames.
        public static void ProcessPendingTexts(double budgetMilliseconds)
        {
            if (PendingTexts.Count == 0)
                return;

            var stopwatch = Stopwatch.StartNew();
            while (PendingTexts.Count > 0)
            {
                var text = PendingTexts.Dequeue();
                PendingTextIds.Remove(text != null ? text.GetInstanceID() : 0);
                if (text != null && IsSignText(text))
                    ConvertSpeedLimitText(text);

                if (stopwatch.Elapsed.TotalMilliseconds >= budgetMilliseconds)
                    break;
            }
        }

        // Cheap numeric test first; only numeric text pays for the hierarchy walk.
        private static bool IsSignText(TMP_Text text) =>
            int.TryParse(text.text, out _) &&
            (text.GetComponentInParent<BaseSign>() != null ||
             text.GetComponentInParent<SignDebug>() != null);


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

        // The shop digital speedometer and CCL's IndicatorLCDDriver both send
        // their display text through LCDDriver.Display. Convert only displays
        // that are known to represent speed; other LCD gadgets must stay in
        // their original units.
        [HarmonyPatch(typeof(LCDDriver), nameof(LCDDriver.Display))]
        public static class DigitalSpeedometerDisplayPatch
        {
            public static void Prefix(LCDDriver __instance, ref string s)
            {
                if (IsSpeedDisplay(__instance))
                    s = ConvertSpeedText(s);
            }
        }

        // CCL also supports a TMP-backed digital indicator. The type lives in
        // the optional CCL assembly, so use a conditional Harmony target and
        // avoid making this mod require CCL to be installed.
        [HarmonyPatch]
        public static class CustomTextSpeedometerPatch
        {
            private const string IndicatorTypeName =
                "CCL.Importer.Components.Indicators.IndicatorTMPInternal";

            public static bool Prepare() => AccessTools.TypeByName(IndicatorTypeName) != null;

            public static MethodBase? TargetMethod()
            {
                var type = AccessTools.TypeByName(IndicatorTypeName);
                return type == null ? null : AccessTools.Method(type, "OnValueSet");
            }

            public static void Postfix(object __instance)
            {
                var component = __instance as Component;
                if (component == null || !IsSpeedIndicator(component))
                    return;

                var text = AccessTools.Field(__instance.GetType(), "Text")?.GetValue(__instance) as TMP_Text;
                if (text != null)
                    text.text = ConvertSpeedText(text.text);
            }
        }

        private static bool IsSpeedDisplay(LCDDriver display)
        {
            if (display.GetComponentInParent<GadgetDigitalSpeedometerLOD>() != null)
                return true;

            return IsSpeedIndicator(display);
        }

        private static bool IsSpeedIndicator(Component component)
        {
            var reader = component.GetComponentInParent<LocoIndicatorReader>();
            var speed = reader?.speed;
            if (speed == null)
                return false;

            return speed == component ||
                   speed.transform.IsChildOf(component.transform) ||
                   component.transform.IsChildOf(speed.transform);
        }

        private static string ConvertSpeedText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            var start = 0;
            while (start < text.Length && char.IsWhiteSpace(text[start]))
                start++;

            var end = start;
            if (end < text.Length && (text[end] == '+' || text[end] == '-'))
                end++;

            var decimalSeparatorSeen = false;
            while (end < text.Length)
            {
                var character = text[end];
                if (char.IsDigit(character))
                {
                    end++;
                    continue;
                }

                if ((character == '.' || character == ',') && !decimalSeparatorSeen)
                {
                    decimalSeparatorSeen = true;
                    end++;
                    continue;
                }

                break;
            }

            if (end <= start ||
                !TryParseNumber(text.Substring(start, end - start), out var kmh))
                return text;

            var numericText = text.Substring(start, end - start);
            var decimals = GetDecimalPlaces(numericText);
            var mph = Math.Round(kmh / Constants.KmPerMile, decimals, MidpointRounding.AwayFromZero);
            var format = decimals == 0 ? "0" : "0." + new string('0', decimals);
            var convertedNumber = mph.ToString(format, CultureInfo.InvariantCulture);

            // Keep fixed-width zero padding used by some custom LCDs.
            var numericStart = numericText[0] == '+' || numericText[0] == '-' ? 1 : 0;
            var integerEnd = numericText.IndexOfAny(new[] { '.', ',' });
            if (integerEnd < 0)
                integerEnd = numericText.Length;
            var integerDigits = integerEnd - numericStart;
            var convertedSign = convertedNumber[0] == '-' || convertedNumber[0] == '+' ? 1 : 0;
            var convertedIntegerEnd = convertedNumber.IndexOf('.');
            if (convertedIntegerEnd < 0)
                convertedIntegerEnd = convertedNumber.Length;
            var convertedIntegerDigits = convertedIntegerEnd - convertedSign;
            if (integerDigits > 1 && numericText[numericStart] == '0' && convertedIntegerDigits < integerDigits)
                convertedNumber = convertedNumber.Insert(convertedSign, new string('0', integerDigits - convertedIntegerDigits));

            return text.Substring(0, start) + convertedNumber + text.Substring(end);
        }

        private static bool TryParseNumber(string text, out float value)
        {
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ||
                   float.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value);
        }

        private static int GetDecimalPlaces(string text)
        {
            var separator = text.IndexOfAny(new[] { '.', ',' });
            return separator < 0 ? 0 : text.Length - separator - 1;
        }
    }
}
