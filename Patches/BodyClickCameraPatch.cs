#nullable disable
using System;
using System.Collections.Generic;
using CameraControl;
using Game.Info;
using HarmonyLib;
using UnityEngine;

namespace SolarExpanseUXTweaks.Patches
{
    [HarmonyPatch(typeof(InfoBase), nameof(InfoBase.MyOnMouseUpAsButton2))]
    internal static class BodyClickCameraSuppressionScopePatch
    {
        [ThreadStatic]
        private static Stack<Transform> suppressedTargets;

        internal static bool IsSuppressedBodyClickTarget(Transform target)
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

        [HarmonyPrefix]
        private static void Prefix(InfoBase __instance, ref Transform __state)
        {
            ObjectInfo objectInfo = __instance as ObjectInfo;
            if (objectInfo == null)
            {
                return;
            }

            __state = objectInfo.transform;
            if (__state == null)
            {
                return;
            }

            if (suppressedTargets == null)
            {
                suppressedTargets = new Stack<Transform>();
            }

            suppressedTargets.Push(__state);
        }

        [HarmonyFinalizer]
        private static void Finalizer(Transform __state)
        {
            if (__state == null || suppressedTargets == null || suppressedTargets.Count == 0)
            {
                return;
            }

            if (suppressedTargets.Peek() == __state)
            {
                suppressedTargets.Pop();
                return;
            }

            Transform[] currentTargets = suppressedTargets.ToArray();
            suppressedTargets.Clear();

            bool removed = false;
            for (int i = currentTargets.Length - 1; i >= 0; i--)
            {
                Transform currentTarget = currentTargets[i];
                if (!removed && currentTarget == __state)
                {
                    removed = true;
                    continue;
                }

                suppressedTargets.Push(currentTarget);
            }
        }
    }

    [HarmonyPatch(typeof(MyCameraController), nameof(MyCameraController.ChangeTarget), typeof(Transform), typeof(bool))]
    internal static class BodyClickCameraChangeTargetPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(Transform newTarget)
        {
            if (!BodyClickCameraSuppressionScopePatch.IsSuppressedBodyClickTarget(newTarget))
            {
                return true;
            }

            return false;
        }
    }
}
