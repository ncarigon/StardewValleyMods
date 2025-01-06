using System.Collections.Generic;
using System.Linq;
using Force.DeepCloner;
using HarmonyLib;
using StardewValley;
using StardewValley.GameData.Machines;
using SObject = StardewValley.Object;
using SMachineData = StardewValley.GameData.Machines.MachineData;

namespace CopperStill.ModPatches {
    internal static class SDVPatches {
        public static void Register() {
            ModEntry.Instance?.ModHarmony?.Patch(
                original: AccessTools.Method(typeof(SObject), "PlaceInMachine"),
                prefix: new HarmonyMethod(typeof(SDVPatches), nameof(Prefix_Object_PlaceInMachine))
            );
        }

        private static void Prefix_Object_PlaceInMachine(
            SObject __instance,
            ref SMachineData machineData, Item inputItem, Farmer who
        ) {
            if (MachineDataUtility.TryGetMachineOutputRule(__instance, machineData, MachineOutputTrigger.ItemPlacedInMachine, inputItem, who, __instance.Location, out var outputRule, out _, out _, out _)) {
                var cd = outputRule.OutputItem.FirstOrDefault()?.CustomData;
                if (cd?.Count > 0) {
                    var aci = new List<MachineItemAdditionalConsumedItems>();
                    for (var i = 1; true; i++) {
                        if (!cd.TryGetValue($"{ModEntry.Instance?.ModHarmony?.Id}CP_AdditionalConsumedItems.ItemId.{i}", out var itemId)) {
                            break;
                        }
                        aci.Add(new MachineItemAdditionalConsumedItems() {
                            ItemId = itemId,
                            RequiredCount = cd.TryGetValue($"{ModEntry.Instance?.ModHarmony?.Id}CP_AdditionalConsumedItems.RequiredCount.{i}", out var ic)
                                && int.TryParse(ic, out var c) && c > 0
                                ? c : 1,
                            InvalidCountMessage = cd.TryGetValue($"{ModEntry.Instance?.ModHarmony?.Id}CP_AdditionalConsumedItems.InvalidCountMessage.{i}", out var m)
                                && !string.IsNullOrWhiteSpace(m)
                                ? m : $"Failed to parse CustomData {ModEntry.Instance?.ModHarmony?.Id}CP_AdditionalConsumedItems.InvalidCountMessage.{i}"
                        });
                    }
                    if (aci.Count > 0) {
                        machineData = machineData.DeepClone();
                        machineData.AdditionalConsumedItems.AddRange(aci);
                    }
                }
            }
        }
    }
}