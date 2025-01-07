using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewValley.ItemTypeDefinitions;
using SObject = StardewValley.Object;

namespace BetterHoneyMead.Patches {
    internal static class MachineDataPatches {
        public static void Register() {
            ModEntry.Instance?.ModHarmony?.Patch(
                original: AccessTools.Method(typeof(ObjectDataDefinition), "CreateFlavoredHoney"),
                postfix: new HarmonyMethod(typeof(MachineDataPatches), nameof(Postfix_ObjectDataDefinition_CreateFlavoredHoney))
            );
        }

        public static void Postfix_ObjectDataDefinition_CreateFlavoredHoney(
            ref SObject __result,
            SObject? ingredient
        ) {
            if (__result is not null && __result.GetPreservedItemId() != null
                && ColoredObject.TrySetColor(__result, TailoringMenu.GetDyeColor(ingredient) ?? Color.Yellow, out var co)
            ) {
                __result = co;
            }
        }
    }
}
