using Microsoft.Xna.Framework;
using Netcode;
using StardewValley;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using System.Xml.Serialization;
using SObject = StardewValley.Object;

namespace GardenPotOptions {
    public static class GameLocationExtensions {
        public static bool TryUsePot(this GameLocation? location, int x, int y, Item? pot, Farmer? who = null) {
            var tile = new Vector2(x / 64, y / 64);
            if (pot.TryPlaceContent(location, tile, who)) { // empty tile, saved terrain
                return true;
            }
            if (location?.terrainFeatures?.TryGetValue(tile, out var terrainFeature) == true) { // terrain tile
                if (pot.TryPlaceContent(terrainFeature, who)) { // dirt tile, saved dirt
                    return true;
                }
                if (terrainFeature.TryToPot(pot, who)) { // full tile, empty pot
                    return true;
                }
            }
            if (location?.objects?.TryGetValue(tile, out var obj) == true && obj is IndoorPot placedPot) { // pot tile
                if (placedPot.TryPickup()) { // pot with saved object
                    return true;
                }
            }
            if (pot.TryPlace(location, tile, who)) { // empty tile, saved pot
                return true;
            }
            return false;
        }
    }

    public static class IndoorPotExtensions {
        public static bool IsFilled(this IndoorPot? pot) {
            return pot?.hoeDirt?.Value?.crop is not null
                || pot?.hoeDirt?.Value?.fertilizer?.Value is not null
                || pot?.bush?.Value is not null
                || pot?.heldObject?.Value is not null;
        }

        public static string? GetContentName(this IndoorPot? pot) {
            if (pot.IsFilled()) {
                return pot?.hoeDirt?.Value?.GetPlantedName() ?? pot?.bush?.Value?.GetPlantedName() ?? pot?.heldObject?.Value?.DisplayName ?? "";
            }
            return null;
        }

        public static bool TryPickup(this IndoorPot? placedPot) {
            if (placedPot.IsFilled()) { // pot with something saved
                // save content to pot
                var pickupPot = ItemRegistry.Create("(BC)62");
                if (pickupPot.TrySetItem(placedPot)) {
                    var location = placedPot!.Location;
                    var tile = placedPot.TileLocation;

                    // do normal object cleanup
                    placedPot?.performRemoveAction();

                    // remove object from location, prevent base function from doing much else
                    location?.objects?.Remove(tile);

                    // drop pot item with saved content
                    Game1.createItemDebris(pickupPot, tile * 64f, 2, location);

                    return true;
                }
            }
            return false;
        }
    }

    public static class TerrainFeatureExtensions {
        private static SObject? GetTreeSeed(this TerrainFeature? terrainFeature) {
            if (terrainFeature is Tree tree) {
                return new SObject(Tree.GetWildTreeSeedLookup().FirstOrDefault(d => d.Value?.Any(v => v?.Equals(tree?.treeType?.Value) == true) == true).Key?.Replace("(O)", ""), 1);
            }
            return null;
        }

        private static SObject? GetFruitTreeSapling(this TerrainFeature? terrainFeature) {
            if (terrainFeature is FruitTree fruitTree) {
                return new SObject(fruitTree?.treeId?.Value, 1);
            }
            return null;
        }

        public static SObject? GetBushSapling(this TerrainFeature? terrainFeature) {
            if (terrainFeature is Bush bush && bush?.size?.Value == 3) {
                return new SObject("251", 1);
            }
            return null;
        }

        public static SObject? GetPlantedObject(this TerrainFeature? terrainFeature) {
            return terrainFeature.GetTreeSeed() ?? terrainFeature.GetFruitTreeSapling() ?? terrainFeature.GetBushSapling();
        }

        private static string? GetDirtContent(this TerrainFeature? terrainFeature) {
            if (terrainFeature is HoeDirt dirt && dirt is not null) {
                if (dirt.crop?.indexOfHarvest?.Value is not null) {
                    return new SObject(dirt.crop.indexOfHarvest.Value, 1).DisplayName;
                } else if (dirt.fertilizer?.Value is not null) {
                    return new SObject(dirt.fertilizer.Value.Replace("(O)", ""), 1).DisplayName;
                }
            }
            return null;
        }

        public static string? GetPlantedName(this TerrainFeature? terrainFeature) {
            return terrainFeature.GetPlantedObject()?.DisplayName ?? terrainFeature.GetDirtContent();
        }

