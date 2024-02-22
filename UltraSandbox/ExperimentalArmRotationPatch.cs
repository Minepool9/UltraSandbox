using HarmonyLib;
using UltraSandbox;

public class ExperimentalArmRotationPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Sandbox.Arm.MoveMode), nameof(Sandbox.Arm.MoveMode.OnEnable))]
    static void Patch()
    {
        // Ensure that ExperimentalArmRotation is always enabled
        if (Configs.rotationSetting.Value) ULTRAKILL.Cheats.ExperimentalArmRotation.Enabled = true;
        else ULTRAKILL.Cheats.ExperimentalArmRotation.Enabled = false;
    }
}
