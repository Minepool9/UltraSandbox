using Configgy;
using UnityEngine;

namespace UltraSandbox
{
    internal abstract class Configs
    {
        // Keybind for the "Build" button
        [Configgable("Keybinds", "Build button")]
        internal static ConfigInputField<string> buildB = new ConfigInputField<string>("X");

        // Keybind for switching asset bundles
        [Configgable("Keybinds", "Switch asset bundle")]
        internal static ConfigInputField<string> switchB = new ConfigInputField<string>("N");

        // Keybind for scrolling through the object list
        [Configgable("Keybinds", "Scroll through object list")]
        internal static ConfigInputField<string> scrollB = new ConfigInputField<string>("C");

        // Keybind for opening/closing the menu
        [Configgable("Keybinds", "Scroll through object list")]
        internal static ConfigInputField<string> menuB = new ConfigInputField<string>("M");

        // Keybind for scrolling through the object list
        [Configgable("Debug", "Use ULTRAKILL's debug arm rotation")]
        internal static ConfigToggle rotationSetting = new ConfigToggle(false);
    }
}
