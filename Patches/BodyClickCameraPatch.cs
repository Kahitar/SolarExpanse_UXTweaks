#nullable disable
using System;
using System.Collections.Generic;
using CameraControl;
using CustomUpdate;
using Game.Info;
using Game.VisualizationScripts;
using HarmonyLib;
using UnityEngine;

namespace SolarExpanseUXTweaks.Patches
{
    [HarmonyPatch(typeof(InfoBase), nameof(InfoBase.MyOnMouseUpAsButton2))]
    internal static class MapObjectClickCameraSuppressionScopePatch
    {
        private const float DoubleClickSeconds = 0.3f;

        private static Transform lastClickTarget;
        private static float lastClickTime = -100f;

        [ThreadStatic]
        private static Stack<Transform> suppressedTargets;

        [ThreadStatic]
        private static bool suppressTargetIsSpacecraftOnFly;

        internal static bool IsSuppressedMapObjectClickTarget(Transform target)
        {
            if (target == null || suppressedTargets == null || suppressedTargets.Count == 0)
            {
                return false;
            }

            foreach (Transform suppressedTarget in suppressedTargets)
            {
                if (suppressedTarget == target)
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool SuppressTargetIsSpacecraftOnFly => suppressTargetIsSpacecraftOnFly;

        [HarmonyPrefix]
        private static void Prefix(InfoBase __instance, ref Transform __state)
        {
            if (!ShouldGateCameraTarget(__instance))
            {
                return;
            }

            __state = __instance.transform;
            if (__state == null)
            {
                return;
            }

            if (ShouldAllowFocusForDoubleClick(__state))
            {
                __state = null;
                return;
            }

            PushSuppressedTarget(__state, suppressSpacecraftOnFly: false);
        }

        private static bool ShouldAllowFocusForDoubleClick(Transform target)
        {
            float now = Time.realtimeSinceStartup;
            bool isDoubleClick = lastClickTarget == target && now - lastClickTime <= DoubleClickSeconds;

            if (isDoubleClick)
            {
                lastClickTarget = null;
                lastClickTime = -100f;
                return true;
            }

            lastClickTarget = target;
            lastClickTime = now;
            return false;
        }

        internal static bool ShouldSuppressSingleClickTarget(Transform target)
        {
            return !ShouldAllowFocusForDoubleClick(target);
        }

        internal static void PushSuppressedTarget(Transform target, bool suppressSpacecraftOnFly)
        {
            if (target == null)
            {
                return;
            }

            if (suppressedTargets == null)
            {
                suppressedTargets = new Stack<Transform>();
            }

            suppressedTargets.Push(target);
            suppressTargetIsSpacecraftOnFly |= suppressSpacecraftOnFly;
        }

        internal static void PopSuppressedTarget(Transform target, bool suppressSpacecraftOnFly)
        {
            if (target == null || suppressedTargets == null || suppressedTargets.Count == 0)
            {
                return;
            }

            if (suppressedTargets.Peek() == target)
            {
                suppressedTargets.Pop();
                if (suppressSpacecraftOnFly)
                {
                    suppressTargetIsSpacecraftOnFly = false;
                }
                return;
            }

            Transform[] currentTargets = suppressedTargets.ToArray();
            suppressedTargets.Clear();

            bool removed = false;
            for (int i = currentTargets.Length - 1; i >= 0; i--)
            {
                Transform currentTarget = currentTargets[i];
                if (!removed && currentTarget == target)
                {
                    removed = true;
                    continue;
                }

                suppressedTargets.Push(currentTarget);
            }

            if (suppressSpacecraftOnFly)
            {
                suppressTargetIsSpacecraftOnFly = false;
            }
        }

        private static bool ShouldGateCameraTarget(InfoBase infoBase)
        {
            return infoBase is ObjectInfo || infoBase is MissionInfo;
        }

        [HarmonyFinalizer]
        private static void Finalizer(Transform __state)
        {
            PopSuppressedTarget(__state, suppressSpacecraftOnFly: false);
        }
    }

    [HarmonyPatch(typeof(MyCameraController), nameof(MyCameraController.ChangeTarget), typeof(Transform), typeof(bool))]
    internal static class MapObjectClickCameraChangeTargetPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Transform newTarget)
        {
            if (!MapObjectClickCameraSuppressionScopePatch.IsSuppressedMapObjectClickTarget(newTarget))
            {
                return true;
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(MyCameraController), "set_TargetIsSCOnFly")]
    internal static class MapObjectClickCameraFlyTargetPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(bool value)
        {
            return !(value && MapObjectClickCameraSuppressionScopePatch.SuppressTargetIsSpacecraftOnFly);
        }
    }

    [HarmonyPatch(typeof(LabelObject), nameof(LabelObject.OnMouseDown))]
    internal static class MissionLabelCameraClickPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(LabelObject __instance)
        {
            Transform target = __instance != null ? __instance.transform : null;
            if (target == null)
            {
                return true;
            }

            return !MapObjectClickCameraSuppressionScopePatch.ShouldSuppressSingleClickTarget(target);
        }
    }

    [HarmonyPatch(typeof(MissionsLabelsMainUI), nameof(MissionsLabelsMainUI.Click))]
    internal static class MissionMainLabelCameraClickPatch
    {
        [HarmonyPrefix]
        private static void Prefix(MissionsLabelsMainUI __instance, ref Transform __state)
        {
            MissionInfo missionInfo = __instance != null ? __instance.MissionInfo : null;
            Spacecraft spacecraft = missionInfo?.spacecraftInfo2 as Spacecraft;
            __state = spacecraft != null ? spacecraft.transform : null;
            if (__state == null)
            {
                return;
            }

            if (!MapObjectClickCameraSuppressionScopePatch.ShouldSuppressSingleClickTarget(__state))
            {
                __state = null;
                return;
            }

            MapObjectClickCameraSuppressionScopePatch.PushSuppressedTarget(__state, suppressSpacecraftOnFly: true);
        }

        [HarmonyFinalizer]
        private static void Finalizer(Transform __state)
        {
            if (__state != null)
            {
                MapObjectClickCameraSuppressionScopePatch.PopSuppressedTarget(__state, suppressSpacecraftOnFly: true);
            }
        }
    }
}
