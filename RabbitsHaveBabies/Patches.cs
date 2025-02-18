using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Events;

namespace RabbitsHaveBabies {
    internal static class Patches {
        public static void Patch() {
            if (ModEntry.Instance?.ModManifest?.UniqueID is not null) {
                new Harmony(ModEntry.Instance.ModManifest.UniqueID).Patch(
                    original: AccessTools.Method(typeof(QuestionEvent), nameof(QuestionEvent.setUp)),
                    postfix: new HarmonyMethod(typeof(Patches), nameof(Postfix_QuestionEvent_setUp))
                );
            }
        }

        private static void Postfix_QuestionEvent_setUp(
            QuestionEvent __instance, int ___whichQuestion, ref AnimalHouse ___animalHouse,
            ref bool __result
        ) {
            if (___whichQuestion == 2 && __result) {
                FarmAnimal? a = null;
                AnimalHouse? h = null;
                Utility.ForEachBuilding((Building b) => {
                    if ((b.owner.Value == Game1.player.UniqueMultiplayerID || !Game1.IsMultiplayer)
                        && b.AllowsAnimalPregnancy()
                        && b.GetIndoors() is AnimalHouse animalHouse && !animalHouse.isFull()
                    ) {
                        var rabbits = animalHouse.animalsThatLiveHere.Select(i => Utility.getAnimal(i)).Where(i =>
                            i.type.Value == "Rabbit"
                            && !i.isBaby()
                            && i.allowReproduction.Value
                            && i.CanHavePregnancy()
                        ).ToArray();
                        if (rabbits.Length > 1
                            && Game1.random.NextDouble() < rabbits.Length * 0.0055 * Config.Instance.ExtraChanceToReproduce
                        ) {
                            a = rabbits[Game1.random.Next(0, rabbits.Length)];
                            h = animalHouse;
                            return false;
                        }
                    }
                    return true;
                });
                if (h != null) {
                    ___animalHouse = h;
                }
                if (a != null) {
                    Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Events:AnimalBirth", a.displayName, a.shortDisplayType()));
                    Game1.messagePause = true;
                    __instance.animal = a;
                    __result = false;
                }
            }
        }
    }
}
