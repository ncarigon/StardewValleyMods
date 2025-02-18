using HarmonyLib;
using StardewModdingAPI;
using StardewValley.Locations;
using System.Reflection.Emit;

namespace BundlesOpenSooner {
    internal sealed class ModEntry : Mod {
        public override void Entry(IModHelper helper) {
			new Harmony(helper.ModContent.ModID).Patch(
                original: AccessTools.Method(typeof(CommunityCenter), "shouldNoteAppearInArea"),
                transpiler: new HarmonyMethod(typeof(ModEntry), nameof(Transpile_CommunityCenter_shouldNoteAppearInArea))
            );
        }
		
		private static IEnumerable<CodeInstruction> Transpile_CommunityCenter_shouldNoteAppearInArea(IEnumerable<CodeInstruction> instructions) {
            var found = false;
            foreach (var instruction in instructions) {
                if (!found && instruction.opcode == OpCodes.Call && instruction.operand?.ToString()?.Contains("numberOfCompleteBundles()") == true) {
                    found = true;
                } else if (found &&
                    (instruction.opcode == OpCodes.Ldc_I4_0 // including identical opcode to ensure consistent behavior
                    || instruction.opcode == OpCodes.Ldc_I4_1
                    || instruction.opcode == OpCodes.Ldc_I4_2
                    || instruction.opcode == OpCodes.Ldc_I4_3
                )) {
                    found = false;
                    instruction.opcode = OpCodes.Ldc_I4_0;
                }
                yield return instruction;
            }
        }
    }
}