        public static bool TryToPot(this TerrainFeature? terrainFeature, Item? pot = null, Farmer? who = null) {
            if (ModEntry.Instance?.ModConfig?.AllowTransplant == true // can transplant
                && terrainFeature.GetPlantedName() is not null // terrain contains something
                && (pot ?? (who ?? Game1.player)?.ActiveItem).IsGardenPot(out var contents) // holding pot
                && string.IsNullOrWhiteSpace(contents) // which is empty
            ) {
                string? deniedMsg = null;
                if (terrainFeature is Tree existingTree && existingTree?.growthStage?.Value > (ModEntry.Instance?.ModConfig?.TreeTransplantMax ?? -1)) {
                    //deniedMsg = ModEntry.Instance?.Helper.Translation.Get($"{ModEntry.Instance?.ModHarmony?.Id}/tree_too_large");
                    deniedMsg = "This tree too large to transplant.";
                } else if (terrainFeature is FruitTree existingFruitTree && existingFruitTree?.growthStage?.Value > (ModEntry.Instance?.ModConfig?.FruitTreeTransplantMax ?? -1)) {
                    //deniedMsg = ModEntry.Instance?.Helper.Translation.Get($"{ModEntry.Instance?.ModHarmony?.Id}/fruittree_too_large");
                    deniedMsg = "This fruit tree is too large to transplant.";
                } else if (terrainFeature is Bush existingBush && existingBush?.size?.Value != 3) { // must be a tea sapling (or modded?)
                    //deniedMsg = ModEntry.Instance?.Helper.Translation.Get($"{ModEntry.Instance?.ModHarmony?.Id}/bush_cant_transplant");
                    deniedMsg = "This bush can not be transplanted.";
                } else {
                    var location = terrainFeature!.Location;
                    var tile = terrainFeature.Tile;

                    // save terrain to pot
                    var pickupPot = ItemRegistry.Create("(BC)62");
                    if (pickupPot.TrySetTerrainFeature(terrainFeature)) {
                        // remove terrain feature
                        location?.terrainFeatures?.Remove(tile);

                        if (terrainFeature is HoeDirt) {
                            // add empty dirt
                            location?.terrainFeatures?.Add(tile, new HoeDirt(0, location));
                        }

                        // remove empty garden pot from inventory
                        (who ?? Game1.player)?.reduceActiveItemByOne();

                        // drop full garden pot
                        Game1.createItemDebris(pickupPot, tile * 64f, 2, location);

                        return true;
                    }
                }
                if (deniedMsg is not null) {
                    Game1.showRedMessage(deniedMsg);
                }
            }
            return false;
        }
    }

    public static class ItemExtensions {
        private const string PottedObjectKey = "NCarigon.GardenPotOptions.PottedObject";
        private static readonly XmlSerializer ItemSerializer = new(typeof(Item));
        private static readonly XmlSerializer TerrainFeatureSerializer = new(typeof(TerrainFeature));

        public static bool TryGetItem<T>(this Item? item, out T? save) where T : Item {
            save = null;
            if (item?.modData?.TryGetValue(PottedObjectKey, out var xml) is not null
                && !string.IsNullOrWhiteSpace(xml)
            ) {
                try {
                    using var r = new StringReader(xml);
                    save = ItemSerializer.Deserialize(r) as T;
                } catch { }
            }
            return save is not null;
        }

        public static bool TrySetItem(this Item? item, Item? save) {
            if (item is not null) {
                if (save is not null) {
                    try {
                        using var w = new StringWriter();
                        ItemSerializer.Serialize(w, save);
                        var xml = w.ToString();
                        if (!string.IsNullOrWhiteSpace(xml)) {
                            item.modData[PottedObjectKey] = xml;
                            return true;
                        }
                    } catch { }
                } else {
                    item.modData.Remove(PottedObjectKey);
                    return true;
                }
            }
            return false;
        }

        public static bool TryGetTerrainFeature<T>(this Item? item, out T? save) where T : TerrainFeature {
            save = null;
            if (item?.modData?.TryGetValue(PottedObjectKey, out var xml) is not null
                && !string.IsNullOrWhiteSpace(xml)
            ) {
                try {
                    using var r = new StringReader(xml);
                    save = TerrainFeatureSerializer.Deserialize(r) as T;
                } catch { }
            }
            return save is not null;
        }

        public static bool TrySetTerrainFeature<T>(this Item? item, T? save) where T : TerrainFeature {
            if (item is not null) {
                if (save is not null) {
                    try {
                        using var w = new StringWriter();
                        TerrainFeatureSerializer.Serialize(w, save);
                        var xml = w.ToString();
                        if (!string.IsNullOrWhiteSpace(xml)) {
                            item.modData[PottedObjectKey] = xml;
                            return true;
                        }
                    } catch { }
                } else {
                    item.modData.Remove(PottedObjectKey);
                    return true;
                }
            }
            return false;
        }

        public static bool TryGetContentType(this Item? item, out string? type) {
            type = "";
            if (item is not null) {
                if (item.TryGetItem<IndoorPot>(out var savedPot)) {
                    type += savedPot.GetContentName() ?? "";
                } else if (item.TryGetTerrainFeature<TerrainFeature>(out var terrainFeature)) {
                    type += terrainFeature.GetPlantedName() ?? "";
                }
            }
            return !string.IsNullOrWhiteSpace(type);
        }

