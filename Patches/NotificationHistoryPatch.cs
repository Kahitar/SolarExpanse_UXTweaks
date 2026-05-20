#nullable disable
using System;
using System.Collections.Generic;
using System.Reflection;
using Game.UI.MainWindow;
using HarmonyLib;
using Manager;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SolarExpanseUXTweaks.Patches
{
    [HarmonyPatch(typeof(NotificationManager), "ShowHistory")]
    internal static class NotificationHistoryShowPatch
    {
        [HarmonyPostfix]
        private static void Postfix(NotificationManager __instance)
        {
            NotificationHistoryTweaks.CollapseDuplicateRows(__instance);
        }
    }

    [HarmonyPatch(typeof(NotificationManager), nameof(NotificationManager.ShowNotification))]
    internal static class NotificationHistoryShowNotificationPatch
    {
        [HarmonyPostfix]
        private static void Postfix(NotificationManager __instance)
        {
            NotificationHistoryTweaks.CollapseDuplicateRows(__instance);
        }
    }

    [HarmonyPatch(typeof(NotificationManager), nameof(NotificationManager.ClearNotificationUILast))]
    internal static class NotificationHistoryClearLastPatch
    {
        [HarmonyPostfix]
        private static void Postfix(NotificationManager __instance)
        {
            NotificationHistoryTweaks.CollapseDuplicateRows(__instance);
        }
    }

    internal static class NotificationHistoryTweaks
    {
        private const string CounterObjectName = "uxTweaksNotificationCounter";
        private const string CounterTextObjectName = "uxTweaksNotificationCounterText";
        private const string ScrollbarObjectName = "uxTweaksNotificationScrollbar";
        private const float CounterWidth = 44f;
        private const float CounterHeight = 24f;
        private const float CounterRightPadding = 8f;
        private const float CounterReservedWidth = CounterWidth + CounterRightPadding + 12f;
        private const float MinimumTextWidthAfterReserve = 80f;

        private static readonly FieldInfo NotificationHistoryField = AccessTools.Field(typeof(NotificationManager), "notificationHistory");
        private static readonly FieldInfo NotificationTextField = AccessTools.Field(typeof(NotificationUI), "text");
        private static bool loggedUnsupportedScrollHierarchy;

        internal static void CollapseDuplicateRows(NotificationManager manager)
        {
            try
            {
                ConfigureScrollRect(manager);
                CollapseRows(manager);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[UXTweaks] Notification history collapse failed: {ex}");
            }
        }

        private static void ConfigureScrollRect(NotificationManager manager)
        {
            RectTransform content = GetHistoryContent(manager);
            GameObject historyPanel = NotificationHistoryField?.GetValue(manager) as GameObject;
            RectTransform viewport = historyPanel != null ? historyPanel.transform as RectTransform : null;
            if (content == null || viewport == null || !historyPanel.activeInHierarchy)
            {
                return;
            }

            if (viewport == content)
            {
                if (!loggedUnsupportedScrollHierarchy)
                {
                    loggedUnsupportedScrollHierarchy = true;
                    Plugin.Log?.LogWarning("[UXTweaks] Notification history content is the panel root; leaving scroll setup disabled to avoid hiding rows.");
                }
                return;
            }

            ScrollRect scrollRect = FindScrollRect(manager, content);
            if (scrollRect == null)
            {
                scrollRect = viewport.gameObject.AddComponent<ScrollRect>();
            }

            scrollRect.content = content;
            scrollRect.viewport = viewport;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.inertia = true;
            scrollRect.scrollSensitivity = 45f;

            ResizeContentForScroll(viewport, content);

            Scrollbar scrollbar = EnsureScrollbar(viewport);
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            LayoutRebuilder.ForceRebuildLayoutImmediate(viewport);
        }

        private static void CollapseRows(NotificationManager manager)
        {
            RectTransform content = GetHistoryContent(manager);
            if (content == null)
            {
                return;
            }

            Dictionary<string, NotificationGroup> groups = new Dictionary<string, NotificationGroup>();
            List<NotificationUI> duplicates = new List<NotificationUI>();

            for (int i = 0; i < content.childCount; i++)
            {
                NotificationUI row = content.GetChild(i).GetComponent<NotificationUI>();
                if (row == null)
                {
                    continue;
                }

                NotificationRowState state = row.GetComponent<NotificationRowState>();
                if (state != null && state.IsMergedDuplicate)
                {
                    continue;
                }

                string text = GetNotificationText(row);
                if (string.IsNullOrWhiteSpace(text))
                {
                    SetCounter(row, 1);
                    continue;
                }

                string key = BuildNotificationKey(row, text);
                int count = state != null && state.Count > 0 ? state.Count : 1;

                if (groups.TryGetValue(key, out NotificationGroup group))
                {
                    group.Count += count;
                    duplicates.Add(row);
                    continue;
                }

                groups.Add(key, new NotificationGroup(row, count));
            }

            foreach (NotificationGroup group in groups.Values)
            {
                SetCounter(group.Row, group.Count);
            }

            foreach (NotificationUI duplicate in duplicates)
            {
                MarkAndDestroyDuplicate(duplicate);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        }

        private static RectTransform GetHistoryContent(NotificationManager manager)
        {
            return manager != null ? manager.TransformParentNotificationHistory as RectTransform : null;
        }

        private static void ResizeContentForScroll(RectTransform viewport, RectTransform content)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);

            float preferredHeight = LayoutUtility.GetPreferredHeight(content);
            if (preferredHeight <= 0f)
            {
                preferredHeight = EstimateContentHeight(content);
            }

            float viewportHeight = viewport.rect.height;
            if (preferredHeight > 0f && viewportHeight > 0f)
            {
                content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Math.Max(viewportHeight, preferredHeight));
            }
        }

        private static float EstimateContentHeight(RectTransform content)
        {
            float height = 0f;
            for (int i = 0; i < content.childCount; i++)
            {
                RectTransform child = content.GetChild(i) as RectTransform;
                if (child == null || !child.gameObject.activeSelf)
                {
                    continue;
                }

                height += Math.Max(LayoutUtility.GetPreferredHeight(child), child.rect.height);
            }

            VerticalLayoutGroup layoutGroup = content.GetComponent<VerticalLayoutGroup>();
            if (layoutGroup != null)
            {
                int activeChildren = 0;
                for (int i = 0; i < content.childCount; i++)
                {
                    GameObject child = content.GetChild(i).gameObject;
                    if (child.activeSelf)
                    {
                        activeChildren++;
                    }
                }

                height += layoutGroup.padding.top + layoutGroup.padding.bottom;
                height += Math.Max(0, activeChildren - 1) * layoutGroup.spacing;
            }

            return height;
        }

        private static ScrollRect FindScrollRect(NotificationManager manager, RectTransform content)
        {
            GameObject historyPanel = NotificationHistoryField?.GetValue(manager) as GameObject;
            if (historyPanel == null)
            {
                return null;
            }

            foreach (ScrollRect scrollRect in historyPanel.GetComponentsInChildren<ScrollRect>(includeInactive: true))
            {
                if (scrollRect.content == null || scrollRect.content == content)
                {
                    return scrollRect;
                }
            }

            return null;
        }

        private static Scrollbar EnsureScrollbar(RectTransform viewport)
        {
            Transform existing = viewport.Find(ScrollbarObjectName);
            if (existing != null)
            {
                Scrollbar existingScrollbar = existing.GetComponent<Scrollbar>();
                if (existingScrollbar != null)
                {
                    return existingScrollbar;
                }
            }

            GameObject scrollbarObject = new GameObject(ScrollbarObjectName, typeof(RectTransform));
            scrollbarObject.transform.SetParent(viewport, worldPositionStays: false);

            RectTransform scrollbarRect = scrollbarObject.GetComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(1f, 0f);
            scrollbarRect.anchorMax = new Vector2(1f, 1f);
            scrollbarRect.pivot = new Vector2(1f, 0.5f);
            scrollbarRect.anchoredPosition = new Vector2(-3f, 0f);
            scrollbarRect.sizeDelta = new Vector2(8f, 0f);

            LayoutElement layoutElement = scrollbarObject.AddComponent<LayoutElement>();
            layoutElement.ignoreLayout = true;

            Image trackImage = scrollbarObject.AddComponent<Image>();
            trackImage.color = new Color(0f, 0f, 0f, 0.18f);

            GameObject slidingAreaObject = new GameObject("Sliding Area", typeof(RectTransform));
            slidingAreaObject.transform.SetParent(scrollbarObject.transform, worldPositionStays: false);
            RectTransform slidingAreaRect = slidingAreaObject.GetComponent<RectTransform>();
            slidingAreaRect.anchorMin = Vector2.zero;
            slidingAreaRect.anchorMax = Vector2.one;
            slidingAreaRect.offsetMin = new Vector2(0f, 4f);
            slidingAreaRect.offsetMax = new Vector2(0f, -4f);

            GameObject handleObject = new GameObject("Handle", typeof(RectTransform));
            handleObject.transform.SetParent(slidingAreaObject.transform, worldPositionStays: false);
            RectTransform handleRect = handleObject.GetComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.offsetMin = Vector2.zero;
            handleRect.offsetMax = Vector2.zero;

            Image handleImage = handleObject.AddComponent<Image>();
            handleImage.color = new Color(1f, 1f, 1f, 0.42f);

            Scrollbar scrollbar = scrollbarObject.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = handleImage;

            return scrollbar;
        }

        private static string BuildNotificationKey(NotificationUI row, string text)
        {
            string notificationId = string.Empty;
            try
            {
                if (row.notification != null)
                {
                    notificationId = row.notification.ID ?? string.Empty;
                }
            }
            catch
            {
                notificationId = string.Empty;
            }

            return notificationId + "\n" + (row.extraString ?? string.Empty) + "\n" + text.Trim();
        }

        private static string GetNotificationText(NotificationUI row)
        {
            TMP_Text text = GetNotificationTextComponent(row);
            return text != null ? text.text ?? string.Empty : string.Empty;
        }

        private static TMP_Text GetNotificationTextComponent(NotificationUI row)
        {
            TMP_Text text = NotificationTextField?.GetValue(row) as TMP_Text;
            if (text != null)
            {
                return text;
            }

            foreach (TMP_Text candidate in row.GetComponentsInChildren<TMP_Text>(includeInactive: true))
            {
                if (candidate.gameObject.name == CounterTextObjectName)
                {
                    continue;
                }

                return candidate;
            }

            return null;
        }

        private static void SetCounter(NotificationUI row, int count)
        {
            NotificationRowState state = row.GetComponent<NotificationRowState>();
            if (state == null)
            {
                state = row.gameObject.AddComponent<NotificationRowState>();
            }

            state.Count = Math.Max(1, count);
            state.IsMergedDuplicate = false;

            GameObject counterObject = EnsureCounterObject(row);
            bool showCounter = state.Count > 1;
            counterObject.SetActive(showCounter);
            ReserveTextSpace(row, showCounter);

            if (!showCounter)
            {
                return;
            }

            TMP_Text counterText = counterObject.GetComponentInChildren<TMP_Text>(includeInactive: true);
            if (counterText != null)
            {
                counterText.text = "x" + state.Count;
            }
        }

        private static GameObject EnsureCounterObject(NotificationUI row)
        {
            Transform existing = row.transform.Find(CounterObjectName);
            if (existing != null)
            {
                return existing.gameObject;
            }

            GameObject counterObject = new GameObject(CounterObjectName, typeof(RectTransform));
            counterObject.transform.SetParent(row.transform, worldPositionStays: false);

            RectTransform counterRect = counterObject.GetComponent<RectTransform>();
            counterRect.anchorMin = new Vector2(1f, 0.5f);
            counterRect.anchorMax = new Vector2(1f, 0.5f);
            counterRect.pivot = new Vector2(1f, 0.5f);
            counterRect.anchoredPosition = new Vector2(-CounterRightPadding, 0f);
            counterRect.sizeDelta = new Vector2(CounterWidth, CounterHeight);

            LayoutElement layoutElement = counterObject.AddComponent<LayoutElement>();
            layoutElement.ignoreLayout = true;

            Image background = counterObject.AddComponent<Image>();
            background.color = new Color(0.12f, 0.14f, 0.17f, 0.92f);
            background.raycastTarget = false;

            GameObject textObject = new GameObject(CounterTextObjectName, typeof(RectTransform));
            textObject.transform.SetParent(counterObject.transform, worldPositionStays: false);

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMin = 10f;
            label.fontSizeMax = 15f;
            label.fontStyle = FontStyles.Bold;
            label.color = new Color(1f, 1f, 1f, 0.96f);
            label.raycastTarget = false;

            TMP_Text sourceText = GetNotificationTextComponent(row);
            if (sourceText != null)
            {
                label.font = sourceText.font;
                label.fontMaterial = sourceText.fontMaterial;
            }

            return counterObject;
        }

        private static void ReserveTextSpace(NotificationUI row, bool reserve)
        {
            TMP_Text text = GetNotificationTextComponent(row);
            RectTransform textRect = text != null ? text.transform as RectTransform : null;
            if (textRect == null)
            {
                return;
            }

            NotificationTextLayoutState state = textRect.GetComponent<NotificationTextLayoutState>();
            if (state == null)
            {
                state = textRect.gameObject.AddComponent<NotificationTextLayoutState>();
                state.OriginalOffsetMax = textRect.offsetMax;
                state.OriginalSizeDelta = textRect.sizeDelta;
            }

            if (!reserve)
            {
                textRect.offsetMax = state.OriginalOffsetMax;
                textRect.sizeDelta = state.OriginalSizeDelta;
                return;
            }

            if (!Mathf.Approximately(textRect.anchorMin.x, textRect.anchorMax.x))
            {
                textRect.offsetMax = new Vector2(Math.Min(state.OriginalOffsetMax.x, -CounterReservedWidth), textRect.offsetMax.y);
                return;
            }

            if (state.OriginalSizeDelta.x >= CounterReservedWidth + MinimumTextWidthAfterReserve)
            {
                textRect.sizeDelta = new Vector2(state.OriginalSizeDelta.x - CounterReservedWidth, textRect.sizeDelta.y);
            }
        }

        private static void MarkAndDestroyDuplicate(NotificationUI duplicate)
        {
            if (duplicate == null)
            {
                return;
            }

            NotificationRowState state = duplicate.GetComponent<NotificationRowState>();
            if (state == null)
            {
                state = duplicate.gameObject.AddComponent<NotificationRowState>();
            }

            state.IsMergedDuplicate = true;
            state.Count = 0;
            duplicate.gameObject.SetActive(false);
            UnityEngine.Object.Destroy(duplicate.gameObject);
        }

        private sealed class NotificationGroup
        {
            internal readonly NotificationUI Row;
            internal int Count;

            internal NotificationGroup(NotificationUI row, int count)
            {
                Row = row;
                Count = count;
            }
        }
    }

    internal sealed class NotificationRowState : MonoBehaviour
    {
        internal int Count = 1;
        internal bool IsMergedDuplicate;
    }

    internal sealed class NotificationTextLayoutState : MonoBehaviour
    {
        internal Vector2 OriginalOffsetMax;
        internal Vector2 OriginalSizeDelta;
    }
}
