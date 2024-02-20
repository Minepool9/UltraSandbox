using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Linq; // Added for LINQ
using Configgy;

public static class ExperimentalArmRotationPatch
{
    [HarmonyPatch(typeof(Sandbox.Arm.MoveMode), nameof(Sandbox.Arm.MoveMode.Update))]
    public class MoveMode_Update_Patch
    {
        static void Postfix(Sandbox.Arm.MoveMode __instance)
        {
            // Ensure that ExperimentalArmRotation is always enabled
            ULTRAKILL.Cheats.ExperimentalArmRotation.Enabled = true;
        }
    }
}