        public static bool TryPlaceContent(this Item? pot, GameLocation? location, Vector2 tile, Farmer? who = null) {
            if (ModEntry.Instance?.ModConfig?.AllowTransplant == true // can transplant
                && location?.isTilePlaceable(tile) == true // tile is empty
                && pot.TryGetTerrainFeature<TerrainFeature>(out var savedTerrain) // holding pot which contains terrain
                && savedTerrain.GetPlantedObject()?.canBePlacedHere(location, tile, showError: true) == true
            ) {
                // correct new positioning
                savedTerrain!.Location = location;
                savedTerrain.Tile = tile;

                if (savedTerrain is Bush savedBush) {
                    // ensures bush is drawn
                    savedBush.setUpSourceRect();
                }

                // add saved terrain
                location?.terrainFeatures?.Add(tile, savedTerrain);

                // remove from inventory
                (who ?? Game1.player)?.reduceActiveItemByOne();

                // drop new garden pot
                Game1.createItemDebris(ItemRegistry.Create("(BC)62"), tile * 64f, 2, location);

                return true;
            }
            return false;
        }

        public static bool TryPlaceContent(this Item? pot, TerrainFeature? terrainFeature, Farmer? who = null) {
            if (ModEntry.Instance?.ModConfig?.AllowTransplant == true // can transplant
                && terrainFeature is HoeDirt existingDirt && existingDirt is not null // terrain exists
                && pot.TryGetTerrainFeature<HoeDirt>(out var savedDirt) // holding pot which contains dirt
            ) {
                var location = existingDirt.Location;
                var tile = existingDirt.Tile;

                string? deniedMsg = null;
                if (existingDirt?.crop is not null && savedDirt?.crop is not null) {
                    //deniedMsg = ModEntry.Instance?.Helper.Translation.Get($"{ModEntry.Instance?.ModHarmony?.Id}/existing_crop");
                    deniedMsg = "Existing crop in both dirt and garden pot.";
                } else if (existingDirt?.fertilizer?.Value is not null && savedDirt?.fertilizer?.Value is not null) {
                    //deniedMsg = ModEntry.Instance?.Helper.Translation.Get($"{ModEntry.Instance?.ModHarmony?.Id}/existing_fertilizer");
                    deniedMsg = "Existing fertilizer in both dirt and garden pot.";
                } else if (savedDirt?.crop is not null && location?.CanPlantSeedsHere(savedDirt.crop.netSeedIndex.Value, (int)tile.X, (int)tile.Y, false, out deniedMsg) != true) {
                    // msg already set
                } else {
                    // correct new positioning
                    savedDirt!.Location = location;
                    savedDirt.Tile = tile;

                    // keep whatever content exists
                    savedDirt.fertilizer.Value ??= existingDirt?.fertilizer?.Value;
                    savedDirt!.crop ??= existingDirt?.crop;
                    if (savedDirt.crop is not null) {
                        savedDirt.crop.Dirt = savedDirt;
                        savedDirt.crop.currentLocation = null;
                        savedDirt.crop.tilePosition = Vector2.Zero;
                    }

                    // remove existing terrain
                    location?.terrainFeatures?.Remove(tile);

                    // add saved terrain
                    location?.terrainFeatures?.Add(tile, savedDirt);

                    // remove from inventory
                    who?.reduceActiveItemByOne();

                    // drop new garden pot
                    Game1.createItemDebris(ItemRegistry.Create("(BC)62"), tile * 64f, 2, location);

                    return true;
                }
                if (deniedMsg is not null) {
                    Game1.showRedMessage(deniedMsg);
                }
            }
            return false;
        }

        public static bool TryPlace(this Item? item, GameLocation? location, Vector2 tile, Farmer? who = null) {
            if (ModEntry.Instance?.ModConfig?.KeepContents == true
                && item.TryGetItem<IndoorPot>(out var pot) && pot is not null
                && location?.isTilePlaceable(tile) == true
            ) {
                // update dirt details
                pot!.Location = location;
                pot.TileLocation = tile;
                if (pot.hoeDirt?.Value is not null) {
                    pot.hoeDirt.Value.Pot = pot;
                    pot.hoeDirt.Value.Location = location;
                    pot.hoeDirt.Value.Tile = tile;
                }
                if (pot?.bush?.Value is not null) {
                    pot.bush.Value.inPot.Value = true;
                    pot.bush.Value.Location = location;
                    pot.bush.Value.Tile = tile;
                }
                if (pot?.heldObject?.Value is not null) {
                    pot.heldObject.Value.Location = location;
                    pot.heldObject.Value.TileLocation = tile;
                }

                // add saved pot
                location?.objects?.Add(tile, pot);

                // remove from inventory
                who?.reduceActiveItemByOne();

                return true;
            }
            return false;
        }

        public static bool IsGardenPot(this Item? item, out string? contents) {
            contents = null;
            if (item?.QualifiedItemId?.Equals("(BC)62") == true) {
                if (item?.TryGetContentType(out var c) == true) {
                    contents = c;
                }
                return true;
            }
            return false;
        }
    }
}
