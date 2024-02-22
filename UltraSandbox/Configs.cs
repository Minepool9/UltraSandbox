using Configgy;
using UnityEngine;

namespace UltraSandbox
{
    internal abstract class Configs
    {
        // Keybind for the "Build" button
        [Configgable("Keybinds", "Build button")]
        internal static ConfigKeybind buildB = new ConfigKeybind(KeyCode.X);

        // Keybind for switching asset bundles
        [Configgable("Keybinds", "Switch asset bundle")]
        internal static ConfigKeybind switchB = new ConfigKeybind(KeyCode.N);

        // Keybind for scrolling through the object list
        [Configgable("Keybinds", "Scroll through object list")]
        internal static ConfigKeybind scrollB = new ConfigKeybind(KeyCode.C);

        // Keybind for opening/closing the menu
        [Configgable("Keybinds", "Scroll through object list")]
        internal static ConfigKeybind menuB = new ConfigKeybind(KeyCode.M);

        // Keybind for scrolling through the object list
        [Configgable("Debug", "Use ULTRAKILL's debug arm rotation")]
        internal static ConfigToggle rotationSetting = new ConfigToggle(false);
    }
}
