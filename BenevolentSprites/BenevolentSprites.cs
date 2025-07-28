using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using HarmonyLib;
using Steamworks;
using System.Reflection;
using System.Linq;
using HMLLibrary;
using System;
using System.IO;
using System.Reflection.Emit;
using System.Globalization;
using I2.Loc;
using RaftModLoader;
using ALib;
using System.Runtime.Serialization;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using static BenevolentSprites.BenevolentSprites;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace BenevolentSprites
{
    public class BenevolentSprites : Mod
    {
        static Dictionary<string, Item_Base> itemLookup = new Dictionary<string, Item_Base>();
        public static Item_Base LookupItem(string name)
        {
            if (!itemLookup.TryGetValue(name, out var item) || !item)
                item = itemLookup[name] = ItemManager.GetItemByName(name);
            return item;
        }
        static BenevolentSprites self = null;
        public static List<HelperSprite> sprites = new List<HelperSprite>();
        static MethodInfo _getRecipes = typeof(CookingTable).GetMethod("GetRecipeFromIndex",~BindingFlags.Default);
        public static SO_CookingTable_Recipe GetRecipeFromIndex(int ind) => (SO_CookingTable_Recipe)_getRecipes.Invoke(System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(CookingTable)),new object[] { ind });
        static GameObject firePrefab;
        static Transform prefabHolder;
        public static LanguageSourceData language;
        public static List<Object> createdObjects = new List<Object>();
        public static List<AI_NetworkBehaviour_Domestic_Resource> animals = new List<AI_NetworkBehaviour_Domestic_Resource>();
        public static List<PickupItem_Networked> eggs = new List<PickupItem_Networked>();
        public static List<BeeHive> beehives = new List<BeeHive>();
        public static List<CookingTable> tables = new List<CookingTable>();
        public static List<Block_CookingStand> stands = new List<Block_CookingStand>();
        public static List<CookingTable_Recipe_UI> recipes = new List<CookingTable_Recipe_UI>();
        public static List<Seagull> seagulls = new List<Seagull>();
        public static List<Block_Foundation_ItemNet> itemnets = new List<Block_Foundation_ItemNet>();
        public static List<TankAccess> tanks = new List<TankAccess>();
        public static HashSet<uint> MissingIndecies = new HashSet<uint>();
        public static FieldInfo _activeFishingNets = null;
        public static IEnumerable<MonoBehaviour> ActiveFishingNets() => (IEnumerable<MonoBehaviour>)_activeFishingNets?.GetValue(null);
        public static Type fishingnet;
        public static Type arashiObjectEnabler;
        public static List<MonoBehaviour_ID> dredgers = new List<MonoBehaviour_ID>();
        public static Type dredger_basket;
        public static Type dredger_network;
        Harmony harmony;
        public static List<Message_InitiateConnection> waiting = new List<Message_InitiateConnection>();
        public static bool gameLoaded = false;
        public static Item_Base repellentItem;
        public static Dictionary<string, Item_Base> spriteItems = new Dictionary<string, Item_Base>();
        public static Dictionary<string, ConstructorInfo> spriteTypes = new Dictionary<string, ConstructorInfo>();
        static int keepItem;
        public static int KeepItem => ExtraSettingsAPI_Loaded ? keepItem : 0;
        static bool ec_garden;
        public static bool EC_Garden => ExtraSettingsAPI_Loaded ? ec_garden : false;
        static bool ec_animal;
        public static bool EC_Animal => ExtraSettingsAPI_Loaded ? ec_animal : false;
        static bool ec_animal2;
        public static bool EC_Animal2 => ExtraSettingsAPI_Loaded ? ec_animal2 : false;
        static bool ec_animal3;
        public static bool EC_Animal3 => ExtraSettingsAPI_Loaded ? ec_animal3 : false;
        static bool ec_fire;
        public static bool EC_Fire => ExtraSettingsAPI_Loaded ? ec_fire : false;
        static bool ec_fire2;
        public static bool EC_Fire2 => ExtraSettingsAPI_Loaded ? ec_fire2 : false;
        static bool ec_clean;
        public static bool EC_Clean => ExtraSettingsAPI_Loaded ? ec_clean : false;
        static float spriteRange;
        public static float SpriteRange => ExtraSettingsAPI_Loaded ? spriteRange : 0.5f;
        static float gardenRange;
        public static float GardenRange => ExtraSettingsAPI_Loaded ? gardenRange : 1;
        static float animalRange;
        public static float AnimalRange => ExtraSettingsAPI_Loaded ? animalRange : 1;
        static float fireRange;
        public static float FireRange => ExtraSettingsAPI_Loaded ? fireRange : 1;
        static float fire2Range;
        public static float Fire2Range => ExtraSettingsAPI_Loaded ? fire2Range : 1;
        static float cleanerRange;
        public static float CleanerRange => ExtraSettingsAPI_Loaded ? cleanerRange : 1;
        static float mechanicRange;
        public static float MechanicRange => ExtraSettingsAPI_Loaded ? mechanicRange : 1;
        static float repelRange;
        public static float RepelRange => ExtraSettingsAPI_Loaded ? repelRange : 2.25f;
        static int lightMode;
        public static int LightMode => ExtraSettingsAPI_Loaded ? lightMode : 2;
        public static bool PlaySounds = true;
        static bool ExtraSettingsAPI_Loaded = false;
        public const string dataKey = "SpriteData";
        public static GameObject FirePrefab
        {
            get
            {
                if (firePrefab == null)
                {
                    firePrefab = new GameObject("Helper Sprite");
                    firePrefab.transform.SetParent(prefabHolder,false);
                    if (!RAPI.IsDedicatedServer())
                    {
                        firePrefab.transform.localScale = Vector3.one / 2;
                        var r = firePrefab.AddComponent<SpriteRenderer>();
                        foreach (var p in Resources.FindObjectsOfTypeAll<ParticleSystemRenderer>())
                            if (p.name == "Fire" && p.transform.parent?.name == "Fire_v2")
                            {
                                var obj = Instantiate(p.gameObject, firePrefab.transform, false);
                                obj.name = "Particles";
                                obj.SetActive(true);
                                var m = obj.GetComponent<ParticleSystem>().main;
                                m.duration *= 2;
                                m.startDelayMultiplier /= 3;
                                //m.simulationSpace = ParticleSystemSimulationSpace.World;
                                break;
                            }
                        r.glow = firePrefab.AddComponent<Light>();
                        r.glow.intensity = 1;
                        r.glow.range = 2;
                        r.glow.shadowResolution = UnityEngine.Rendering.LightShadowResolution.FromQualitySettings;
                        r.glow.shadowNearPlane = 0.1f;
                    }
                }
                return firePrefab;
            }
        }
        static Texture2D baseSpriteTex;
        static Dictionary<Color, Sprite> spriteTexs = new Dictionary<Color, Sprite>();
        public static Sprite GetSpriteTexture(Color color)
        {
            if (!baseSpriteTex)
            {
                baseSpriteTex = new Texture2D(0, 0);
                baseSpriteTex.LoadImage(self.GetEmbeddedFileBytes("sprite.png"));
                createdObjects.Add(baseSpriteTex);
            }
            if (!spriteTexs.TryGetValue(color, out var sprite))
            {
                sprite = Sprite.Create(new Texture2D(baseSpriteTex.width, baseSpriteTex.height, baseSpriteTex.format, false), new Rect(0, 0, baseSpriteTex.width, baseSpriteTex.height), new Vector2(0, 0), 1);
                var texture = sprite.texture;
                createdObjects.Add(sprite);
                createdObjects.Add(texture);
                Func<float, float, float, float> edit = (v, s, b) => 1 - (1 - v * s) * (1 - b);
                for (int x = 0; x < texture.width; x++)
                    for (int y = 0; y < texture.height; y++)
                    {
                        var c = baseSpriteTex.GetPixel(x, y);
                        var b = Math.Max(c.r * 2 - 1, 0);
                        var s = Math.Min(c.r * 2, 1);

                        if (c.a == 0)
                            texture.SetPixel(x, y, Color.clear);
                        else
                            texture.SetPixel(x, y, new Color(edit(color.r, s, b), edit(color.g, s, b), edit(color.b, s, b), c.a));
                    }
                texture.Apply();
                spriteTexs[color] = sprite;
            }
            return sprite;
        }
        public void Start()
        {
            AccessorAttribute.ApplyAll();
            self = this;
            if (SceneManager.GetActiveScene().name != Raft_Network.MenuSceneName)
            {
                modlistEntry.modinfo.unloadBtn.GetComponent<Button>().onClick.Invoke();
                throw new ModLoadException("Mod must be loaded on the main menu");
            }
            var lang = new Dictionary<string, TermData>();
            LocalizationManager.Sources.Add(language = new LanguageSourceData()
            {
                mDictionary = lang,
                mLanguages = new List<LanguageData> { new LanguageData() { Code = "en", Name = "English" } }
            });
            void repelTexture(Texture2D texture)
            {
                for (int x = 0; x < texture.width; x++)
                    for (int y = 0; y < texture.height; y++)
                    {
                        var old = texture.GetPixel(x, y);
                        texture.SetPixel(x, y, new Color(old.r, old.b, old.g, old.a));
                    }
                texture.Apply();
            }
            var originalItem = LookupItem("Placeable_Lantern_Metal");
            repellentItem = originalItem.Clone(8764, "Sprite_Repellent");
            repellentItem.name = repellentItem.UniqueName;
            repellentItem.settings_Inventory.LocalizationTerm = "Item/" + repellentItem.UniqueName;
            lang.Add(repellentItem.settings_Inventory.LocalizationTerm, new TermData() { Languages = new[] { "Sprite Repellent@Deters sprites within a small radius" } });
            repellentItem.settings_recipe = new ItemInstance_Recipe(CraftingCategory.Other, false, false, "Sprite Repellent", 0);
            repellentItem.settings_recipe.SetRecipe(new CostMultiple[]
                {
                new CostMultiple(new Item_Base[] { LookupItem("Scrap") }, 2 ),
                new CostMultiple(new Item_Base[] { LookupItem("Glass") },1)
                }, 2);
            repellentItem.settings_recipe.LearnedViaBlueprint = true;
            prefabHolder = new GameObject("prefabHolder").transform;
            prefabHolder.gameObject.SetActive(false);
            DontDestroyOnLoad(prefabHolder.gameObject);
            createdObjects.Add(prefabHolder.gameObject);

            var prefabs = Traverse.Create(repellentItem.settings_buildable).Field<Block[]>("blockPrefabs");
            var newPrefabs = new Block[prefabs.Value.Length];
            for (int i = 0; i < prefabs.Value.Length; i++)
            {
                newPrefabs[i] = Instantiate(prefabs.Value[i], prefabHolder, false);
                newPrefabs[i].gameObject.AddComponent<SpriteRepellent>();
                DestroyImmediate(newPrefabs[i].GetComponent<LightSingularityExternal>());
                if (!RAPI.IsDedicatedServer())
                {
                    var lightObj = new GameObject("lightEmitter");
                    lightObj.transform.SetParent(newPrefabs[i].transform, false);
                    lightObj.transform.localPosition = Vector3.up * 0.1f;
                    var light = lightObj.AddComponent<Light>();
                    light.color = new Color(1, 0, 1, 0.6f);
                    light.intensity = 3;
                    light.range = 1;
                }
                newPrefabs[i].itemToReturnOnDestroy = repellentItem;
                newPrefabs[i].buildableItem = repellentItem;
                if (!RAPI.IsDedicatedServer())
                    foreach (var rend in newPrefabs[i].GetComponentsInChildren<MeshRenderer>())
                    {
                        rend.material = new Material(rend.material);
                        var tex = ((Texture2D)rend.material.GetTexture(298)).GetReadable();
                        rend.material.SetTexture(298, tex);
                        repelTexture(tex);
                    }
                newPrefabs[i].name = repellentItem.UniqueName;
            }
            prefabs.Value = newPrefabs;
            Traverse.Create(repellentItem.settings_Inventory).Field("stackSize").SetValue(10);
            Traverse.Create(repellentItem.settings_recipe).Field("subCategory").SetValue(null);
            if (!RAPI.IsDedicatedServer())
            {
                repellentItem.settings_Inventory.Sprite = originalItem.settings_Inventory.Sprite.GetReadable();
                repelTexture(repellentItem.settings_Inventory.Sprite.texture);
            }
            foreach (var type in Resources.FindObjectsOfTypeAll<SO_BlockQuadType>())
                if (type.AcceptsBlock(originalItem))
                    Traverse.Create(type).Field("acceptableBlockTypes").GetValue<List<Item_Base>>().Add(repellentItem); 
            foreach (var type in Resources.FindObjectsOfTypeAll<SO_BlockCollisionMask>())
                if (type.IgnoresBlock(originalItem))
                    Traverse.Create(type).Field("blockTypesToIgnore").GetValue<List<Item_Base>>().Add(repellentItem);

            var baseTex = new Texture2D(0,0);
            if (!RAPI.IsDedicatedServer())
                baseTex.LoadImage(GetEmbeddedFileBytes("sprite.png"));
            createdObjects.Add(baseTex);
            Sprite spriteTexture(Color color)
            {
                if (RAPI.IsDedicatedServer())
                    return null;
                var sprite = Sprite.Create(new Texture2D(baseTex.width, baseTex.height, baseTex.format, false), new Rect(0, 0, baseTex.width, baseTex.height), new Vector2(0, 0), 1);
                var texture = sprite.texture;
                createdObjects.Add(sprite);
                createdObjects.Add(texture);
                Func<float, float, float, float> edit = (v, s, b) => 1 - (1 - v * s) * (1 - b);
                for (int x = 0; x < texture.width; x++)
                    for (int y = 0; y < texture.height; y++)
                    {
                        var c = baseTex.GetPixel(x, y);
                        var b = Math.Max(c.r * 2 - 1, 0);
                        var s = Math.Min(c.r * 2, 1);

                        if (c.a == 0)
                            texture.SetPixel(x, y, Color.clear);
                        else
                            texture.SetPixel(x, y, new Color(edit(color.r, s, b), edit(color.g, s, b), edit(color.b, s, b), c.a));
                    }
                texture.Apply();
                return sprite;
            };

            originalItem = LookupItem("Plank");
            var newItem = originalItem.Clone(8765, "Garden_Sprite");
            newItem.settings_Inventory.LocalizationTerm = "Item/" + newItem.UniqueName;
            lang.Add(newItem.settings_Inventory.LocalizationTerm, new TermData() { Languages = new[] { "Garden Sprite@A little glowing friend.\nIt seems to take interest in nearby plants." } });
            Traverse.Create(newItem.settings_Inventory).Field("stackSize").SetValue(1);
            newItem.settings_Inventory.Sprite = spriteTexture(new Color(0.2f, 1, 0));
            Traverse.Create(newItem.settings_recipe).Field("blueprintItem").SetValue(repellentItem);
            spriteItems = new Dictionary<string, Item_Base>();
            spriteTypes = new Dictionary<string, ConstructorInfo>();
            spriteItems.Add(newItem.UniqueName, newItem);
            spriteTypes.Add(newItem.UniqueName, GetConstructor<GardenSprite>());

            newItem = spriteItems["Garden_Sprite"].Clone(8766, "Animal_Sprite");
            newItem.settings_Inventory.LocalizationTerm = "Item/" + newItem.UniqueName;
            lang.Add(newItem.settings_Inventory.LocalizationTerm, new TermData() { Languages = new[] { "Animal Sprite@A little glowing friend.\nIt seems to take interest in nearby animals." } });
            newItem.settings_Inventory.Sprite = spriteTexture(new Color(1, 0, 1));
            spriteItems.Add(newItem.UniqueName, newItem);
            spriteTypes.Add(newItem.UniqueName, GetConstructor<AnimalSprite>());

            newItem = spriteItems["Garden_Sprite"].Clone(8767, "Fire_Sprite");
            newItem.settings_Inventory.LocalizationTerm = "Item/" + newItem.UniqueName;
            lang.Add(newItem.settings_Inventory.LocalizationTerm, new TermData() { Languages = new[] { "Fire Sprite@A little glowing friend.\nIt seems to take interest in nearby grills and smelters." } });
            newItem.settings_Inventory.Sprite = spriteTexture(new Color(1, 0, 0));
            spriteItems.Add(newItem.UniqueName, newItem);
            spriteTypes.Add(newItem.UniqueName, GetConstructor<FireSprite>());

            newItem = spriteItems["Garden_Sprite"].Clone(8768, "Fire_Sprite_2");
            newItem.settings_Inventory.LocalizationTerm = "Item/" + newItem.UniqueName;
            lang.Add(newItem.settings_Inventory.LocalizationTerm, new TermData() { Languages = new[] { "Fire Sprite@A little glowing friend.\nIt seems to take interest in nearby cooking pots." } });
            newItem.settings_Inventory.Sprite = spriteTexture(new Color(0.5f, 0, 0));
            spriteItems.Add(newItem.UniqueName, newItem);
            spriteTypes.Add(newItem.UniqueName, GetConstructor<FireSprite2>());

            newItem = spriteItems["Garden_Sprite"].Clone(8769, "Cleaner_Sprite");
            newItem.settings_Inventory.LocalizationTerm = "Item/" + newItem.UniqueName;
            lang.Add(newItem.settings_Inventory.LocalizationTerm, new TermData() { Languages = new[] { "Cleaner Sprite@A little glowing friend.\nIt seems to become irritated by nearby mess." } });
            newItem.settings_Inventory.Sprite = spriteTexture(new Color(1, 1, 0));
            spriteItems.Add(newItem.UniqueName, newItem);
            spriteTypes.Add(newItem.UniqueName, GetConstructor<CleanerSprite>());

            newItem = spriteItems["Garden_Sprite"].Clone(8770, "Mechanic_Sprite");
            newItem.settings_Inventory.LocalizationTerm = "Item/" + newItem.UniqueName;
            lang.Add(newItem.settings_Inventory.LocalizationTerm, new TermData() { Languages = new[] { "Mechanic Sprite@A little glowing friend.\nIt seems to take interest in nearby refiners, engines and tanks." } });
            newItem.settings_Inventory.Sprite = spriteTexture(new Color(0, 0.5f, 1));
            spriteItems.Add(newItem.UniqueName, newItem);
            spriteTypes.Add(newItem.UniqueName, GetConstructor<MechanicSprite>());
            


            foreach (var item in spriteItems.Values)
                RAPI.RegisterItem(item);
            RAPI.RegisterItem(repellentItem);
            foreach (var rand in Resources.FindObjectsOfTypeAll<SO_RandomDropper>())
                if (rand.name.Contains("Dropper_FishingRod_Metal")) //csrun Debug.Log(Traverse.Create(Traverse.Create(Traverse.Create(FindObjectOfType<FishingRod>()).Field("fishingBaitHandler").GetValue()).Field("defaultDropper").GetValue()).Field("randomDropperAsset").GetValue());
                {
                    var items = rand.randomizer.items.ToList();
                    items.Add(new RandomItem() { obj = spriteItems["Garden_Sprite"], weight = 1.5f, spawnChance = "" });
                    items.Add(new RandomItem() { obj = spriteItems["Animal_Sprite"], weight = 1.5f, spawnChance = "" });
                    items.Add(new RandomItem() { obj = spriteItems["Fire_Sprite"], weight = 1.5f, spawnChance = "" });
                    items.Add(new RandomItem() { obj = spriteItems["Fire_Sprite_2"], weight = 0.5f, spawnChance = "" });
                    items.Add(new RandomItem() { obj = spriteItems["Cleaner_Sprite"], weight = 1.5f, spawnChance = "" });
                    items.Add(new RandomItem() { obj = spriteItems["Mechanic_Sprite"], weight = 1f, spawnChance = "" });
                    rand.randomizer.items = items.ToArray();
                    var total = rand.randomizer.TotalWeight;
                    foreach (var item in rand.randomizer.items)
                        item.spawnChance = (item.weight / total * 100) + "%";
                }

            BlockCreator.RemoveBlockCallStack += RemoveBlock;

            harmony = new Harmony("com.aidanamite.BenevolentSprites");
            harmony.PatchAll();

            Traverse.Create(typeof(LocalizationManager)).Field("OnLocalizeEvent").GetValue<LocalizationManager.OnLocalizeCallback>().Invoke();
            Log("Mod has been loaded!");
        }

        public void OnModUnload()
        {
            harmony?.UnpatchAll(harmony.Id);
            BlockCreator.RemoveBlockCallStack -= RemoveBlock;
            if (language != null)
                LocalizationManager.Sources.Remove(language);
            foreach (var rand in Resources.FindObjectsOfTypeAll<SO_RandomDropper>())
                if (rand.randomizer.items.Any(x => x.obj is Item_Base && spriteItems.Any(y => y.Value?.UniqueIndex == (x.obj as Item_Base).UniqueIndex) ))
                {
                    var items = rand.randomizer.items.ToList();
                    items.RemoveAll(x=> spriteItems.Any(y => y.Value?.UniqueIndex == (x.obj as Item_Base).UniqueIndex));
                    rand.randomizer.items = items.ToArray();
                    var total = rand.randomizer.TotalWeight;
                    foreach (var item in rand.randomizer.items)
                        item.spawnChance = (item.weight / total * 100) + "%";
                }
            if (repellentItem)
            {
                foreach (var type in Resources.FindObjectsOfTypeAll<SO_BlockQuadType>())
                    Traverse.Create(type).Field("acceptableBlockTypes").GetValue<List<Item_Base>>()?.RemoveAll(x => !x || repellentItem.UniqueIndex == x.UniqueIndex);
                foreach (var type in Resources.FindObjectsOfTypeAll<SO_BlockCollisionMask>())
                    Traverse.Create(type).Field("blockTypesToIgnore").GetValue<List<Item_Base>>()?.RemoveAll(x => !x || repellentItem.UniqueIndex == x.UniqueIndex);
            }
            foreach (var o in createdObjects)
                if (o)
                    DestroyImmediate(o);
            ItemManager.GetAllItems().RemoveAll(x => !x);
            createdObjects.Clear();
            Log("Mod has been unloaded!");
        }

        static void RemoveBlock(List<Block> blocks, Network_Player player)
        {
            foreach (var block in blocks)
            {
                if (!block) continue;
                if (block is Block_Foundation_ItemNet)
                    itemnets.Remove((Block_Foundation_ItemNet)block);
                if (block.GetComponent<BeeHive>())
                    beehives.Remove(block.GetComponent<BeeHive>());
                if (block.GetComponent<CookingTable_Recipe_UI>())
                    recipes.Remove(block.GetComponent<CookingTable_Recipe_UI>());
                if (block.GetComponent<CookingTable>())
                    tables.Remove(block.GetComponent<CookingTable>());
                if (block.GetComponent<Block_CookingStand>())
                    stands.Remove(block.GetComponent<Block_CookingStand>());
                tanks.RemoveAll((x) => !x.Receiver || !x.Tank || x.Receiver.gameObject == block.gameObject || x.Tank.transform.IsChildOf(block.transform));
                if (dredger_basket != null && block.networkedBehaviour)
                    dredgers.Remove(block.networkedBehaviour);
            }
        }

        public static HelperSprite CreateSprite(Storage_Small box, Slot item, uint SpriteIndex = 0)
        {
            if (item.itemInstance?.UniqueName == null)
            {
                Debug.LogWarning("Storage box slot does not contain a valid item. Something may have gone wrong");
                return null;
            }
            if (!spriteTypes.ContainsKey(item.itemInstance.UniqueName))
                return null;
            HelperSprite newFire = (HelperSprite)spriteTypes[item.itemInstance.UniqueName].Invoke(new object[] { box, item, SpriteIndex });
            sprites.Add(newFire);
            if (Raft_Network.IsHost)
                new Message_Sprite_Create(newFire).Message.Broadcast();
            return newFire;
        }
        public static HelperSprite CreateSprite(Message_InitiateConnection message)
        {
            var data = Message_Sprite.CreateFrom(message) as Message_Sprite_Create;
            if (data != null)
            {
                data.Use();
                return data.Sprite;
            }
            return null;
        }
        public static HelperSprite CreateSprite(string data)
        {
            //Debug.Log("Restoring sprite from serial");
            var msg = Message_Sprite.CreateFrom(new Message_InitiateConnection(0, MessageValues.Recreate, data)) as Message_Sprite_Recreate;
            msg.Use();
            return msg.Sprite;
        }

        public void Update()
        {
            if (Time.deltaTime > 0)
                for (int i = sprites.Count - 1; i >= 0; i--)
                    try
                    {
                        //GetSystemTimeAsFileTime(out var start);
                        if (sprites[i].IsDead)
                            sprites.RemoveAt(i);
                        else
                            sprites[i].Update(Time.deltaTime);
                        //GetSystemTimeAsFileTime(out var end);
                        //var span = (end - start) / 10000.0;
                        //if (span > 10)
                        //    Debug.Log($"Too long sprite time {i} {sprites[i]} took {span} ms");
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                    }
            if (MissingIndecies.Count > 0)
            {
                new Message_Sprite_RequestMissing(MissingIndecies.ToArray()).Message.Send(ComponentManager<Raft_Network>.Value.HostID);
                MissingIndecies.Clear();
            }
        }
        static NetworkChannel ModUtils_Channel = (NetworkChannel)MessageValues.ChannelID;
        bool ModUtils_MessageRecieved(CSteamID steamID, NetworkChannel channel, Message message)
        {
            if (message.Type == MessageValues.MessageID && message is Message_InitiateConnection)
            {
                if (gameLoaded)
                    ProcessMessage(message as Message_InitiateConnection);
                else
                    waiting.Add(message as Message_InitiateConnection);
                return true;
            }
            return false;
        }

        public static void ProcessMessage(Message_InitiateConnection msg)
        {
            try
            {
                Message_Sprite.CreateFrom(msg).Use();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        public override void WorldEvent_OnPlayerConnected(CSteamID steamid, RGD_Settings_Character characterSettings)
        {
            if (Raft_Network.IsHost)
            {
                var pkt = new Packet_Multiple(EP2PSend.k_EP2PSendReliable) { messages = new Message[sprites.Count] };
                var i = 0;
                foreach (var sprite in sprites)
                    pkt.messages[i++] = new Message_Sprite_Recreate(sprite, false).Message;
                pkt.Send(steamid);
            }
        }
        public override void WorldEvent_WorldLoaded()
        {
            modlistEntry.jsonmodinfo.isModPermanent = true;
            ModManagerPage.RefreshModState(modlistEntry);
            if (Raft_Network.IsHost)
            {
                gameLoaded = true;
                foreach (var box in StorageManager.allStorages)
                {
                    var placed = new HashSet<Slot>();
                    foreach (var slot in box.GetInventoryReference().allSlots)
                        if (slot.HasSprite() && placed.Add(slot))
                            try
                            {
                                //Debug.Log("Sprite has " + slot.itemInstance.exclusiveString);
                                if (slot.itemInstance.TryGetValue(dataKey, out var data))
                                    try {
                                        CreateSprite(data);
                                    } catch (Exception err)
                                    {
                                        Debug.LogError($"Failed to create sprite in slot {box.GetInventoryReference().allSlots.IndexOf(slot)} of {box.name} ({box.ObjectIndex})\nRaw Item Data: {slot.itemInstance.exclusiveString}\nExtracted Sprite Data: {data}\n{err}");
                                        CreateSprite(box, slot);
                                    }
                                else
                                    CreateSprite(box, slot);
                            }
                            catch (Exception err)
                            {
                                Debug.LogError($"Failed to create sprite in slot {box.GetInventoryReference().allSlots.IndexOf(slot)} of {box.name} ({box.ObjectIndex})\n{err}");
                            }
                }
            }
        }

        [ConsoleCommand(name: "clearSpriteData", docs: "Clears any cache data left on sprites in the world")]
        public static string MyCommand(string[] args)
        {
            if (SceneManager.GetActiveScene().name != Raft_Network.GameSceneName)
                return "Can only be used in world";
            foreach (var s in RAPI.GetLocalPlayer().Inventory.allSlots)
                if (s.HasSprite())
                    s.itemInstance.RemoveValue(dataKey);
            foreach (var b in StorageManager.allStorages)
                foreach (var s in b.GetInventoryReference().allSlots)
                    if (s.HasSprite())
                        s.itemInstance.RemoveValue(dataKey);
            return "Sprite data cleared";
        }

        [ConsoleCommand(name: "getSpriteData", docs: "Usage: getSpriteData [index] - Returns a list of sprite indecies or returns details on a sprite with a specific index")]
        public static string MyCommand2(string[] args)
        {
            if (SceneManager.GetActiveScene().name != Raft_Network.GameSceneName)
                return "Can only be used in world";
            if (args.Length == 0)
                return sprites.Join(x => x == null ? "null" : $"{x.ObjectIndex} ({x.GetType().Name}) in {x.box?.buildableItem?.settings_Inventory.DisplayName}", "\n");
            if (!uint.TryParse(args[0], out var i))
                return $"Could not parse {args[0]} as a whole positive number";
            foreach (var s in sprites)
                if (s?.ObjectIndex == i)
                    return $"Type: {s.GetType().Name}\nStorage Box Type: {s.box?.buildableItem?.settings_Inventory.DisplayName}\nPosition: {s.gameObject?.transform.position}\nTarget Position: {s.TargetGlobal}\nTarget Object: {s.targetObject?.name}";
            return "";
        }

        public override void WorldEvent_WorldUnloaded()
        {
            gameLoaded = false;
            itemnets.Clear();
            beehives.Clear();
            recipes.Clear();
            tables.Clear();
            stands.Clear();
            tanks.Clear();
            CleanerSprite.fishNetCache.Clear();

            modlistEntry.jsonmodinfo.isModPermanent = false;
            ModManagerPage.RefreshModState(modlistEntry);
        }

        void ExtraSettingsAPI_Load() => ExtraSettingsAPI_SettingsClose();
        void ExtraSettingsAPI_SettingsClose()
        {
            PlaySounds = ExtraSettingsAPI_GetCheckboxState("playSound");
            keepItem = ExtraSettingsAPI_GetComboboxSelectedIndex("leaveItem");
            ec_garden = ExtraSettingsAPI_GetCheckboxState("gardenCollect");
            ec_animal = ExtraSettingsAPI_GetCheckboxState("animalCollect");
            ec_animal2 = ExtraSettingsAPI_GetCheckboxState("animal2Collect");
            ec_animal3 = ExtraSettingsAPI_GetCheckboxState("animal3Collect");
            ec_fire = ExtraSettingsAPI_GetCheckboxState("fireCollect");
            ec_fire2 = ExtraSettingsAPI_GetCheckboxState("fire2Collect");
            ec_clean = ExtraSettingsAPI_GetCheckboxState("cleanCollect");
            lightMode = ExtraSettingsAPI_GetComboboxSelectedIndex("lightMode");
            spriteRange = Parse(ExtraSettingsAPI_GetInputValue("spriteRange"));
            gardenRange = Parse(ExtraSettingsAPI_GetInputValue("gardenRange"));
            animalRange = Parse(ExtraSettingsAPI_GetInputValue("animalRange"));
            fireRange = Parse(ExtraSettingsAPI_GetInputValue("fireRange"));
            fire2Range = Parse(ExtraSettingsAPI_GetInputValue("fire2Range"));
            cleanerRange = Parse(ExtraSettingsAPI_GetInputValue("cleanerRange"));
            mechanicRange = Parse(ExtraSettingsAPI_GetInputValue("mechanicRange"));
            repelRange = Parse(ExtraSettingsAPI_GetInputValue("repelRange"));
            repelRange *= repelRange;

            //Debug.Log($"{spriteRange} {repelRange}");
        }

        public static ConstructorInfo GetConstructor<T>() where T : HelperSprite => typeof(T).GetConstructor(new Type[] { typeof(Storage_Small), typeof(Slot), typeof(uint) });

        public virtual bool ExtraSettingsAPI_GetCheckboxState(string SettingName) => false;
        public virtual string ExtraSettingsAPI_GetInputValue(string SettingName) => "";
        public virtual int ExtraSettingsAPI_GetComboboxSelectedIndex(string SettingName) => -1;


        static float Parse(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 1;
            if (value.Contains(",") && !value.Contains("."))
                value = value.Replace(',', '.');
            var c = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfoByIetfLanguageTag("en-NZ");
            Exception e = null;
            float r = 0;
            try
            {
                r = float.Parse(value);
            }
            catch (Exception e2)
            {
                e = e2;
            }
            CultureInfo.CurrentCulture = c;
            if (e != null)
                throw e;
            return r;
        }

        public override void ModEvent_OnModLoaded(Mod mod)
        {
            base.ModEvent_OnModLoaded(mod);
            if (mod.GetType().Name == "Arashi_RaftFishingExpansion")
            {
                _activeFishingNets = AccessTools.Field(mod.GetType(), "activeFishingNets");
                fishingnet = mod.GetType().Assembly.GetType("Arashi.FishingNet");
                arashiObjectEnabler = mod.GetType().Assembly.GetType("Arashi.objectEnabler");
                ExtentionMethods.SetupFishingNetMethods();
            }
            if (mod.GetType().Name == "Dredgers")
            {
                dredger_basket = mod.GetType().Assembly.GetType("Dredger_Basket");
                dredger_network = mod.GetType().Assembly.GetType("Dredger_Network");
                foreach (var b in BlockCreator.GetPlacedBlocks())
                    if (b && b.isActiveAndEnabled && b.networkedBehaviour && dredger_network.IsAssignableFrom(b.networkedBehaviour.GetType()))
                        dredgers.Add(b.networkedBehaviour);
                ExtentionMethods.SetupDredgerMethods();
            }
        }
        public override void ModEvent_OnModUnloaded(Mod mod)
        {
            base.ModEvent_OnModUnloaded(mod);
            if (mod.GetType().Name == "Arashi_RaftFishingExpansion")
                _activeFishingNets = null;
            if (mod.GetType().Name == "Dredgers")
            {
                dredger_basket = null;
                dredger_network = null;
                dredgers.Clear();
            }
        }

        public static void UsingFakeInventory(Action<PlayerInventory> action)
        {
            var tempObj = new GameObject();
            Patch_InventoryOverrides.ignore = tempObj;
            tempObj.SetActive(false);
            var tempInv = tempObj.AddComponent<PlayerInventory>();
            tempInv.hotslotCount = 1;
            tempInv.hotbar = tempObj.AddComponent<Hotbar>();
            tempInv.SetInventoryPickup(tempObj.AddComponent<InventoryPickup>());
            var tempTxt = new GameObject().AddComponent<Text>();
            for (int i = 0; i < 20; i++)
            {
                var s = new GameObject().AddComponent<Slot>();
                s.rectTransform = s.gameObject.AddComponent<RectTransform>();
                s.active = true;
                s.textComponent = tempTxt;
                tempInv.allSlots.Add(s);
            }
            action(tempInv);
            foreach (var slot in tempInv.allSlots)
                Destroy(slot);
            Destroy(tempTxt.gameObject);
            Destroy(tempObj);
        }

        public static Network_Player CreateFakePlayer(bool setLocal = false)
        {
            var g = new GameObject("");
            g.SetActive(false);
            DontDestroyOnLoad(g);
            var DummyPlayer = g.AddComponent<Network_Player>();
            DummyPlayer.SetAnimator(g.AddComponent<PlayerAnimator>());
            if (setLocal)
                DummyPlayer.SetIsLocalPlayer(true);
            return DummyPlayer;
        }

        [DllImport("kernel32")]
        public static extern void GetSystemTimeAsFileTime(out long value);
    }

    static class ExtentionMethods
    {
        static Dictionary<Slot, Storage_Small> refs = new Dictionary<Slot, Storage_Small>();
        static Dictionary<SO_ItemYield, List<Item_Base>> yieldCache = new Dictionary<SO_ItemYield, List<Item_Base>>();

        [Accessor(AccessorType.Field, typeof(Cropplot), "plantManager")]
        public static PlantManager plantManager(this Cropplot plot) => default;
        [Accessor(AccessorType.Field, typeof(PlayerInventory), "inventoryPickup")]
        public static void SetInventoryPickup(this PlayerInventory inv, InventoryPickup value) { }
        [Accessor(AccessorType.Field, typeof(Network_Player), "animator")]
        public static void SetAnimator(this Network_Player player, PlayerAnimator value) { }
        [Accessor(AccessorType.Field, typeof(Network_Player), "isLocalPlayer")]
        public static void SetIsLocalPlayer(this Network_Player player, bool value) { }
        [Accessor(AccessorType.Method, typeof(CookingTable), "StartCooking")]
        public static bool StartCooking(this CookingTable table, SO_CookingTable_Recipe recipe) => default;
        [Accessor(AccessorType.Method, typeof(CookingTable), "SetAnimationState")]
        public static IEnumerator SetAnimationState(this CookingTable table, bool state, float delay) => default;
        [Accessor(AccessorType.Field, typeof(CookingTable), "startAnimDelay")]
        public static float StartAnimDelay(this CookingTable table) => default;
        [Accessor(AccessorType.Field, typeof(CookingTable), "gooModel")]
        public static GameObject GooModel(this CookingTable table) => default;
        [Accessor(AccessorType.Field, typeof(CookingTable), "finishedModel")]
        public static GameObject FinishedModel(this CookingTable table) => default;
        [Accessor(AccessorType.Field, typeof(CookingTable), "pickupFoodItem")]
        public static Item_Base PickupFoodItem(this CookingTable table) => default;
        [Accessor(AccessorType.Field, typeof(Slot), "inventory")]
        public static Inventory Inventory(this Slot slot) => default;
        [Accessor(AccessorType.Field, typeof(RandomDropper), "randomDropperAsset")]
        public static SO_RandomDropper RandomDropperAsset(this RandomDropper dropper) => default;
        [Accessor(AccessorType.Field, typeof(SoundManager), "eventRef_UI_MoveItem")]
        public static string MoveItemEventRef(this SoundManager sm) => default;
        [Accessor(AccessorType.Method, typeof(BeeHive), "HarvestYield")]
        public static void HarvestYield(this BeeHive hive, Network_Player player) { }
        [Accessor(AccessorType.Field, typeof(BirdsNest), "eggItem")]
        public static Item_Base EggItem(this BirdsNest nest) => default;
        [Accessor(AccessorType.Field, typeof(BirdsNest), "featherItem")]
        public static Item_Base FeatherItem(this BirdsNest nest) => default;
        [Accessor(AccessorType.Field, typeof(ItemNet), "itemCollector")]
        public static ItemCollector ItemCollector(this ItemNet net) => default;
        [Accessor(AccessorType.Method, typeof(ItemCollector), "ClearCollectedItems")]
        public static void ClearCollectedItems(this ItemCollector collector, Network_Player player) { }

        [Accessor(AccessorType.Field, typeof(List<Slot>), "_items")]
        public static Slot[] GetInternal(this List<Slot> slots) => default;

        public static void SetupFishingNetMethods()
        {
            _fishingNet = fishingnet.GetField("fishingNet", ~BindingFlags.Default);
            _fishCount = fishingnet.GetField("fishCount", ~BindingFlags.Default);
            _caughtFish = fishingnet.GetField("caughtFish", ~BindingFlags.Default);
            _caughtFishItem = fishingnet.GetField("caughtFishItem", ~BindingFlags.Default);
            _netFull = fishingnet.GetField("netFull", ~BindingFlags.Default);
            _DisableModels = arashiObjectEnabler.GetMethod("DisableModels", ~BindingFlags.Default);
        }
        static FieldInfo _fishingNet;
        public static Block FishingNet_Block(this MonoBehaviour net) => (Block)_fishingNet.GetValue(net);
        static FieldInfo _fishCount;
        public static int FishingNet_FishCount(this MonoBehaviour net) => (int)_fishCount.GetValue(net);
        public static void FishingNet_FishCount(this MonoBehaviour net, int value) => _fishCount.SetValue(net,value);
        static FieldInfo _caughtFish;
        public static Component[] FishingNet_CaughtFish(this MonoBehaviour net) => (Component[])_caughtFish.GetValue(net);
        static FieldInfo _caughtFishItem;
        public static Item_Base[] FishingNet_CaughtFishItem(this MonoBehaviour net) => (Item_Base[])_caughtFishItem.GetValue(net);
        public static void FishingNet_CaughtFishItem(this MonoBehaviour net, Item_Base[] value) => _caughtFishItem.SetValue(net,value);
        static FieldInfo _netFull;
        public static void FishingNet_SetNetFull(this MonoBehaviour net, bool value) => _netFull.SetValue(net, value);
        static MethodInfo _DisableModels;
        public static void FishObjectEnabler_DisableModels(this Component enabler) => _DisableModels.Invoke(enabler, new object[0]);

        public static void SetupDredgerMethods()
        {
            _currentItem = dredger_network.GetField("currentItem", ~BindingFlags.Default);
            _basket = dredger_network.GetField("basket", ~BindingFlags.Default);
            _dredgerType = dredger_network.GetField("dredgerType", ~BindingFlags.Default);
            _PickupItem = dredger_network.GetMethod("PickupItem", ~BindingFlags.Default);
            _DropDredger = dredger_network.GetMethod("DropDredger", ~BindingFlags.Default);
            _RaiseDredger = dredger_network.GetMethod("RaiseDredger", ~BindingFlags.Default);
            _IsDropped = dredger_network.GetProperty("IsDropped", ~BindingFlags.Default).GetGetMethod();
            _IsRising = dredger_network.GetProperty("IsRising", ~BindingFlags.Default).GetGetMethod();
            _ItemInBasket = dredger_basket.GetProperty("ItemInBasket", ~BindingFlags.Default).GetGetMethod();
        }
        static FieldInfo _currentItem;
        public static Item_Base Dredger_CurrentItem(this MonoBehaviour dredger) => (Item_Base)_currentItem.GetValue(dredger);
        static FieldInfo _basket;
        public static MonoBehaviour Dredger_Basket(this MonoBehaviour dredger) => (MonoBehaviour)_basket.GetValue(dredger);
        static MethodInfo _PickupItem;
        public static void Dredger_PickupItem(this MonoBehaviour dredger, Network_Player player) => _PickupItem.Invoke(dredger, new[] { player });
        static MethodInfo _DropDredger;
        public static void Dredger_DropDredger(this MonoBehaviour dredger, bool sendMessage = true) => _DropDredger.Invoke(dredger, new object[] { sendMessage });
        static MethodInfo _RaiseDredger;
        public static void Dredger_RaiseDredger(this MonoBehaviour dredger, bool sendMessage = true) => _RaiseDredger.Invoke(dredger, new object[] { sendMessage });
        static MethodInfo _IsDropped;
        public static bool Dredger_IsDropped(this MonoBehaviour dredger) => (bool)_IsDropped.Invoke(dredger, new object[0]);
        static MethodInfo _IsRising;
        public static bool Dredger_IsRising(this MonoBehaviour dredger) => (bool)_IsRising.Invoke(dredger, new object[0]);
        static FieldInfo _dredgerType;
        public static int Dredger_Type(this MonoBehaviour dredger) => ((IConvertible)_dredgerType.GetValue(dredger)).ToInt32(CultureInfo.InvariantCulture);
        public static bool Dredger_IsManual(this MonoBehaviour dredger) => dredger.Dredger_Type() == 0;
        static MethodInfo _ItemInBasket;
        public static bool DredgerBasket_ItemInBasket(this MonoBehaviour dredger_basket) => (bool)_ItemInBasket.Invoke(dredger_basket, new object[0]);


        public static Sprite GetReadable(this Sprite source)
        {
            var s = Sprite.Create(source.texture.GetReadable(), source.rect, source.pivot, source.pixelsPerUnit);
            createdObjects.Add(s);
            return s;
        }

        public static Texture2D GetReadable(this Texture2D source)
        {
            RenderTexture temp = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Default);
            Graphics.Blit(source, temp);
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = temp;
            Texture2D texture = new Texture2D(source.width, source.height);
            texture.ReadPixels(new Rect(0, 0, temp.width, temp.height), 0, 0);
            texture.Apply();
            createdObjects.Add(texture);
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(temp);
            return texture;
        }
        public static Sprite CreateEmpty(this Sprite sprite)
        {
            var s =  Sprite.Create(new Texture2D(sprite.texture.width, sprite.texture.height, TextureFormat.RGBA32, false), sprite.rect, sprite.pivot, sprite.pixelsPerUnit);
            createdObjects.Add(s);
            return s;
        }
        public static Item_Base Clone(this Item_Base source, int uniqueIndex, string uniqueName)
        {
            Item_Base item = ScriptableObject.CreateInstance<Item_Base>();
            item.Initialize(uniqueIndex, uniqueName, source.MaxUses);
            item.settings_buildable = source.settings_buildable.Clone();
            item.settings_consumeable = source.settings_consumeable.Clone();
            item.settings_cookable = source.settings_cookable.Clone();
            item.settings_equipment = source.settings_equipment.Clone();
            item.settings_Inventory = source.settings_Inventory.Clone();
            item.settings_recipe = source.settings_recipe.Clone();
            item.settings_usable = source.settings_usable.Clone();
            createdObjects.Add(item);
            return item;
        }

        public static void SetRecipe(this ItemInstance_Recipe item, CostMultiple[] cost, int amountToCraft = 1)
        {
            Traverse.Create(item).Field("amountToCraft").SetValue(amountToCraft);
            item.NewCost = cost;
        }

        public static bool HasSprite(this Slot slot)
        {
            return slot.HasValidItemInstance() && slot.itemInstance.UniqueName.IsSprite();
        }

        public static bool IsSprite(this string uniqueName) => spriteItems.ContainsKey(uniqueName);
        public static bool IsSprite(this Item_Base item) => item.UniqueName.IsSprite();

        public static HelperSprite GetSprite(this Slot slot)
        {
            foreach (var sprite in sprites)
                if (sprite.store == slot)
                    return sprite;
            return null;
        }

        public static int Missing(this Fuel fuel) => fuel == null ? 0 : fuel.MaxFuel - fuel.GetFuelCount();

        public static List<ItemInstance> ForceStart(this CookingTable table, SO_CookingTable_Recipe recipe, List<Item_Base> itemsToUse = null)
        {
            if (recipe == null)
                return null;
            if (Raft_Network.IsHost)
            {
                var previous = new ItemInstance[table.Slots.Length];
                for (int i = 0; i < previous.Length; i++)
                    previous[i] = table.Slots[i].CurrentItem;
                for (int i = 0; i < previous.Length; i++)
                    if (itemsToUse.Count > i)
                        table.Slots[i].InsertItem(null, new ItemInstance(itemsToUse[i], 1, itemsToUse[i].MaxUses));
                    else
                        table.Slots[i].ClearItem();
                Patch_StartCookingTable.forceStart = true;
                table.StartCooking(recipe);
                Patch_StartCookingTable.forceStart = false;
                var items = new List<ItemInstance>();
                for (int i = 0; i < previous.Length; i++)
                {
                    if (table.Slots[i].CurrentItem != null)
                        items.Add(table.Slots[i].CurrentItem);
                    if (previous[i] == null)
                        table.Slots[i].ClearItem();
                    else
                        table.Slots[i].InsertItem(null, previous[i]);
                }
                return items;
            }
            table.CurrentRecipe = recipe;
            table.CookTimer = 0f;
            table.Portions = 0;
            table.StartCoroutine(table.SetAnimationState(true, table.StartAnimDelay()));
            if (table is CookingTable_Pot p)
                p.Fuel.StartBurning();
            return null;
        }

        public static void Remove(this CookingTable table, int amount)
        {
            if (amount <= 0)
                return;
            if (table.Portions < amount)
                table.Portions = 0;
            else
                table.Portions -= (uint)amount;
            if (table.Portions == 0)
            {
                table.CurrentRecipe = null;
                table.GooModel().SetActive(false);
                table.FinishedModel().SetActive(false);
            }
        }

        public static void Clear(this PickupChanneling pickup)
        {
            while (pickup.ItemsRemaining != 0)
                pickup.YieldHandler.RemoveYield();
            pickup.pickupItem.networkID.OnPickedUp?.Invoke();
        }

        public static Storage_Small GetStorage(this Slot slot)
        {
            if (refs.TryGetValue(slot, out var box) && box != null)
                return box;
            var inv = slot.Inventory();
            foreach (var storage in StorageManager.allStorages)
                if (storage.GetInventoryReference() == inv)
                {
                    refs[slot] = storage;
                    return storage;
                }
            return null;
        }

        public static void Close(this Storage_Small box)
        {
            new Message_Storage_Close(Messages.StorageManager_Close, RAPI.GetLocalPlayer().StorageManager, box).Broadcast(NetworkChannel.Channel_Game);
        }

        public static bool Contains(this List<ItemInstance> items, Item_Base item)
        {
            foreach (var i in items)
                if (i.Valid && i.baseItem == item)
                    return true;
            return false;
        }

        public static int GetCount(this List<ItemInstance> items, Item_Base item)
        {
            var c = 0;
            foreach (var i in items)
                if (i.baseItem == item)
                    c += i.Amount;
            return c;
        }

        public static int GetCount(this List<ItemInstance> items, CostMultiple item, bool leaveOneItem = false)
        {
            var c = 0;
            var leave = new HashSet<Item_Base>();
            foreach (var i in items)
                if (item.ContainsItem(i.baseItem))
                {
                    if (leaveOneItem && leave.Add(i.baseItem))
                        c += i.Amount - 1;
                    else
                        c += i.Amount;
                }
            return c;
        }

        public static void Remove(this List<ItemInstance> items, Item_Base item, int Count, bool returnItemAfterUse = false)
        {
            for (int i = items.Count - 1; i >= 0; i--)
                if (items[i].baseItem.UniqueIndex == item.UniqueIndex)
                {
                    if (items[i].Amount > Count)
                    {
                        items[i].Amount -= Count;
                        if (returnItemAfterUse && item.settings_consumeable.ItemAfterUse?.item != null)
                            for (int j = 0; j < Count; j++)
                                items.Add(item.settings_consumeable.ItemAfterUse);
                        return;
                    }
                    else if (items[i].Valid)
                    {
                        Count -= items[i].Amount;
                        if (returnItemAfterUse && item.settings_consumeable.ItemAfterUse?.item != null)
                            for (int j = 0; j < items[i].Amount; j++)
                                items.Add(item.settings_consumeable.ItemAfterUse);
                        items.RemoveAt(i);
                    }
                    else
                        items.RemoveAt(i);
                }
        }
        
        public static void RemoveUses(this List<ItemInstance> items, Item_Base item, int Count)
        {
            if (item.MaxUses <= 1)
                items.Remove(item, Count, true);
            else
                for (int i = items.Count - 1; i >= 0; i--)
                    if (items[i].baseItem.UniqueIndex == item.UniqueIndex)
                    {
                        if (items[i].Uses > Count)
                        {
                            items[i].Uses -= Count;
                            return;
                        }
                        else if (!items[i].Valid)
                            items.RemoveAt(i);
                        else if (items[i].UsesInStack <= Count)
                        {
                            Count -= items[i].UsesInStack;
                            if (item.settings_consumeable.ItemAfterUse?.item != null)
                                for (int j = 0; j < items[i].Amount; j++)
                                    items.Add(item.settings_consumeable.ItemAfterUse);
                            items.RemoveAt(i);
                        }
                        else
                        {
                            while (Count > items[i].Uses)
                            {
                                Count -= items[i].Uses;
                                items[i].Amount--;
                                if (item.settings_consumeable.ItemAfterUse?.item != null)
                                    items.Add(item.settings_consumeable.ItemAfterUse);
                                items[i].Uses = item.MaxUses;
                            }
                            items[i].Uses -= Count;
                            return;
                        }
                    }
        }

        public static CookingTable_Recipe_UI FindRecipe(this CookingTable table, CachedItemCollection items, bool leaveOneItem = false)
        {
            CookingTable_Recipe_UI nearest = null;
            var min = float.MaxValue;
            foreach (var recipe in recipes)
                if (recipe && recipe.Recipe && recipe.Recipe.RecipeType == table.cookingType)
                {
                    var dist = (recipe.transform.localPosition - table.transform.localPosition).sqrMagnitude;
                    if (dist < min && items.HasEnough(recipe.Recipe.RecipeCost, leaveOneItem))
                    {
                        min = dist;
                        nearest = recipe;
                    }
                }
            if (min > 4)
                return null;
            return nearest;
        }

        public static List<ItemInstance> TakeItems(this Inventory inventory, CostMultiple[] costs)
        {
            var items = new List<ItemInstance>();
            foreach (CostMultiple cost in costs)
            {
                var c = cost.amount;
                foreach (var slot in inventory.allSlots)
                    if (slot.HasValidItemInstance() && cost.ContainsItem(slot.itemInstance.baseItem))
                    {
                        if (slot.itemInstance.Amount <= c)
                        {
                            c -= slot.itemInstance.Amount;
                            items.Add(slot.itemInstance.Clone());
                            slot.SetItem(null);
                        }
                        else
                        {
                            items.Add(new Cost(slot.itemInstance.baseItem, c));
                            slot.itemInstance.Amount -= c;
                            c = 0;
                        }
                        slot.RefreshComponents();
                        if (c == 0)
                            break;
                    }
            }
            return items;
        }
        public static List<ItemInstance> TakeItems(this Inventory inventory, Item_Base item, int count, bool leaveOneItem = false)  
        {
            var items = new List<ItemInstance>();
            foreach (var slot in inventory.allSlots)
                if (slot.HasValidItemInstance() && slot.itemInstance.baseItem.UniqueIndex == item.UniqueIndex)
                {
                    if (slot.itemInstance.Amount > count)
                    {
                        items.Add(new ItemInstance(item, count, item.MaxUses));
                        slot.itemInstance.Amount -= count;
                        slot.RefreshComponents();
                        break;
                    }
                    else
                    {
                        if (leaveOneItem)
                        {
                            leaveOneItem = false;
                            count -= slot.itemInstance.Amount - 1;
                            items.Add(new ItemInstance(item, slot.itemInstance.Amount - 1, item.MaxUses));
                            slot.itemInstance.Amount = 1;
                            slot.RefreshComponents();
                        }
                        else
                        {
                            count -= slot.itemInstance.Amount;
                            items.Add(slot.itemInstance);
                            slot.itemInstance = null;
                            slot.RefreshComponents();
                        }
                    }
                }
            return items;
        }
        public static List<ItemInstance> TakeItemUses(this Inventory inventory, Item_Base item, int count, bool leaveOneItem = false)
        {
            var items = new List<ItemInstance>();
            if (item.MaxUses <= 1)
                return inventory.TakeItems(item, count, leaveOneItem);
            foreach (var slot in inventory.allSlots)
                if (slot.HasValidItemInstance() && slot.itemInstance.baseItem.UniqueIndex == item.UniqueIndex)
                {
                    if (slot.itemInstance.Uses > count)
                    {
                        if (slot.itemInstance.Amount > 1)
                        {
                            items.Add(new ItemInstance(item, 1, slot.itemInstance.Uses));
                            slot.itemInstance.Amount--;
                            slot.itemInstance.Uses = item.MaxUses;
                        } else
                        {
                            if (leaveOneItem)
                                leaveOneItem = false;
                            else
                            {
                                items.Add(slot.itemInstance);
                                slot.itemInstance = null;
                            }
                        }
                        slot.RefreshComponents();
                        break;
                    } else if (slot.itemInstance.Amount > 1 && item.MaxUses > count)
                    {
                        items.Add(new ItemInstance(item, 1, item.MaxUses));
                        slot.itemInstance.Amount--;
                        slot.RefreshComponents();
                        break;
                    }
                    else if (slot.itemInstance.UsesInStack <= count)
                    {
                        if (leaveOneItem)
                        {
                            leaveOneItem = false;
                            if (slot.itemInstance.Amount > 1)
                            {
                                count -= slot.itemInstance.UsesInStack - slot.itemInstance.Uses;
                                items.Add(new ItemInstance(slot.itemInstance.baseItem, slot.itemInstance.Amount - 1, slot.itemInstance.baseItem.MaxUses));
                                slot.itemInstance.Amount = 1;
                                slot.RefreshComponents();
                            }
                        }
                        else
                        {
                            count -= slot.itemInstance.UsesInStack;
                            items.Add(slot.itemInstance);
                            slot.itemInstance = null;
                            slot.RefreshComponents();
                        }
                    }
                    else
                    {
                        if (!leaveOneItem && count % item.MaxUses <= slot.itemInstance.Uses)
                        {
                            var c = (int)Math.Ceiling((double)count / item.MaxUses);
                            slot.itemInstance.Amount -= c;
                            items.Add(new ItemInstance(item, c, slot.itemInstance.Uses));
                            slot.itemInstance.Uses = item.MaxUses;
                        }
                        else
                        {
                            var c = (int)Math.Ceiling((double)count / item.MaxUses);
                            slot.itemInstance.Amount -= c;
                            items.Add(new ItemInstance(item, c, item.MaxUses));
                        }
                        slot.RefreshComponents();
                        break;
                    }
                }
            return items;
        }

        public static List<ItemInstance> Remove(this List<ItemInstance> items, CostMultiple[] costs)
        {
            var result = new List<ItemInstance>();
            foreach (CostMultiple cost in costs)
            {
                var c = cost.amount;
                for (int i = items.Count - 1; i >= 0; i--)
                    if (items[i].Valid && cost.ContainsItem(items[i].baseItem))
                    {
                        if (items[i].Amount <= c)
                        {
                            c -= items[i].Amount;
                            result.Add(items[i]);
                            items.RemoveAt(i);
                        }
                        else
                        {
                            var r = items[i].Clone();
                            r.Amount = c;
                            result.Add(r);
                            items[i].Amount -= c;
                            c = 0;
                        }
                        if (c == 0)
                            break;
                    }
            }
            return result;
        }

        public static List<ItemInstance> GetAllItems(this Inventory inventory)
        {
            var items = new List<ItemInstance>();
            foreach (var slot in inventory.allSlots)
                if (slot.HasValidItemInstance())
                    items.Add(slot.itemInstance);
            return items;
        }

        public static List<ItemInstance> GetAllItems(this PickupItem pickup)
        {
            var items = new List<ItemInstance>();
            if (pickup.yieldHandler?.Yield?.Count > 0)
            {
                items.AddAll(pickup.yieldHandler.Yield);
                if (pickup.dropper)
                    for (int i = 0; i < pickup.yieldHandler.Yield.Count; i++)
                    items.AddAll(pickup.dropper.GetRandomItems());
            }
            else if (pickup.dropper)
                items.AddAll(pickup.dropper.GetRandomItems());
            if (pickup.itemInstance != null && pickup.itemInstance.Valid)
                items.Add(pickup.itemInstance);
            if (pickup.specificPickups != null)
                UsingFakeInventory(tempInv =>
                {
                    foreach (var specificPickup in pickup.specificPickups)
                    {
                        specificPickup.PickupSpecific(tempInv);
                        items.AddRange(tempInv.GetAllItems());
                        foreach (var slot in tempInv.allSlots)
                            slot.SetItem(null);
                    }
                });
            return items;
        }

        public static List<Item_Base> GetAllPossible(this PickupItem pickup)
        {
            var loot = new List<Item_Base>();
            if (pickup.yieldHandler && pickup.yieldHandler.yieldAsset)
                foreach (var item in pickup.yieldHandler.yieldAsset.GetAllPossible())
                    loot.AddUniqueOnly(item);
            if (pickup.dropper)
                foreach (var item in pickup.dropper.RandomDropperAsset().randomizer.items)
                    if (item.obj)
                        loot.AddUniqueOnly((Item_Base)item.obj);
            if (pickup.itemInstance != null)
                loot.AddUniqueOnly(pickup.itemInstance.baseItem);
            return loot;
        }

        public static List<Item_Base> GetAllPossible(this SO_ItemYield yield)
        {
            if (yieldCache.ContainsKey(yield))
                return yieldCache[yield];
            var loot = new List<Item_Base>();
            foreach (var item in yield.yieldAssets)
                loot.AddUniqueOnly(item.item);
            return loot;
        }

        public static void AddAll(this List<ItemInstance> items, IEnumerable<Cost> newItems)
        {
            if (newItems != null)
                foreach (var cost in newItems)
                    if (cost != null)
                        items.Add(new ItemInstance(cost.item, cost.amount, cost.item.MaxUses));
        }
        public static void AddAll(this List<ItemInstance> items, IEnumerable<Item_Base> newItems)
        {
            if (newItems != null)
                foreach (var item in newItems)
                    if (item != null)
                        items.Add(new ItemInstance(item, 1, item.MaxUses));
        }
        public static void AddAll(this List<ItemInstance> items, IEnumerable<ItemInstance> newItems)
        {
            if (newItems != null)
                foreach (var item in newItems)
                    if (item != null)
                        items.Add(item);
        }
        public static void Add(this List<ItemInstance> items, Cost cost)
        {
            if (cost != null)
                items.Add(new ItemInstance(cost.item, cost.amount, cost.item.MaxUses));
        }
        public static void Add(this List<ItemInstance> items, Item_Base item)
        {
            if (item)
                items.Add(new ItemInstance(item, 1, item.MaxUses));
        }
        public static void Remove(this PickupItem_Networked pickup)
        {
            if (pickup.stopTrackUseRPC)
            {
                PickupObjectManager.RemovePickupItemNetwork(pickup, new CSteamID(0));
            }
            else
            {
                PickupObjectManager.RemovePickupItem(pickup, new CSteamID(0));
            }
        }

        public static Item_Base FindItem(this Inventory inventory, Block_CookingStand stand, out List<CookingSlot> slots, bool leaveOneItem = false)
        {
            slots = null;
            var leave = new HashSet<Item_Base>();
            var check = new HashSet<int>();
            foreach (var s in inventory.allSlots)
                if (s.HasValidItemInstance())
                {
                    if (leaveOneItem && leave.Add(s.itemInstance.baseItem) && s.itemInstance.Amount == 1)
                        continue;
                    if (!check.Add(s.itemInstance.UniqueIndex))
                        continue;
                    slots = stand.GetCookingSlotsForItem(s.itemInstance.baseItem)?.ToList();
                    if (slots != null)
                        return s.itemInstance.baseItem;
                }
            return null;
        }

        public static bool IsRepelled(this Transform t)
        {
            foreach (var g in SpriteRepellent.repellents)
                if ((t.position - g.transform.position).sqrMagnitude < RepelRange)
                    return true;
            return false;
        }
        public static bool IsRepelled(this Transform t, Vector3 off)
        {
            foreach (var g in SpriteRepellent.repellents)
                if ((t.TransformPoint(off) - g.transform.position).sqrMagnitude < RepelRange)
                    return true;
            return false;
        }

        public static void Broadcast(this Message message, NetworkChannel channel = (NetworkChannel)MessageValues.ChannelID) => ComponentManager<Raft_Network>.Value.RPC(message, Target.Other, EP2PSend.k_EP2PSendReliable, channel);
        public static void Send(this Message message, CSteamID steamID, NetworkChannel channel = (NetworkChannel)MessageValues.ChannelID) => ComponentManager<Raft_Network>.Value.SendP2P(steamID, message, EP2PSend.k_EP2PSendReliable, channel);
        public static void Broadcast(this Packet_Multiple message, NetworkChannel channel = (NetworkChannel)MessageValues.ChannelID) => ComponentManager<Raft_Network>.Value.RPC(message, Target.Other, channel);
        public static void Send(this Packet message, CSteamID steamID, NetworkChannel channel = (NetworkChannel)MessageValues.ChannelID) {
            if (message is Packet_Single single)
                ComponentManager<Raft_Network>.Value.SendP2P(steamID, single, channel);
            else
                ComponentManager<Raft_Network>.Value.SendP2P(steamID, message as Packet_Multiple, channel);
        }

        public static void SetValue(this ItemInstance item, string valueKey, string value)
        {
            JSON json;
            try
            {
                json = JSON.Parse(item.exclusiveString);
            } catch
            {
                json = JSON.EmptyObject;
            }
            if (!json.IsObject)
                json = JSON.EmptyObject;
            if (value != null)
                json[valueKey] = new JSON(value);
            else
                json.Remove(valueKey);
            item.exclusiveString = json.ToString(false);
        }

        public static void SetValues(this ItemInstance item, Dictionary<string, string> values)
        {
            JSON json;
            try
            {
                json = JSON.Parse(item.exclusiveString);
            }
            catch
            {
                json = JSON.EmptyObject;
            }
            if (!json.IsObject)
                json = JSON.EmptyObject;
            foreach (var value in values)
                if (value.Value != null)
                    json[value.Key] = new JSON(value.Value);
                else
                    json.Remove(value.Key);
            item.exclusiveString = json.ToString(false);
        }

        public static bool TryGetValue(this ItemInstance item, string valueKey, out string value)
        {
            value = null;
            try
            {
                var json = JSON.Parse(item.exclusiveString);
                if (!json.IsObject)
                    return false;
                if (json.TryGetValue(valueKey,out var v) && v.CanBeString)
                {
                    value = v;
                    return true;
                }
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return false;
            }
        }

        public static Dictionary<string, string> GetValues(this ItemInstance item, params string[] valueKeys)
        {
            try
            {
                var json = JSON.Parse(item.exclusiveString);
                if (!json.IsObject)
                    return new Dictionary<string, string>();
                var d = new Dictionary<string, string>();
                foreach (var valueKey in valueKeys)
                    if (json.TryGetValue(valueKey, out var v) && v.CanBeString)
                        d[valueKey] = v;
                    else
                        d[valueKey] = null;
                return d;
            }
            catch
            {
                return new Dictionary<string, string>();
            }
        }

        public static void RemoveValue(this ItemInstance item, string valueKey) => item.SetValue(valueKey, null);
        public static void RemoveValues(this ItemInstance item, params string[] valueKeys) => item.SetValues(valueKeys.ToDictionary<string,string>(x => null));

        public static int IndexOf<T>(this T[] t, T value)
        {
            for (int i = 0; i < t.Length; i++)
                if (t[i].Equals(value))
                    return i;
            return -1;
        }

        public static bool Exists<T1, T2>(this IDictionary<T1, T2> pairs, Func<T1, T2, bool> condition)
        {
            foreach (var pair in pairs)
                if (condition(pair.Key, pair.Value))
                    return true;
            return false;
        }

        public static List<T> FindAll<T>(this object obj)
        {
            var t = obj.GetType();
            var l = new List<T>();
            do
            {
                foreach (var f in t.GetFields((BindingFlags)(-1)))
                    if (!f.IsStatic && (f.FieldType == typeof(T) || f.FieldType.IsSubclassOf(typeof(T))))
                        l.Add((T)f.GetValue(obj));
                t = t.BaseType;
            } while (t != typeof(object));
            return l;
        }
        static HashSet<string> warned = new HashSet<string>();
        public static int FuelValue(this Tank tank, Item_Base item)
        {
            SO_FuelValue v = null;
            if (tank && item)
            {
                v = LiquidFuelManager.GetFilteredFuelValueSoFromItem(item, tank.tankFiltrationType);
                if (v == null && warned.Add($"{tank.tankFiltrationType} has no value for {item.UniqueName}"))
                    Debug.LogError($"The tank type {tank.tankFiltrationType} has no value for {item.UniqueName}");
            }
            return v == null ? int.MaxValue : v.fuelValueOfType;
        }
        public static Y Join<X, Y>(this IEnumerable<X> colleciton, Func<X, Y> converter, Func<Y, Y, Y> joiner)
        {
            var c = default(Y);
            foreach (var v in colleciton)
                c = joiner(c, converter(v));
            return c;
        }

        public static bool SequenceEquals<X,Y>(this IEnumerable<X> first, IEnumerable<Y> second, Func<X,Y,bool> equals)
        {
            if (first == second)
                return true;
            if (first == null || second == null)
                return false;
            var a = first.GetEnumerator();
            var b = second.GetEnumerator();
            var c = a.MoveNext();
            var d = b.MoveNext();
            while (c && d)
            {
                if (!equals(a.Current, b.Current))
                    return false;
                c = a.MoveNext();
                d = b.MoveNext();
            }
            return c == d;
        }

        public static string instStr(this Object obj) => obj + ": " + obj?.GetInstanceID();
    }

    public class CachedItemCollection
    {
        Storage_Small box;
        Dictionary<int, int> counts = new Dictionary<int, int>();
        public CachedItemCollection(Storage_Small storage)
        {
            box = storage;
        }
        public void Forget(int index) => counts.Remove(index);
        bool PreventClear = false;
        public void Clear()
        {
            if (!PreventClear)
                counts.Clear();
        }
        public void CloseWithoutClear()
        {
            PreventClear = true;
            try
            {
                box.Close();
            }
            finally
            {
                PreventClear = false;
            }
        }
        public int GetItemCount(string name) => GetItemCount(LookupItem(name).UniqueIndex);
        public int GetItemCount(int index)
        {
            if (counts.TryGetValue(index, out var result) && result != -1)
                return result;
            result = 0;
            var inter = box.GetInventoryReference().allSlots.GetInternal();
            var len = box.GetInventoryReference().allSlots.Count;
            if (len != 0)
                foreach (var i in inter)
                {
                    if (i.HasValidItemInstance() && i.itemInstance.UniqueIndex == index)
                        result += i.itemInstance.Amount;
                    len--;
                    if (len == 0)
                        break;
                }
            return counts[index] = result;
        }
        public int[] GetItemCounts(params int[] index)
        {
            var result = new int[index.Length];
            var found = new bool[index.Length];
            var flag = true;
            unsafe
            {
                fixed (int* ia = index)
                fixed (int* ra = result)
                fixed (bool* fa = found)
                {
                    var e = ia + index.Length;
                    var pp = ia;
                    var rp = ra;
                    var fp = fa;
                    while (pp < e)
                    {
                        if (!counts.TryGetValue(*pp, out *rp) || *rp == -1)
                        {
                            *rp = 0;
                            flag = false;
                        }
                        else
                            *fp = true;
                        pp++;
                        rp++;
                        fp++;
                    }
                }
            }
            if (flag)
                return result;
            var inter = box.GetInventoryReference().allSlots.GetInternal();
            var len = box.GetInventoryReference().allSlots.Count;
            if (len != 0)
                foreach (var i in inter)
                {
                    if (i.HasValidItemInstance())
                    {
                        var ind = FastIndexOf(index, i.itemInstance.UniqueIndex);
                        if (ind == -1)
                            continue;
                        if (!found[ind])
                            result[ind] += i.itemInstance.Amount;
                    }
                    len--;
                    if (len == 0)
                        break;
                }
            for (int i = 0; i < found.Length; i++)
                if (!found[i])
                    counts[index[i]] = result[i];
            return result;
        }
        public bool HasItem(string name) => HasItem(LookupItem(name).UniqueIndex);
        public bool HasItem(int index)
        {
            if (counts.TryGetValue(index, out var result))
                return result != 0;
            var inter = box.GetInventoryReference().allSlots.GetInternal();
            var len = box.GetInventoryReference().allSlots.Count;
            if (len != 0)
                foreach (var i in inter)
                {
                    if (i.HasValidItemInstance() && i.itemInstance.UniqueIndex == index)
                    {
                        counts[index] = -1;
                        return true;
                    }
                    len--;
                    if (len == 0)
                        break;
                }
            counts[index] = 0;
            return false;
        }

        public int GetCount(CostMultiple cost, bool leaveOne)
        {
            var ints = new int[cost.items.Length];
            for (int i = 0; i < ints.Length; i++)
                ints[i] = cost.items[i].UniqueIndex;
            ints = GetItemCounts(ints);
            var result = 0;
            foreach (var i in ints)
                if (leaveOne && i != 0)
                    result += i - 1;
                else
                    result += i;
            return result;
        }

        public bool HasEnough(CostMultiple[] costs, bool leaveOne)
        {
            var len = 0;
            foreach (var c in costs)
                len += c.items.Length;
            var ints = new int[len];
            unsafe
            {
                fixed (int* a = ints)
                {
                    var pos = a;
                    foreach (var c in costs)
                        foreach (var i in c.items)
                        {
                           *pos = i.UniqueIndex;
                            pos++;
                        }
                }
            }
            ints = GetItemCounts(ints);
            unsafe
            {
                fixed (int* a = ints)
                {
                    var pos = a;
                    foreach (var c in costs)
                    {
                        var count = 0;
                        var e = pos + c.items.Length;
                        for (;pos < e;pos++)
                        {
                            var v = *pos;
                            if (leaveOne && v != 0)
                                count += v - 1;
                            else
                                count += v;
                        }
                        if (count < c.amount)
                            return false;
                    }
                }
            }
            return true;
        }

        static unsafe int FastIndexOf(int[] array, int value)
        {
            fixed(int* a = array)
            {
                var e = array.Length;
                var p = a;
                for (var i = 0; i < e; i++)
                {
                    if (*p == value)
                        return i;
                    p++;
                }
            }
            return -1;
        }
    }

    public class SpriteRepellent : MonoBehaviour
    {
        public static List<SpriteRepellent> repellents = new List<SpriteRepellent>();
        public void OnBlockPlaced() => repellents.Add(this);
        public void OnDestroy()
        {
            if (repellents.Contains(this))
                repellents.Remove(this);
        }
    }

    public class HelperSprite
    {
        static ConditionalWeakTable<Storage_Small, CachedItemCollection> table = new ConditionalWeakTable<Storage_Small, CachedItemCollection>();
        public static CachedItemCollection GetStorageItems(Storage_Small box)
        {
            if (table.TryGetValue(box, out var cache))
                return cache;
            table.Add(box, cache = new CachedItemCollection(box));
            return cache;
        }
        public CachedItemCollection GetStorageItems() => GetStorageItems(box);

        internal GameObject gameObject;
        internal Slot store;
        internal Item_Base item;
        public List<ItemInstance> holding;
        const float acceleration = 2f;
        const float maxVelocity = 10f;
        Vector3 velocity;
        internal Transform targetObject;
        public Vector3 targetPosition;
        Vector3 randomAccel;
        Vector3 targetRand;
        float time;
        internal bool dying;
        public uint ObjectIndex { private set; get; }
        internal Storage_Small box => store.GetStorage();
        public virtual float MaxDistance => box.GetInventoryReference().GetSlotCount() * SpriteRange;
        public bool IsDead => gameObject == null;
        public Vector3 Target => targetObject == null ? targetPosition : gameObject.transform.parent.InverseTransformPoint(targetObject.TransformPoint(targetPosition));
        public Vector3 TargetGlobal => gameObject.transform.parent.TransformPoint(Target);
        public float SqrDistanceFromTarget => (Target - gameObject.transform.localPosition).sqrMagnitude;
        public const float interactDistance = 0.2f;
        public const float sqrInteractDistance = interactDistance * interactDistance;
        public HelperSprite(Storage_Small container, Slot slot, Color color, uint objectIndex = 0)
        {
            gameObject = Object.Instantiate(FirePrefab, GameManager.Singleton.lockedPivot, false);
            if (!RAPI.IsDedicatedServer())
            {
                var renderer = gameObject.GetComponent<SpriteRenderer>();
                renderer.glow.color = color;
            }
            if (objectIndex == 0)
                objectIndex = SaveAndLoad.GetUniqueObjectIndex();
            ObjectIndex = objectIndex;
            store = slot;
            item = slot.itemInstance.baseItem;
            gameObject.transform.position = container.transform.position;
            foreach (var p in gameObject.GetComponentsInChildren<ParticleSystem>())
            {
                var s = p.colorBySpeed;
                s.enabled = false;
                var l = p.colorOverLifetime;
                l.enabled = true;
                l.color = new ParticleSystem.MinMaxGradient(color);
                var m = p.main;
                m.startColor = new ParticleSystem.MinMaxGradient(Color.white);
            }
            SetTarget(container.transform, Vector3.up);
            randomAccel = Vector3.zero;
            dying = false;
            holding = new List<ItemInstance>();
        }
        public void SetTarget(Transform target, Vector3 position)
        {
            targetObject = target;
            targetPosition = position;
        }
        public void SetTargetNetwork(MonoBehaviour_ID target, Vector3 position)
        {
            if (target == null)
                return;
            SetTarget(target.transform, position);
            if (Raft_Network.IsHost)
                new Message_Sprite_SetTarget(this, target, position).Message.Broadcast();
        }

        public void Update(float t)
        {
            if (Raft_Network.IsHost && (!box || store?.itemInstance?.baseItem != item))
                Kill();
            time += t;
            if (time > 0.2)
            {
                targetRand = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f));
                time -= 0.2f;
            }
            randomAccel = Vector3.Lerp(randomAccel, targetRand, t / 2);
            Vector3 dir = Target + randomAccel - gameObject.transform.localPosition;
            if (!dying)
                velocity += dir.normalized * acceleration * t;
            velocity *= (float)Math.Pow(0.3, t);
            velocity.Clamp(0, maxVelocity);
            gameObject.transform.localPosition += velocity * t;
            AI();
        }

        public virtual void Kill()
        {
            if (!dying)
            {
                if (Raft_Network.IsHost)
                    new Message_Sprite_Kill(this).Message.Broadcast();
                CoroutineManager.Singleton.StartCoroutine(Death());
            }
        }

        IEnumerator Death()
        {
            dying = true;
            foreach (var item in holding)
                Helper.DropItem(item, gameObject.transform.position, Vector3.down, true);
            if (!RAPI.IsDedicatedServer())
            {
                float timePassed = 0;
                Light light = gameObject.GetComponent<Light>();
                while (timePassed < 0.75)
                {
                    timePassed += Time.deltaTime / 2;
                    float strength = Math.Max((float)Math.Sin(timePassed * Math.PI) * 3, 0);
                    light.intensity = strength;
                    light.range = strength * 2;
                    yield return new WaitForEndOfFrame();
                }
            }
            Destroy();
            yield break;
        }

        public void Destroy() => Object.Destroy(gameObject);

        void AI()
        {
            if (!Raft_Network.IsHost || dying)
                return;
            if (tryInteract())
            {
                MonoBehaviour_ID target = null;
                Vector3 targetOffset = Vector3.zero;
                //GetSystemTimeAsFileTime(out var start);
                if (holding.Count == 0 && targetObject == box.transform && !box.transform.IsRepelled())
                    pickTarget(out target, out targetOffset);
                //GetSystemTimeAsFileTime(out var end);
                //var span = (end - start) / 10000.0;
                //if (span > 10)
                //    Debug.Log($"Took too long. {this}: {sprites.IndexOf(this)} took {span} ms to pick a target");
                if (target == null)
                {
                    target = box;
                    if (holding.Count == 0)
                        targetOffset = Vector3.up;
                    else
                        targetOffset = Vector3.up * 0.2f;
                }
                if ((target == null ? null : target.transform) != targetObject || targetOffset != targetPosition)
                    SetTargetNetwork(target, targetOffset);
            }
        }

        internal bool tryDepositHeld()
        {
            if (targetObject == box.transform && !box.IsOpen && SqrDistanceFromTarget <= sqrInteractDistance)
            {
                bool added = false;
                Patch_PlayMoveItemSound.disable = !PlaySounds;
                var storage = GetStorageItems();
                for (int i = holding.Count - 1; i >= 0; i--)
                {
                    var item = holding[i];
                    int pre = item.Amount;
                    box.GetInventoryReference().AddItem(item, false);
                    if (pre != item.Amount)
                    {
                        added = true;
                        storage.Forget(item.UniqueIndex);
                    }
                    if (item.Amount == 0)
                        holding.RemoveAt(i);
                }
                Patch_PlayMoveItemSound.disable = false;
                if (added)
                {
                    storage.CloseWithoutClear();
                    if (PlaySounds)
                    {
                        var eventRef = ComponentManager<SoundManager>.Value.MoveItemEventRef();
                        var msg = new Message_SoundManager_PlayOneShot(Messages.SoundManager_PlayOneShot, ComponentManager<Raft_Network>.Value.NetworkIDManager, ComponentManager<SoundManager>.Value.ObjectIndex, eventRef, box.transform.position);
                        msg.Broadcast();
                        FMODUnity.RuntimeManager.PlayOneShot(eventRef, msg.Position);
                    }
                }
            }
            return holding.Count == 0;
        }

        internal virtual bool tryInteract() => true;

        public virtual void RestoreData(string data) { }
        public virtual string GenerateData() => "";

        internal virtual void pickTarget(out MonoBehaviour_ID target, out Vector3 targetOffset) { target = box; targetOffset = Vector3.up; }

        public virtual MonoBehaviour_ID FindTarget(uint objectIndex)
        {
            foreach (var mono in Object.FindObjectsOfType<MonoBehaviour_ID>())
                if (mono.ObjectIndex == objectIndex)
                    return mono;
            return null;
        }

    }

    class GardenSprite : HelperSprite
    {
        static Dictionary<GardenSprite, PlantationSlot> targeted = new Dictionary<GardenSprite, PlantationSlot>();
        static Dictionary<int, List<Item_Base>> plantLootCache = new Dictionary<int, List<Item_Base>>();
        public override float MaxDistance => base.MaxDistance * GardenRange;
        public GardenSprite(Storage_Small container, Slot slot, uint objectIndex = 0) : base(container, slot, new Color(Random.Range(0, 0.125f), Random.Range(0.875f, 1), 0), objectIndex)
        {
            targeted.Add(this, null);
        }

        public override void Kill()
        {
            targeted.Remove(this);
            base.Kill();
        }

        internal override bool tryInteract()
        {
            if (targetObject == null)
                return true;
            if (targetObject == box.transform)
                return tryDepositHeld();
            if (box.transform.IsRepelled() || !targeted[this]?.plant || targeted[this].plant.transform.IsRepelled() || !targeted[this].plant.FullyGrown())
            {
                targeted[this] = null;
                return true;
            }
            if (SqrDistanceFromTarget > sqrInteractDistance)
                return false;
            holding.AddRange(targeted[this].plant.pickupComponent.GetAllItems());
            var seed = targeted[this].plant.item.UniqueIndex;
            targetObject.GetComponent<Cropplot>().plantManager().Harvest(targeted[this].plant, false);
            new Message_HarvestPlant(Messages.PlantManager_HarvestPlant, targetObject.GetComponent<Cropplot>().plantManager(), targeted[this].plant, false).Broadcast(NetworkChannel.Channel_Game);
            foreach (var item in holding)
                if (item.baseItem.UniqueIndex == seed)
                {
                    PlantSeed(targetObject.GetComponent<Cropplot>(), seed);
                    if (item.Amount == 1)
                        holding.Remove(item);
                    else
                        item.Amount--;
                    break;
                }
            targeted[this] = null;
            return true;
        }
        internal override void pickTarget(out MonoBehaviour_ID target, out Vector3 targetOffset)
        {
            float dist = MaxDistance * MaxDistance;
            target = null;
            targetOffset = Vector3.zero;
            targeted[this] = null;
            var storage = GetStorageItems();
            foreach (var cropplot in PlantManager.allCropplots)
            {
                if (cropplot is Cropplot_Grass)
                    continue;
                foreach (var plantation in cropplot.plantationSlots)
                {
                    if (targeted.ContainsValue(plantation) || plantation.transform.IsRepelled())
                        continue;
                    var newDist = (box.transform.position - plantation.transform.position).sqrMagnitude;
                    if (plantation.plant && plantation.plant.FullyGrown() && newDist < dist && ExclusiveCollect(plantation.plant, storage))
                    {
                        target = cropplot;
                        targetOffset = plantation.plant.transform.localPosition + Vector3.up * 0.2f;
                        dist = newDist;
                        targeted[this] = plantation;
                    }
                }
            }
            if (target)
            {
                var item = targeted[this].plant.item;
                if (!box.IsOpen && storage.GetItemCount(item.UniqueIndex) > (KeepItem > 0 ? 1 : 0))
                {
                    storage.Forget(item.UniqueIndex);
                    holding.AddRange(box.GetInventoryReference().TakeItems(item, 1));
                    storage.CloseWithoutClear();
                }
            }
            else
                targeted[this] = null;
        }

        static void PlantSeed(Cropplot crop, int ItemIndex)
        {
            var msg = new Message_Sprite_PlantSeed(crop, ItemIndex, SaveAndLoad.GetUniqueObjectIndex(), ComponentManager<WeatherManager>.Value.GetCurrentWeatherType() == UniqueWeatherType.Rain);
            msg.Use();
            msg.Message.Broadcast();
        }

        bool ExclusiveCollect(Plant plant, CachedItemCollection items)
        {
            if (!EC_Garden)
                return true;
            List<Item_Base> loot;
            if (plantLootCache.ContainsKey(plant.item.UniqueIndex))
                loot = plantLootCache[plant.item.UniqueIndex];
            else
            {
                loot = plant.pickupComponent.GetAllPossible();
                loot.RemoveAll(x => x.UniqueName == "Plank" || x.UniqueName == "Thatch");
                loot.AddUniqueOnly(plant.item);
                plantLootCache.Add(plant.item.UniqueIndex, loot);
            }
            foreach (var item in loot)
                if (items.HasItem(item.UniqueIndex))
                    return true;
            return false;
        }

        public override void RestoreData(string data)
        {
            if (string.IsNullOrEmpty(data))
                return;
            if (targetObject && targetObject.GetComponent<Cropplot>())
                targeted[this] = targetObject.GetComponent<Cropplot>().plantationSlots[int.Parse( data)];
        }
        public override string GenerateData()
        {
            if (targeted[this] != null && targetObject.GetComponent<Cropplot>())
                return targetObject.GetComponent<Cropplot>().plantationSlots.IndexOf(targeted[this]).ToString();
            return null;
        }

        public override MonoBehaviour_ID FindTarget(uint objectIndex)
        {
            foreach (var cropplot in PlantManager.allCropplots)
                if (cropplot.ObjectIndex == objectIndex)
                    return cropplot;
            return base.FindTarget(objectIndex);
        }
    }

    class AnimalSprite : HelperSprite
    {
        static Dictionary<AnimalSprite, MonoBehaviour_ID> targeted = new Dictionary<AnimalSprite, MonoBehaviour_ID>();
        public override float MaxDistance => base.MaxDistance * AnimalRange;
        public AnimalSprite(Storage_Small container, Slot slot, uint objectIndex = 0) : base(container, slot, new Color(Random.Range(0.125f, 0.25f), 0, Random.Range(0.875f, 1)), objectIndex)
        {
            targeted.Add(this, null);
        }
        public override void Kill()
        {
            targeted.Remove(this);
            base.Kill();
        }

        internal override void pickTarget(out MonoBehaviour_ID target, out Vector3 targetOffset)
        {
            target = null;
            targetOffset = Vector3.zero;
            float dist = MaxDistance * MaxDistance;
            targeted[this] = null;
            Item_Base requiredItem = null;
            var storage = GetStorageItems();
            foreach (var animal in animals)
            {
                if (animal == null || animal.DomesticState != DomesticStateType.Raft || targeted.ContainsValue(animal) || animal.transform.IsRepelled())
                    continue;
                var newDist = (box.transform.position - animal.transform.position).sqrMagnitude;
                if (!animal.Resource || !animal.Resource.IsReady || newDist >= dist)
                    continue;
                var collectionItem = animal.Resource.resource.collectionItem;
                if ((collectionItem.MaxUses != 1 || (!box.IsOpen && (KeepItem > 1 ? storage.GetItemCount(collectionItem.UniqueIndex) > 1 : storage.HasItem(collectionItem.UniqueIndex)))) && ExclusiveCollect(animal, storage))
                {
                    if (collectionItem.MaxUses == 1)
                        requiredItem = collectionItem;
                    else
                        requiredItem = null;
                    target = animal;
                    targetOffset = Vector3.up * 0.2f;
                    dist = newDist;
                    targeted[this] = animal;
                }
            }
            foreach (var item in eggs)
            {
                if (item == null || targeted.ContainsValue(item) || item.transform.IsRepelled())
                    continue;
                var newDist = (box.transform.position - item.transform.position).sqrMagnitude;
                if (newDist < dist && ExclusiveCollect(item, storage))
                {
                    requiredItem = null;
                    target = item;
                    targetOffset = Vector3.up * 0.2f;
                    dist = newDist;
                    targeted[this] = item;
                }
            }
            foreach (var hive in beehives)
            {
                if (hive == null || targeted.ContainsValue(hive) || hive.transform.IsRepelled())
                    continue;
                var newDist = (box.transform.position - hive.transform.position).sqrMagnitude;
                if (hive.currentHoneyLevelIndex != -1 && newDist < dist && ExclusiveCollect(hive, storage))
                {
                    requiredItem = null;
                    target = hive;
                    targetOffset = Vector3.up * 0.2f;
                    dist = newDist;
                    targeted[this] = hive;
                }
            }
            if (BirdsNest.AllNests != null)
                foreach (var nest in BirdsNest.AllNests)
                {
                    if (targeted.ContainsValue(nest) || nest.transform.IsRepelled())
                        continue;
                    var newDist = (box.transform.position - nest.transform.position).sqrMagnitude;
                    if ((nest.HasFeather || nest.HasEgg) && newDist < dist)
                    {
                        var v = ExclusiveCollect(nest, storage);
                        if (!v.Item1 && !v.Item2)
                            continue;
                        requiredItem = null;
                        target = nest;
                        targetOffset = Vector3.up * 0.2f;
                        dist = newDist;
                        targeted[this] = nest;
                    }
                }
            if (target && requiredItem)
            {
                storage.Forget(requiredItem.UniqueIndex);
                holding.AddRange(box.GetInventoryReference().TakeItemUses(requiredItem, 1));
                storage.CloseWithoutClear();
            }
        }
        internal override bool tryInteract()
        {
            if (targetObject == null)
                return true;
            if (targetObject == box.transform)
                return tryDepositHeld();
            if (targetObject.IsRepelled() || box.transform.IsRepelled())
                return true;
            ResourceRegenerative resource = null;
            var animal = targetObject.GetComponent<AI_NetworkBehaviour_Domestic_Resource>();
            var pickup = targetObject.GetComponent<PickupItem_Networked>();
            var beehive = targetObject.GetComponent<BeeHive>();
            var nest = targetObject.GetComponent<BirdsNest>();
            if (animal)
            {
                resource = animal.Resource;
                if (!resource.IsReady || animal.DomesticState != DomesticStateType.Raft)
                {
                    targeted[this] = null;
                    return true;
                }
            }
            if ((beehive && beehive.currentHoneyLevelIndex == -1) || (nest && !nest.HasEgg && !nest.HasFeather))
            {
                targeted[this] = null;
                return true;
            }
            if (SqrDistanceFromTarget > sqrInteractDistance)
                return false;
            if (animal)
            {
                Item_Base RequiredItem = resource.resource.collectionItem;
                Item_Base _pickup = resource.HarvestResource();
                holding.Add(new ItemInstance(_pickup, 1, _pickup.MaxUses));
                if (RequiredItem.MaxUses == 1)
                    holding.Remove(RequiredItem, 1);
            }
            else if (beehive)
            {
                holding.AddAll(beehive.CurrentHoneyLevel.yield.yieldAssets);
                new Message_NetworkBehaviour_ID_SteamID(Messages.BeeHiveHarvestOutput, ComponentManager<Raft_Network>.Value.NetworkIDManager, beehive.ObjectIndex, new CSteamID(0)).Broadcast();
                beehive.HarvestYield(null);
            }
            else if (nest)
            {
                var b = ExclusiveCollect(nest, GetStorageItems());
                if (nest.HasEgg && b.Item1)
                {
                    var item = nest.EggItem();
                    holding.Add(new ItemInstance(item, nest.EggCount, item.MaxUses));
                    while (nest.PickupEgg(null))
                        new Message_NetworkBehaviour_SteamID(Messages.BirdsNest_PickupEgg, nest, 0).Broadcast();
                }
                if (nest.HasFeather && b.Item2)
                {
                    var item = nest.FeatherItem();
                    holding.Add(new ItemInstance(item, nest.FeatherCount, item.MaxUses));
                    while (nest.PickupFeather(null))
                        new Message_NetworkBehaviour_SteamID(Messages.BirdsNest_PickupFeather, nest, 0).Broadcast();
                }
            }
            else
            {
                holding.AddRange(pickup.PickupItem.GetAllItems());
                pickup.Remove();
            }
            targeted[this] = null;
            return true;
        }

        bool ExclusiveCollect(AI_NetworkBehaviour_Domestic_Resource animal, CachedItemCollection storage) => !EC_Animal || storage.HasItem(animal.Resource.resource.resource.UniqueIndex);

        bool ExclusiveCollect(PickupItem_Networked pickup, CachedItemCollection storage)
        {
            if (!EC_Animal)
                return true;
            foreach (var item in pickup.PickupItem.GetAllPossible())
                if (storage.HasItem(item.UniqueIndex))
                    return true;
            return false;
        }

        bool ExclusiveCollect(BeeHive hive, CachedItemCollection storage)
        {
            if (!EC_Animal2)
                return true;
            foreach (var item in hive.CurrentHoneyLevel.yield.GetAllPossible())
                if (storage.HasItem(item.UniqueIndex))
                    return true;
            return false;
        }

        (bool,bool) ExclusiveCollect(BirdsNest nest, CachedItemCollection storage)
        {
            if (!EC_Animal3)
                return (true,true);
            return (
                nest.HasEgg && storage.HasItem(nest.EggItem().UniqueIndex),
                nest.HasFeather && storage.HasItem(nest.FeatherItem().UniqueIndex)
                );
        }

        public override void RestoreData(string data)
        {
            if (targetObject)
                targeted[this] = targetObject.GetComponent<MonoBehaviour_ID>();
        }

        public override MonoBehaviour_ID FindTarget(uint objectIndex)
        {
            foreach (var animal in animals)
                if (animal.ObjectIndex == objectIndex)
                    return animal;
            foreach (var item in eggs)
                if (item.ObjectIndex == objectIndex)
                    return item;
            foreach (var hive in beehives)
                if (hive.ObjectIndex == objectIndex)
                    return hive;
            foreach (var nest in BirdsNest.AllNests)
                if (nest.ObjectIndex == objectIndex)
                    return nest;
            return base.FindTarget(objectIndex);
        }
    }

    class FireSprite : HelperSprite
    {
        static Dictionary<FireSprite, CookingSlotTargets> targeted = new Dictionary<FireSprite, CookingSlotTargets>();
        public override float MaxDistance => base.MaxDistance * FireRange;
        public FireSprite(Storage_Small container, Slot slot, uint objectIndex = 0) : base(container, slot, new Color(Random.Range(0.875f, 1), Random.Range(0, 0.125f), 0), objectIndex)
        {
            targeted.Add(this, CookingSlotTargets.Null);
        }
        public override void Kill()
        {
            targeted.Remove(this);
            base.Kill();
        }

        public static bool IsTargeted(CookingSlot slot) => targeted.ContainsValue(slot);

        internal override void pickTarget(out MonoBehaviour_ID target, out Vector3 targetOffset)
        {
            target = null;
            targetOffset = Vector3.zero;
            var maxDist = MaxDistance * MaxDistance;
            float dist = maxDist;
            targeted[this] = CookingSlotTargets.Null;
            Item_Base cookItem = null;
            Cost fuel = null;
            var fueling = true;
            var newTarget = CookingSlotTargets.Null;
            var storage = GetStorageItems();
            foreach (var stand in stands)
            {
                if (!stand || stand is Block_CookingStand_Purifier || (box.transform.position - stand.transform.position).sqrMagnitude > maxDist * 1.2 || stand.transform.IsRepelled())
                    continue;
                var f = 0;
                if (!box.IsOpen && stand.fuel)
                    f = Math.Max(Math.Min(stand.fuel.Missing() - InboundFuel(stand), storage.GetItemCount(stand.fuel.fuelItem.UniqueIndex) - (KeepItem > 1 ? 1 : 0)), 0);
                foreach (var slot in stand.cookingSlots)
                {
                    var flagEmpty = !slot.IsBusy && !box.IsOpen;
                    var flagComplete = slot.IsComplete;
                    if ((!flagComplete && !flagEmpty && f == 0) || slot.transform.IsRepelled())
                        continue;
                    var newDist = (box.transform.position - slot.transform.position).sqrMagnitude;
                    if (newDist >= (fueling ? maxDist : dist))
                        continue;
                    Item_Base item = null;
                    List<CookingSlot> fetched = null;
                    if (f != 0 && (targeted.ContainsValue(slot) || (slot.connectedSlot && targeted.ContainsValue(slot.connectedSlot))))
                        continue;
                    else if (flagEmpty)
                    {
                        Patch_GetCookingSlots.spriteCall = true;
                        try
                        {
                            item = box.GetInventoryReference().FindItem(stand, out fetched, KeepItem > 0);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(e);
                        }
                        Patch_GetCookingSlots.spriteCall = false;
                        if (!item || fetched == null || targeted.ContainsValue(fetched))
                        {
                            if (f == 0)
                                continue;
                            item = null;
                        }
                    }
                    else if (flagComplete)
                        flagComplete = ExclusiveCollect(slot, storage) && !targeted.ContainsValue(slot);
                    if (flagComplete || item || (f != 0 && fueling))
                    {
                        if (stand.fuel != null)
                            fuel = new Cost(stand.fuel.fuelItem, f);
                        cookItem = item;
                        target = stand;
                        if (!flagComplete && !item && !box.IsOpen)
                        {
                            fueling = true;
                            targetOffset = Vector3.up * 0.2f;
                        }
                        else
                        {
                            fueling = false;
                            if (flagComplete)
                                targetOffset = slot.transform.localPosition + Vector3.up * 0.2f;
                            else if (item)
                                targetOffset = fetched[0].transform.localPosition + Vector3.up * 0.2f;
                        }
                        dist = newDist;
                        if (item)
                            newTarget = fetched;
                        else
                        {
                            var l = new List<CookingSlot>();
                            var s = slot;
                            if (slot.connectedSlot)
                                s = slot.connectedSlot;
                            l.Add(s);
                            if (flagComplete)
                                foreach (var i in slot.GetComponentInParent<Block_CookingStand>().cookingSlots)
                                    if (i.connectedSlot == s)
                                        l.Add(i);
                            newTarget = l;
                        }
                    }
                }
            }
            if (target)
            {
                targeted[this] = newTarget;
                if (cookItem)
                {
                    storage.Forget(cookItem.UniqueIndex);
                    holding.AddRange(box.GetInventoryReference().TakeItemUses(cookItem, 1));
                    storage.CloseWithoutClear();
                }
                if (fuel != null && fuel.amount != 0)
                {
                    storage.Forget(fuel.item.UniqueIndex);
                    holding.AddRange(box.GetInventoryReference().TakeItems(fuel.item, fuel.amount));
                    storage.CloseWithoutClear();
                }

                //Debug.Log($"Sprite {sprites.IndexOf(this)} has targeted {newTarget.stand.instStr()} [{newTarget.slotInds.Join()}]. Mode: {(cookItem ? "Start Cooking " + cookItem.settings_Inventory.DisplayName : fuel != null && fuel.amount != 0 ? "Add fuel" : "Pickup item")}");
            }
        }
        internal override bool tryInteract()
        {
            if (!targetObject)
                return true;
            if (targetObject == box.transform)
                return tryDepositHeld();
            if (targetObject.IsRepelled() || box.transform.IsRepelled())
                return true;
            var target = targeted[this];
            if (!target.stand)
                return true;
            foreach (var slot in target.Slots)
                if (!slot.IsComplete && slot.IsBusy && !(target.stand.fuel && holding.Contains(target.stand.fuel.fuelItem) && !target.stand.fuel.HasMaxFuel()))
                {
                    //Debug.Log($"Sprite {sprites.IndexOf(this)} target lost");
                    targeted[this] = CookingSlotTargets.Null;
                    return true;
                }
            if (SqrDistanceFromTarget > sqrInteractDistance)
                return false;
            if (target.stand.fuel != null && holding.Contains(target.stand.fuel.fuelItem) && !target.stand.fuel.HasMaxFuel())
            {
                var m = Math.Min(target.stand.fuel.Missing(), holding.GetCount(target.stand.fuel.fuelItem));
                AddFuel(target.stand, m);
                holding.Remove(target.stand.fuel.fuelItem, m, true);
            }
            if (target.Slot.IsComplete && ExclusiveCollect(target.Slot, GetStorageItems()))
            {
                holding.Add(target.Slot.CurrentItem.settings_cookable.CookingResult);
                ClearItem(target.stand, target.Slot);
            }
            else if (!target.Slot.IsBusy)
            {
                Item_Base item = null;
                foreach (var i in holding)
                {
                    bool all = true;
                    foreach (var s in target.Slots)
                        if (!s.CanCookItem(i.baseItem))
                        {
                            all = false;
                            break;
                        }
                    if (all)
                    {
                        item = i.baseItem;
                        break;
                    }
                }
                if (item != null)
                {
                    InsertItem(target, item);
                    holding.Remove(item, 1);
                }
            }
            targeted[this] = CookingSlotTargets.Null;
            return true;
        }

        bool ExclusiveCollect(CookingSlot slot, CachedItemCollection storage) => !EC_Fire || storage.HasItem(slot.CurrentItem.UniqueIndex) || storage.HasItem(slot.CurrentItem.settings_cookable.CookingResult.item.UniqueIndex);

        static void ClearItem(Block_CookingStand stand, CookingSlot slot)
        {
            var msg = new Message_Sprite_ClearCooker(stand, slot);
            msg.Use();
            msg.Message.Broadcast();
        }

        static void InsertItem(CookingSlotTargets slots, Item_Base item)
        {
            var msg = new Message_Sprite_InsertCooker(slots, item);
            msg.Use();
            msg.Message.Broadcast();
        }

        static void AddFuel(Block_CookingStand stand, int amount)
        {
            var msg = new Message_Sprite_AddFuel(stand, amount);
            msg.Use();
            msg.Message.Broadcast();
        }

        static int InboundFuel(Block_CookingStand stand)
        {
            var i = 0;
            foreach (var p in targeted)
                if (p.Value.stand == stand)
                    i += p.Key.holding.GetCount(stand.fuel.fuelItem);
            return i;
        }

        public override void RestoreData(string data) => targeted[this] = JsonUtility.FromJson<RGD_CookingSlotTargets>( data).GetReal();
        public override string GenerateData() => JsonUtility.ToJson(new RGD_CookingSlotTargets( targeted[this]));

        public override MonoBehaviour_ID FindTarget(uint objectIndex)
        {
            foreach (var stand in stands)
                if (stand.ObjectIndex == objectIndex)
                    return stand;
            return base.FindTarget(objectIndex);
        }
    }

    class FireSprite2 : HelperSprite
    {
        static Dictionary<FireSprite2, CookingTable> targeted = new Dictionary<FireSprite2, CookingTable>();
        SO_CookingTable_Recipe Recipe = null;
        public override float MaxDistance => base.MaxDistance * Fire2Range;
        public FireSprite2(Storage_Small container, Slot slot, uint objectIndex = 0) : base(container, slot, new Color(Random.Range(0.5f, 0.625f), Random.Range(0, 0.125f), 0), objectIndex)
        {
            targeted.Add(this, null);
        }
        public override void Kill()
        {
            targeted.Remove(this);
            base.Kill();
        }

        internal override void pickTarget(out MonoBehaviour_ID target, out Vector3 targetOffset)
        {
            target = null;
            targetOffset = Vector3.zero;
            var maxDist = MaxDistance * MaxDistance;
            var dist = maxDist;
            targeted[this] = null;
            Cost requiredItem = null;
            CookingTable_Recipe_UI recipe = null;
            Cost fuel = null;
            var fueling = true;
            var itemCount = GetStorageItems();
            if (!box.IsOpen)
                foreach (var table in tables)
                {
                    //GetSystemTimeAsFileTime(out var start);
                    if (table == null || targeted.ContainsValue(table) || table.transform.IsRepelled())
                        continue;
                    var newDist = (box.transform.position - table.transform.position).sqrMagnitude;
                    if (newDist >= (fueling ? maxDist : dist))
                        continue;
                    var collectionItem = table.PickupFoodItem();
                    //GetSystemTimeAsFileTime(out var end);
                    //var span = end - start;
                    //if (span > 100000)
                    //    Debug.Log($"Took too long for init check {table.instStr()}. Took {span / 10000.0} ms");
                    //GetSystemTimeAsFileTime(out start);
                    CookingTable_Recipe_UI cooked = null;
                    if (table.CurrentRecipe == null)
                        cooked = table.FindRecipe(itemCount, KeepItem > 0);
                    //GetSystemTimeAsFileTime(out end);
                    //span = end - start;
                    //if (span > 100000)
                    //    Debug.Log($"Took too long to find recipe for {table.instStr()}. Took {span / 10000.0} ms");
                    //GetSystemTimeAsFileTime(out start);
                    var collecting = table.Portions != 0 && (KeepItem > 1 ? itemCount.GetItemCount(collectionItem.UniqueIndex) > 1 : itemCount.HasItem(collectionItem.UniqueIndex));
                    if (collecting && EC_Fire2)
                        collecting = itemCount.HasItem(table.CurrentRecipe.Result.UniqueIndex);
                    //GetSystemTimeAsFileTime(out end);
                    //span = end - start;
                    //if (span > 100000)
                    //    Debug.Log($"Took too long to check collection for {table.instStr()}. Took {span / 10000.0} ms");
                    //GetSystemTimeAsFileTime(out start);
                    var f = 0;
                    var fC = table.GetComponentInChildren<FuelNetwork>();
                    if (fC)
                        f = Math.Max(Math.Min(fC.Fuel.Missing(), itemCount.GetItemCount(fC.Fuel.fuelItem.UniqueIndex) - (KeepItem > 1 ? 1 : 0)), 0);
                    //GetSystemTimeAsFileTime(out end);
                    //span = end - start;
                    //if (span > 100000)
                    //    Debug.Log($"Took too long to check fuel for {table.instStr()}. Took {span / 10000.0} ms");
                    if (cooked || collecting || (f != 0 && fueling))
                    {
                        fueling = f != 0;
                        if (fueling)
                            fuel = new Cost(fC.Fuel.fuelItem, f);
                        else
                            fuel = null;
                        recipe = null;
                        requiredItem = null;
                        if (table.CurrentRecipe == null)
                        {
                            recipe = cooked;
                            fueling = false;
                        }
                        else if (collecting)
                        {
                            requiredItem = new Cost(collectionItem, Math.Min((int)table.Portions, itemCount.GetItemCount(collectionItem.UniqueIndex) - (KeepItem > 1 ? 1 : 0)));
                            fueling = false;
                        }
                        target = table;
                        targetOffset = Vector3.up * 0.4f;
                        dist = newDist;
                        targeted[this] = table;
                    }
                }
            Recipe = null;
            if (target)
            {
                if (requiredItem != null && requiredItem.amount > 0)
                {
                    itemCount.Forget(requiredItem.item.UniqueIndex);
                    holding.AddRange(box.GetInventoryReference().TakeItems(requiredItem.item, requiredItem.amount));
                    itemCount.CloseWithoutClear();
                }
                else if (recipe)
                {
                    Recipe = recipe.Recipe;
                    foreach (var ca in recipe.Recipe.RecipeCost)
                        foreach (var c in ca.items)
                            itemCount.Forget(c.UniqueIndex);
                    holding.AddRange(box.GetInventoryReference().TakeItems(recipe.Recipe.RecipeCost));
                    itemCount.CloseWithoutClear();
                }
                if (fuel != null && fuel.amount != 0)
                {
                    itemCount.Forget(fuel.item.UniqueIndex);
                    holding.AddRange(box.GetInventoryReference().TakeItems(fuel.item, fuel.amount));
                    itemCount.CloseWithoutClear();
                }
            }
        }
        internal override bool tryInteract()
        {
            if (targetObject == null)
                return true;
            if (targetObject == box.transform)
                return tryDepositHeld();
            if (targetObject.IsRepelled() || box.transform.IsRepelled())
                return true;
            var target = targeted[this];
            var f = targetObject.GetComponentInChildren<FuelNetwork>();
            if (!(f && holding.Contains(f.Fuel.fuelItem) && !f.Fuel.HasMaxFuel()) && (Recipe == null ? target.Portions == 0 : target.CurrentRecipe))
            {
                targeted[this] = null;
                return true;
            }
            if (SqrDistanceFromTarget > sqrInteractDistance)
                return false;
            if (f && holding.Contains(f.Fuel.fuelItem) && !f.Fuel.HasMaxFuel())
            {
                var m = Math.Min(f.Fuel.Missing(), holding.GetCount(f.Fuel.fuelItem));
                AddFuel(target, m);
                holding.Remove(f.Fuel.fuelItem, m, true);
            }
            if (Recipe)
            {
                var removed = holding.Remove(Recipe.RecipeCost);
                var items = new List<Item_Base>();
                foreach (var i in removed)
                    for (int j = 0; j < i.Amount; j++)
                        items.Add(i.baseItem);
                holding.AddRange(ForceStart(target, Recipe, items));
            }
            else if (target.Portions != 0)
            {
                var item = target.PickupFoodItem();
                var amount = (int)target.Portions;
                if (item)
                {
                    amount = Math.Min(holding.GetCount(item), amount);
                    holding.Remove(item, amount);
                }
                holding.Add(new Cost(target.CurrentRecipe.Result, amount));
                RemoveTablePortions(target,amount);
            }
            targeted[this] = null;
            return true;
        }

        static void AddFuel(CookingTable table, int amount)
        {
            var msg = new Message_Sprite_AddFuel(table, amount);
            msg.Use();
            msg.Message.Broadcast();
        }

        static List<ItemInstance> ForceStart(CookingTable table, SO_CookingTable_Recipe recipe, List<Item_Base> itemsToUse)
        {
            var result = table.ForceStart(recipe, itemsToUse);
            var msg = new Message_Sprite_InsertTable(table, recipe);
            msg.Message.Broadcast();
            return result;
        }

        static void RemoveTablePortions(CookingTable table, int amount)
        {
            var msg = new Message_Sprite_ClearTable(table,amount);
            msg.Use();
            msg.Message.Broadcast();
        }

        public override void RestoreData(string data)
        {
            if (targetObject)
                targeted[this] = targetObject.GetComponent<CookingTable>();
            Recipe = GetRecipeFromIndex(int.Parse(data));
        }

        public override string GenerateData() => Recipe ? Recipe.RecipeIndex.ToString() : "-1";

        public override MonoBehaviour_ID FindTarget(uint objectIndex)
        {
            foreach (var table in tables)
                if (table.ObjectIndex == objectIndex)
                    return table;
            return base.FindTarget(objectIndex);
        }
    }

    class CleanerSprite : HelperSprite
    {
        static Dictionary<CleanerSprite, MonoBehaviour_ID> targeted = new Dictionary<CleanerSprite, MonoBehaviour_ID>();
        public static Dictionary<Transform, MonoBehaviour> fishNetCache = new Dictionary<Transform, MonoBehaviour>();
        public static Dictionary<Transform, MonoBehaviour> dredgersCache = new Dictionary<Transform, MonoBehaviour>();
        public override float MaxDistance => base.MaxDistance * CleanerRange;
        public CleanerSprite(Storage_Small container, Slot slot, uint objectIndex = 0) : base(container, slot, new Color(Random.Range(0.875f, 1), Random.Range(0.875f, 1), 0), objectIndex)
        {
            targeted.Add(this, null);
        }
        public override void Kill()
        {
            targeted.Remove(this);
            base.Kill();
        }

        internal override void pickTarget(out MonoBehaviour_ID target, out Vector3 targetOffset)
        {
            target = null;
            targetOffset = Vector3.zero;
            float dist = MaxDistance * MaxDistance;
            targeted[this] = null;
            var machineCache = new Dictionary<string, bool>();
            var machinesCache = new Dictionary<string, bool>();
            var storage = GetStorageItems();
            if (seagulls != null && (!EC_Clean || storage.HasItem("Feather")))
                foreach (var seagull in seagulls)
                {
                    if (seagull == null || targeted.ContainsValue(seagull))
                        continue;
                    var newDist = (box.transform.position - seagull.transform.position).sqrMagnitude;
                    if (newDist < dist && seagull.currentState == SeagullState.Peck)
                    {
                        target = seagull;
                        targetOffset = Vector3.up * 0.2f;
                        dist = newDist;
                        targeted[this] = seagull;
                    }
                }
            if (!target)
            {
                if (!EC_Clean || storage.HasItem("Placeable_CollectionNet") || storage.HasItem("Placeable_CollectionNet_Advanced"))
                    foreach (var net in itemnets)
                    {
                        if (net == null || targeted.ContainsValue(net) || net.transform.IsRepelled())
                            continue;
                        var newDist = (box.transform.position - net.transform.position).sqrMagnitude;
                        if (newDist < dist && net.GetComponentInChildren<ItemCollector>(true).collectedItems.Count != 0)
                        {
                            target = net;
                            targetOffset = Vector3.up * 0.2f;
                            dist = newDist;
                            targeted[this] = net;
                        }
                    }
                if (ActiveFishingNets() != null && (!EC_Clean || storage.HasItem("Placeable_FishingNet")))
                    foreach (var net in ActiveFishingNets())
                    {
                        if (!net)
                            continue;
                        var block = net.FishingNet_Block();
                        if (!block || targeted.ContainsValue(block) || block.transform.IsRepelled())
                            continue;
                        var newDist = (box.transform.position - block.transform.position).sqrMagnitude;
                        if (newDist < dist && net.FishingNet_FishCount() != 0)
                        {
                            fishNetCache[block.transform] = net;
                            target = block;
                            targetOffset = Vector3.zero;
                            dist = newDist;
                            targeted[this] = block;
                        }
                    }
                if (dredgers.Count > 0 && (!EC_Clean || storage.HasItem("Dredger")))
                    foreach (var dredger in dredgers)
                    {
                        if (!dredger || targeted.ContainsValue(dredger) || dredger.transform.IsRepelled())
                            continue;
                        var newDist = (box.transform.position - dredger.transform.position).sqrMagnitude;
                        if (newDist < dist && !dredger.Dredger_IsRising() && !dredger.Dredger_IsDropped() && dredger.Dredger_CurrentItem())
                        {
                            target = dredger;
                            targetOffset = Vector3.zero;
                            dist = newDist;
                            targeted[this] = dredger;
                        }
                    }
            }
        }
        internal override bool tryInteract()
        {
            if (targetObject == null)
                return true;
            if (targetObject == box.transform)
                return tryDepositHeld();
            if (targetObject.IsRepelled() || box.transform.IsRepelled())
                return true;
            var seagull = targetObject.GetComponent<Seagull>();
            var net = targetObject.GetComponentInChildren<ItemCollector>(true);
            var fisher = GetFishNetFromBlock(targetObject);
            var dredger = dredger_network != null && dredger_network.IsAssignableFrom(targeted[this].GetType());
            if ((net && net.collectedItems.Count == 0) || (seagull && seagull.currentState != SeagullState.Peck))
            {
                targeted[this] = null;
                return true;
            }
            if (SqrDistanceFromTarget > sqrInteractDistance * 2)
                return false;
            if (seagull)
            {
                seagull.entityComponent.Damage(5f, seagull.transform.position, Vector3.down, EntityType.Enemy);
                if (seagull.entityComponent.IsDead)
                {
                    holding.AddRange(seagull.pickupItemChanneling.pickupItem.GetAllItems());
                    seagull.pickupItem.networkID.Remove();
                }
            }
            else if (net)
            {
                foreach (var pickup in net.collectedItems)
                    if (pickup)
                    {
                        var newItems = pickup.PickupItem.GetAllItems();
                        foreach (var item in newItems)
                            if (item.baseItem && item.settings_recipe.IsBlueprint)
                                ComponentManager<Inventory_ResearchTable>.Value.ResearchBlueprint(item.baseItem);
                        holding.AddRange(newItems);
                    }
                ClearAll(net);
            }
            else if (fisher)
            {
                foreach (var item in fisher.FishingNet_CaughtFishItem())
                    if (item)
                        holding.Add(item);
                    else
                        break;
                ClearFish(targetObject.GetComponent<Block>());
            }
            else if (dredger)
            {
                var station = targeted[this];
                if (!station.Dredger_IsRising() && !station.Dredger_IsDropped() && station.Dredger_CurrentItem())
                {
                    holding.Add(LookupItem(station.Dredger_CurrentItem().UniqueName)); // Have to lookup the item before adding to the sprite's inventory because the dredgers use empty fake items instead of the real items
                    station.Dredger_PickupItem(null);
                }
            }
            targeted[this] = null;
            return true;
        }

        static void ClearAll(ItemCollector collector)
        {
            var msg = new Message_Sprite_ClearAll(collector);
            msg.Use();
            msg.Message.Broadcast();
        }

        static void ClearFish(Block net)
        {
            var msg = new Message_Sprite_ClearFish(net);
            msg.Use();
            msg.Message.Broadcast();
        }

        public override void RestoreData(string data)
        {
            if (targetObject)
                targeted[this] = targetObject.GetComponent<MonoBehaviour_ID>();
        }

        public override MonoBehaviour_ID FindTarget(uint objectIndex)
        {
            if (seagulls != null)
                foreach (var seagull in seagulls)
                    if (seagull.ObjectIndex == objectIndex)
                        return seagull;
            foreach (var net in itemnets)
                if (net.ObjectIndex == objectIndex)
                    return net;
            foreach (var block in BlockCreator.GetPlacedBlocks())
                if (block.ObjectIndex == objectIndex)
                    return block;
            return base.FindTarget(objectIndex);
        }

        public static MonoBehaviour GetFishNetFromBlock(Transform block)
        {
            if (fishingnet != null && block.GetComponent<Block>()?.GetComponent(fishingnet) is MonoBehaviour b)
                return b;
            return null;
        }
    }

    class MechanicSprite : HelperSprite
    {
        static Dictionary<MechanicSprite, TankAccess> targeted = new Dictionary<MechanicSprite, TankAccess>();
        public override float MaxDistance => base.MaxDistance * MechanicRange;
        public MechanicSprite(Storage_Small container, Slot slot, uint objectIndex = 0) : base(container, slot, new Color(0, Random.Range(0.125f, 0.250f), Random.Range(0.875f, 1)), objectIndex)
        {
            targeted.Add(this, null);
        }
        public override void Kill()
        {
            targeted.Remove(this);
            base.Kill();
        }

        internal override void pickTarget(out MonoBehaviour_ID target, out Vector3 targetOffset)
        {
            target = null;
            targetOffset = Vector3.zero;
            float dist = MaxDistance * MaxDistance;
            targeted[this] = null;
            Item_Base collectionItem = null;
            var storage = GetStorageItems();
            if (tanks != null)
                foreach (var tank in tanks)
                {
                    if (tank.Tank == null || targeted.ContainsValue(tank) || tank.Tank.transform.IsRepelled())
                        continue;
                    var newDist = (box.transform.position - tank.Receiver.transform.position).sqrMagnitude;
                    if (newDist >= dist)
                        continue;
                    var collect = tank.HasOutput;
                    var put = tank.HasInput;
                    if (collect == put)
                        continue;
                    Item_Base newCollectionItem = null;
                    collect = collect && !tank.IsConnectedToPipe && tank.Tank.IsConsideredHarvestable() && tank.HarvestAmount > 0;
                    put = put && !tank.Tank.IsFull;
                    if (put && !box.IsOpen)
                        newCollectionItem = tank.acceptableTypes.Find((x) => (KeepItem > 1 ? storage.GetItemCount(x.UniqueIndex) > 1 : storage.HasItem(x.UniqueIndex)) && tank.Tank.FuelValue(x) + tank.Tank.CurrentTankAmount <= tank.Tank.maxCapacity);
                    if (collect || (put && newCollectionItem))
                    {
                        collectionItem = newCollectionItem;
                        target = tank.Receiver;
                        targetOffset = Vector3.up * 0.2f + tank.Receiver.transform.InverseTransformPoint(tank.Tank.transform.position);
                        dist = newDist;
                        targeted[this] = tank;
                    }
                }
            if (target && collectionItem)
            {
                var tank = targeted[this].Tank;
                int c = storage.GetItemCount(collectionItem.UniqueIndex);
                var p = new List<ItemInstance>();
                foreach (var s in box.GetInventoryReference().allSlots)
                    if (s.HasValidItemInstance() && s.itemInstance.UniqueIndex == collectionItem.UniqueIndex && !p.Contains(s.itemInstance))
                    {
                        p.Add(s.itemInstance);
                        c += s.itemInstance.UsesInStack;
                    }
                c = Math.Min((int)((tank.maxCapacity - tank.CurrentTankAmount) / tank.FuelValue(collectionItem)), c);
                storage.Forget(collectionItem.UniqueIndex);
                holding.AddRange(box.GetInventoryReference().TakeItemUses(collectionItem, c, KeepItem > 1));
                storage.CloseWithoutClear();
            }
        }
        internal override bool tryInteract()
        {
            if (targetObject == null)
                return true;
            if (targetObject == box.transform)
                return tryDepositHeld();
            if (targetObject.IsRepelled() || box.transform.IsRepelled())
                return true;
            var tank = targeted[this];
            if (tank == null)
                return true;
            int c = 0;
            Item_Base item = null;
            if (tank.HasInput)
            {
                foreach (var hold in holding)
                {
                    if (!tank.acceptableTypes.Contains(hold.baseItem))
                        continue;
                    var t = (int)((tank.Tank.maxCapacity - tank.Tank.CurrentTankAmount) / tank.Tank.FuelValue(hold.baseItem));
                    if (t > 0)
                    {
                        c = t;
                        item = hold.baseItem;
                        break;
                    }
                }
                if (item != null)
                    c = Math.Min(c, holding.Join((x) => (x.UniqueName == item.UniqueName) ? (item.MaxUses > 1 ? x.UsesInStack : x.Amount) : 0, (x, y) => x + y));
            }
            else if (tank.Tank.IsConsideredHarvestable())
            {
                item = tank.OutputItem;
                c = tank.HarvestAmount;
            }
            if (c == 0)
            {
                targeted[this] = null;
                return true;
            }
            if (SqrDistanceFromTarget > sqrInteractDistance)
                return false;
            if (tank.HasInput)
            {
                tank.Tank.ModifyTankRPC(c * tank.Tank.FuelValue(item), null);
                holding.RemoveUses(item, c);
            }
            else
            {
                tank.Tank.ModifyTankRPC(-tank.Tank.CurrentTankAmount, null);
                holding.Add(new ItemInstance(item, c, item.MaxUses));
            }
            targeted[this] = null;
            return true;
        }

        public override void RestoreData(string data)
        {
            if (string.IsNullOrEmpty(data))
                return;
            var id = int.Parse(data);
            foreach (var tank in tanks)
                if (tank.Receiver.transform == targetObject && tank.Tank.tankID == id)
                    targeted[this] = tank;
        }
        public override string GenerateData()
        {
            return targeted[this] != null ? targeted[this].Tank.tankID.ToString() : null;
        }

        public override MonoBehaviour_ID FindTarget(uint objectIndex)
        {
            if (tanks != null)
                foreach (var tank in tanks)
                    if (tank.Receiver.ObjectIndex == objectIndex)
                        return tank.Receiver;
            return base.FindTarget(objectIndex);
        }
    }

    class SpriteRenderer : MonoBehaviour
    {
        public Light glow;
        void Update()
        {
            if (glow.enabled != LightMode > 0)
                glow.enabled = LightMode > 0;
            if (glow.shadows == LightShadows.None == LightMode > 1)
                glow.shadows = LightMode > 1 ? LightShadows.Hard : LightShadows.None;
        }
    }

    class ModLoadException : Exception
    {
        public ModLoadException(string message) : base(message) { }
    }

    [HarmonyPatch(typeof(ModManagerPage), "ShowModInfo")]
    class Patch_ShowModInfo
    {
        static void Postfix(ModData md)
        {
            if (md.modinfo.mainClass && md.modinfo.mainClass.GetType() == typeof(BenevolentSprites))
                ModManagerPage.modInfoObj.transform.Find("MakePermanent").gameObject.SetActive(true);
        }
    }

    [HarmonyPatch(typeof(Storage_Small), "Close")]
    class Patch_CloseStorage
    {
        static void Postfix(Storage_Small __instance)
        {
            if (gameLoaded && Raft_Network.IsHost)
            {
                HelperSprite.GetStorageItems(__instance).Clear();
                foreach (var slot in __instance.GetInventoryReference().allSlots)
                    if (slot.HasSprite())
                    {
                        var s = slot.GetSprite();
                        if (s == null || s.dying)
                        {
                            if (slot.itemInstance.TryGetValue(dataKey, out var data))
                                CreateSprite(data);
                            else
                                CreateSprite(__instance, slot);
                        }
                    }
            }
        }
    }

    [HarmonyPatch(typeof(GameManager), "OnWorldRecievedLate")]
    class Patch_RemoteWorldLoaded
    {
        static void Postfix(Storage_Small __instance)
        {
            gameLoaded = true;
            int i = -1;
            string str = "";
            foreach (var message in waiting)
                try
                {
                    i++;
                    ProcessMessage(message);
                }
                catch (Exception e)
                {
                    str += "\n" + i + "/" + waiting.Count + "\n | Message type: " + MessageValues.getName(message.appBuildID) + "\n | Stacktrace : " + e.StackTrace;
                }
            if (str.Length != 0)
                Debug.LogWarning("Some queued messages failed:" + str);
            waiting.Clear();
        }
    }

    [HarmonyPatch(typeof(RGD_Slot), MethodType.Constructor, typeof(Slot), typeof(int))]
    class Patch_SaveSlot
    {
        static void Prefix(ref bool __state, Slot slot)
        {
            if (slot == null || !slot.HasValidItemInstance() || !Environment.StackTrace.Contains("SaveWorld"))
                return;
            var sprite = slot.GetSprite();
            __state = sprite != null && !sprite.dying;
            if (__state)
                slot.itemInstance.SetValue(dataKey, new Message_Sprite_Recreate(sprite, true).Message.password);
            //Debug.Log("Sprite data: " + slot.itemInstance.exclusiveString.Length);
        }
        static void Postfix(bool __state, Slot slot)
        {
            if (__state)
                slot.itemInstance.RemoveValue(dataKey);
        }
    }

    [HarmonyPatch(typeof(Slot), "RefreshComponents")]
    class Patch_ChangeSlot
    {
        public class store<T> { public T value; public static implicit operator T(store<T> v) => v.value; public static implicit operator store<T>(T v) => new store<T>() { value = v }; }
        public static Dictionary<Slot, store<ItemInstance>> prevs = new Dictionary<Slot, store<ItemInstance>>();
        static void Prefix(Slot __instance)
        {
            if (prevs.TryGetValue(__instance, out var v))
            {
                if (v.value != __instance.itemInstance)
                {
                    if (v.value?.baseItem?.UniqueName != null)
                        Clear(v.value);
                    if (__instance.itemInstance?.baseItem?.UniqueName != null)
                        Clear(__instance.itemInstance);
                    v.value = __instance.itemInstance;
                }
            }
            else
            {
                if (__instance.itemInstance?.baseItem?.UniqueName != null)
                    Clear(__instance.itemInstance);
                prevs[__instance] = __instance.itemInstance;
            }
        }
        static void Clear(ItemInstance item)
        {
            if (item.UniqueName.IsSprite() && gameLoaded)
            {
                item.RemoveValue(dataKey);
            }
        }
    }

    [HarmonyPatch(typeof(SoundManager), "PlayUI_MoveItem")]
    class Patch_PlayMoveItemSound
    {
        public static bool disable = false;
        static bool Prefix() => !disable;
    }

    [HarmonyPatch(typeof(AI_NetworkBehaviour_Domestic_Resource))]
    class Patch_Domestic_Resource
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void Start(AI_NetworkBehaviour_Domestic_Resource __instance) => animals.Add(__instance);


        [HarmonyPatch("OnDestroy")]
        [HarmonyPostfix]
        static void OnDestroy(AI_NetworkBehaviour_Domestic_Resource __instance) => animals.Remove(__instance);
    }

    [HarmonyPatch(typeof(NetworkIDManager), new[] { typeof(MonoBehaviour_ID_Network), typeof(Type), typeof(NetworkIDTag) })]
    class Patch_NetworkedObject
    {
        [HarmonyPatch("AddNetworkID")]
        [HarmonyPostfix]
        static void Add(MonoBehaviour_ID_Network networkID, NetworkIDTag tag)
        {
            if (networkID is Seagull)
                seagulls.Add((Seagull)networkID);
            if (tag == NetworkIDTag.ChickenEgg && networkID is PickupItem_Networked)
                eggs.Add((PickupItem_Networked)networkID);
        }


        [HarmonyPatch("RemoveNetworkID")]
        [HarmonyPostfix]
        static void Remove(MonoBehaviour_ID_Network networkID, NetworkIDTag tag)
        {
            if (networkID is Seagull)
                seagulls.Remove((Seagull)networkID);
            if (tag == NetworkIDTag.ChickenEgg && networkID is PickupItem_Networked)
                eggs.Remove((PickupItem_Networked)networkID);
        }
    }

    [HarmonyPatch(typeof(Block), "OnFinishedPlacement")]
    class Patch_Block
    {
        static HashSet<string> fails = new HashSet<string>();
        static void Postfix(Block __instance)
        {
            if (__instance is Block_Foundation_ItemNet)
                itemnets.Add((Block_Foundation_ItemNet)__instance);
            if (__instance.GetComponent<BeeHive>())
                beehives.Add(__instance.GetComponent<BeeHive>());
            if (__instance.GetComponent<CookingTable_Recipe_UI>())
                recipes.Add(__instance.GetComponent<CookingTable_Recipe_UI>());
            if (__instance.GetComponent<CookingTable>())
                tables.Add(__instance.GetComponent<CookingTable>());
            if (__instance.GetComponent<Block_CookingStand>())
                stands.Add(__instance.GetComponent<Block_CookingStand>());
            foreach (var c in __instance.GetComponents<MonoBehaviour_ID_Network>())
            {
                foreach (var ta in c.FindAll<Tank[]>())
                    foreach (var t in ta)
                        if (t != null && !tanks.Exists((x) => x.Tank == t))
                            try { tanks.Add(new TankAccess(t)); } catch (Exception e) { if (fails.Add(t + " on " + __instance)) Debug.LogError("Failed to generate access data for " + t + " on " + __instance + "\n" + e); }
                foreach (var t in c.FindAll<Tank>())
                    if (t != null && !tanks.Exists((x) => x.Tank == t))
                        try { tanks.Add(new TankAccess(t)); } catch (Exception e) { if (fails.Add(t + " on " + __instance)) Debug.LogError("Failed to generate access data for " + t + " on " + __instance + "\n" + e); }
            }
            if (dredger_network != null && __instance.networkedBehaviour && dredger_network.IsAssignableFrom(__instance.networkedBehaviour.GetType()))
                dredgers.Add(__instance.networkedBehaviour);
        }
    }

    [HarmonyPatch]
    class Patch_InventoryOverrides
    {
        public static GameObject ignore = null;
        [HarmonyPatch(typeof(Hotbar), "ReselectCurrentSlot")]
        [HarmonyPrefix]
        static bool reselect(Hotbar __instance) => __instance.gameObject != ignore;
        [HarmonyPatch(typeof(InventoryPickup), "ShowItem")]
        [HarmonyPrefix]
        static bool show(InventoryPickup __instance) => __instance.gameObject != ignore;
        [HarmonyPatch(typeof(PlayerInventory), "HandleNewItemAdded")]
        [HarmonyPrefix]
        static bool handle(PlayerInventory __instance) => __instance.gameObject != ignore;
    }

    [HarmonyPatch(typeof(PlayerAnimator), "SetAnimation", typeof(PlayerAnimation), typeof(bool))]
    class Patch_PlayerAnimator
    {
        public static bool disable = false;
        static bool Prefix() => !disable;
    }

    [HarmonyPatch(typeof(Block_CookingStand), "GetCookingSlotsForItem")]
    class Patch_GetCookingSlots
    {
        public static bool spriteCall = false;
        static bool Prefix(Block_CookingStand __instance, Block_CookingStand.CookingSlotCollection[] ___cookingSlotCollections, Item_Base itemToInsert, out CookingSlot[] __result)
        {
            bool CanInsert(CookingSlot slot) => !slot.IsBusy && slot.CanCookItem(itemToInsert) && (!spriteCall || !FireSprite.IsTargeted(slot));
            if (itemToInsert)
            {
                if (itemToInsert.settings_cookable.CookingSlotsRequired <= 1)
                    for (int i = 0; i < __instance.cookingSlots.Length; i++)
                    {
                        if (CanInsert(__instance.cookingSlots[i]))
                        {
                            __result = new[] { __instance.cookingSlots[i] };
                            return false;
                        }
                    }
                else
                    foreach (var col in ___cookingSlotCollections)
                    {
                        var f = new List<CookingSlot>();
                        for (int i = 0; i < col.cookingSlots.Length; i++)
                        {
                            if (CanInsert(col.cookingSlots[i]))
                                f.Add(col.cookingSlots[i]);
                            else
                                f.Clear();
                            if (f.Count == itemToInsert.settings_cookable.CookingSlotsRequired)
                            {
                                __result = f.ToArray();
                                return false;
                            }
                        }
                    }
            }
            __result = null;
            return false;
        }
    }

    [HarmonyPatch(typeof(LanguageSourceData), "GetLanguageIndex")]
    static class Patch_GetLanguageIndex
    {
        static void Postfix(LanguageSourceData __instance, ref int __result)
        {
            if (__result == -1 && __instance == language)
                __result = 0;
        }
    }

    [HarmonyPatch(typeof(CookingTable), "StartCooking")]
    static class Patch_StartCookingTable
    {
        public static bool forceStart = false;
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = instructions.ToList();
            code.Insert(code.FindIndex(x => x.operand is MethodInfo m && m.Name == "CanStartCooking") + 1,new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(Patch_StartCookingTable),nameof(OverrideCanStart))));
            return code;
        }
        static bool OverrideCanStart(bool original) => original || forceStart;
    }

    [HarmonyPatch(typeof(CookingTable_Slot), "ThrowItemIntoPotAnimation")]
    static class Patch_ThrowCookingTableItem
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator iL)
        {
            var code = instructions.ToList();
            var label = iL.DefineLabel();
            code[0].labels.Add(label);
            code.InsertRange(0, new[] {
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(Patch_StartCookingTable),"forceStart")),
                new CodeInstruction(OpCodes.Brfalse_S,label),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Callvirt,AccessTools.Method(typeof(CookingTable_Slot),"ClearItem")),
                new CodeInstruction(OpCodes.Ret)
            });
            return code;
        }
    }

    [Serializable]
    public class RGD_CookingSlotTargets
    {
        public int[] slotInds;
        public uint objInd;
        public RGD_CookingSlotTargets(CookingSlotTargets targets)
        {
            slotInds = targets.slotInds;
            objInd = targets.ObjectIndex;
        }
        public CookingSlotTargets GetReal() => new CookingSlotTargets() { slotInds = slotInds, ObjectIndex = objInd };
    }
    public struct CookingSlotTargets
    {
        public static CookingSlotTargets Null = new CookingSlotTargets() { slotInds = new int[0], stand = null };
        public int[] slotInds;
        public Block_CookingStand stand;
        public uint ObjectIndex
        {
            get => stand == null ? 0 : stand.ObjectIndex;
            set
            {
                foreach (var s in stands)
                    if (s.ObjectIndex == value)
                    {
                        stand = s;
                        return;
                    }
            }
        }
        public CookingSlot Slot
        {
            get => stand && slotInds != null && slotInds.Length > 0 ? stand.cookingSlots[slotInds[0]] : null;
            set
            {
                if (value == null)
                {
                    stand = null;
                    slotInds = new int[0];
                    return;
                }
                if (stand)
                    slotInds = new[] { stand.GetIndexOfCookingSlot(value) };
                if (slotInds == null || slotInds.Length == 0 || slotInds[0] == -1)
                {
                    var nStand = value.GetComponentInParent<Block_CookingStand>();
                    if (nStand && nStand != stand)
                    {
                        slotInds = new[] { nStand.GetIndexOfCookingSlot(value) };
                        stand = nStand;
                    }
                }
                if (slotInds == null || slotInds.Length == 0 || slotInds[0] == -1)
                    throw new NullReferenceException();
            }
        }
        public List<CookingSlot> Slots
        {
            get
            {
                var slots = new List<CookingSlot>();
                if (stand && slotInds != null)
                    foreach (var i in slotInds)
                        slots.Add(stand.cookingSlots[i]);
                return slots;
            }
            set
            {
                if (value == null || value.Count == 0)
                {
                    stand = null;
                    slotInds = new int[0];
                    return;
                }
                var nStand = stand;
                if (!stand || stand.GetIndexOfCookingSlot(value[0]) == -1)
                    nStand = value[0].GetComponentInParent<Block_CookingStand>();
                if (!nStand)
                    throw new IndexOutOfRangeException();
                var inds = new int[value.Count];
                for (int i = 0; i < value.Count; i++)
                {
                    inds[i] = nStand.GetIndexOfCookingSlot(value[i]);
                    if (inds[i] == -1)
                        throw new IndexOutOfRangeException();
                }
                stand = nStand;
                slotInds = inds;
            }
        }
        public CookingSlotTargets(Block_CookingStand Stand, int SlotIndex)
        {
            stand = Stand;
            slotInds = new[] { SlotIndex };
        }
        public CookingSlotTargets(uint objectIndex, int SlotIndex) : this(null, SlotIndex)
        {
            ObjectIndex = objectIndex;
        }
        public CookingSlotTargets(CookingSlot slot)
        {
            stand = null;
            slotInds = null;
            Slot = slot;
        }
        public CookingSlotTargets(List<CookingSlot> slots)
        {
            stand = null;
            slotInds = null;
            Slots = slots;
        }

        public override bool Equals(object obj)
        {
            if (obj is CookingSlotTargets)
                return Equals((CookingSlotTargets)obj);
            return base.Equals(obj);
        }

        public bool Equals(CookingSlotTargets target)
        {
            //Debug.Log($"{target.ObjectIndex} == {ObjectIndex} && [{target.slotInds.Join()}] overlaps [{slotInds.Join()}]");
            if (ObjectIndex != target.ObjectIndex)
                return false;
            var len = slotInds.Length;
            if ((len == 0) != (target.slotInds.Length == 0))
                return false;
            if (len == 0)
                return true;
            unsafe
            {
                fixed (int* a1 = slotInds)
                fixed (int* a2 = target.slotInds)
                {
                    var e1 = a1 + len;
                    var e2 = a2 + target.slotInds.Length;
                    for (var p1 = a1; p1 < e1; p1++)
                        for (var p2 = a2; p2 < e2; p2++)
                            if (*p1 == *p2)
                                return true;
                }
            }
            return false;
        }
        public override int GetHashCode() => ObjectIndex.GetHashCode();

        public static implicit operator CookingSlotTargets(CookingSlot slot) => new CookingSlotTargets(slot);
        public static implicit operator CookingSlotTargets(List<CookingSlot> slots) => new CookingSlotTargets(slots);
    }
    public class TankAccess
    {
        MonoBehaviour_ID_Network receiver;
        Tank tank;
        Placeable_Extractor extractor;
        PipeSocket_Tank[] pipes;
        bool isExtractorOutput;

        public MonoBehaviour_ID_Network Receiver => receiver;
        public Tank Tank => tank;
        public List<Item_Base> acceptableOutputTypes => GetOutput(tank);
        public List<Item_Base> acceptableTypes => GetInput(tank);
        public float currentTankAmount { get => GetAmount(tank); set => tank.ModifyTankRPC(value - GetAmount(tank), null); }
        public Tank.TankAcceptance tankAcceptance => GetAcceptance(tank);
        public bool HasOutput => isExtractorOutput || (tank.TankHasOutput() && tankAcceptance == Tank.TankAcceptance.Output);
        public bool HasInput => tank.TankHasInput() && tankAcceptance == Tank.TankAcceptance.Input;
        public bool IsConnectedToPipe => pipes != null && pipes.Any(x => x?.pipeGroup?.GetPipes()?.Count > 1);
        public int HarvestAmount => extractor ? GetHarvestAmount(extractor) : 0;
        public Item_Base OutputItem => GetOutputItem(extractor);
        public TankAccess(Tank Tank) : this(GetReciever(Tank), Tank) { }
        public TankAccess(MonoBehaviour_ID_Network Reciever, Tank Tank)
        {
            receiver = Reciever;
            tank = Tank;
            extractor = receiver.GetComponent<Placeable_Extractor>();
            if (extractor)
                isExtractorOutput = extractor.outputTank == tank;
            pipes = tank.GetComponentInParent<Block>()?.GetComponentsInChildren<PipeSocket_Tank>().ToList().FindAll(x => x.socketType.ToString().StartsWith("Output_") && x.connectedTank == tank).ToArray();
        }

        [Accessor(AccessorType.Field, typeof(Tank), "messageReciever")]
        static MonoBehaviour_ID_Network GetReciever(Tank tank) => default;
        [Accessor(AccessorType.Field, typeof(Tank),"acceptableOutputTypes")]
        static List<Item_Base> GetOutput(Tank tank) => default;
        [Accessor(AccessorType.Field, typeof(Tank), "acceptableTypes")]
        static List<Item_Base> GetInput(Tank tank) => default;
        [Accessor(AccessorType.Field, typeof(Tank), "currentTankAmount")]
        static float GetAmount(Tank tank) => default;
        [Accessor(AccessorType.Field, typeof(Tank), "tankAcceptance")]
        static Tank.TankAcceptance GetAcceptance(Tank tank) => default;
        [Accessor(AccessorType.Field, typeof(Placeable_Extractor), "itemTypeOnHarvest")]
        static Item_Base GetOutputItem(Placeable_Extractor extractor) => default;
        [Accessor(AccessorType.Method, typeof(Placeable_Extractor), "HarvestAmount")]
        static int GetHarvestAmount(Placeable_Extractor extractor) => default;
    }

    public static class MessageValues
    {
        public const int ChannelID = 8;
        public const Messages MessageID = (Messages)23468;
        public const int Create = 0;
        public const int Target = 1;
        public const int Kill = 2;
        public const int Recreate = 3;
        public const int PlantSeed = 4;
        public const int ClearCooker = 5;
        public const int InsertCooker = 6;
        public const int AddFuel = 7;
        public const int InsertTable = 8;
        public const int ClearTable = 9;
        public const int ClearAll = 10;
        public const int ClearFish = 11;
        public const int RequestMissing = 12;
        public static Dictionary<int, Type> MessageTypes = new Dictionary<int, Type>
        {
            [Create] = typeof(Message_Sprite_Create),
            [Target] = typeof(Message_Sprite_SetTarget),
            [Kill] = typeof(Message_Sprite_Kill),
            [Recreate] = typeof(Message_Sprite_Recreate),
            [PlantSeed] = typeof(Message_Sprite_PlantSeed),
            [ClearCooker] = typeof(Message_Sprite_ClearCooker),
            [InsertCooker] = typeof(Message_Sprite_InsertCooker),
            [AddFuel] = typeof(Message_Sprite_AddFuel),
            [InsertTable] = typeof(Message_Sprite_InsertTable),
            [ClearTable] = typeof(Message_Sprite_ClearTable),
            [ClearAll] = typeof(Message_Sprite_ClearAll),
            [ClearFish] = typeof(Message_Sprite_ClearFish),
            [RequestMissing] = typeof(Message_Sprite_RequestMissing)
        };
        public static Network_Player DummyPlayer = CreateFakePlayer();

        public static string getName(int msgId)
        {
            switch (msgId)
            {
                case Create:
                    return "Create Sprite";
                case Target:
                    return "Set Sprite Target";
                case Kill:
                    return "Kill Sprite";
                case Recreate:
                    return "Create Sprite from Serial";
                case PlantSeed:
                    return "Plant Seed";
                case ClearCooker:
                    return "Empty Cooking Stand Slots";
                case InsertCooker:
                    return "Set Cooking Stand Slots";
                case AddFuel:
                    return "Add Fuel";
                case InsertTable:
                    return "Insert Cooking Table Recipe";
                case ClearTable:
                    return "Empty Cooking Table";
                case ClearAll:
                    return "Empty Item Net";
                case ClearFish:
                    return "Empty Fishing Net";
                case RequestMissing:
                    return "Request Missing Sprites";
            }
            return "Unknown (" + msgId + ")";
        }
    }

    abstract class Message_Sprite
    {
        public Message_InitiateConnection Message => new Message_InitiateConnection(MessageValues.MessageID, MessageValues.MessageTypes.First(x => x.Value == this.GetType()).Key, JsonUtility.ToJson(this));
        public abstract void Use();
        public static Message_Sprite CreateFrom(Message_InitiateConnection message) => (Message_Sprite)JsonUtility.FromJson(message.password, MessageValues.MessageTypes[message.appBuildID]);
    }

    [Serializable]
    class Message_Sprite_Create : Message_Sprite
    {
        public Storage_Small Box
        {
            get
            {
                foreach (var box in StorageManager.allStorages)
                    if (box.ObjectIndex == boxIndex)
                        return box;
                Debug.LogWarning($"[Benevolent Sprites]: Failed to find box with index {boxIndex}");
                return null;
            }
        }
        public Slot Slot
        {
            get
            {
                var slots = Box?.GetInventoryReference()?.allSlots;
                if (slots != null && slotIndex >= 0 && slots.Count > slotIndex)
                    return slots[slotIndex];
                Debug.LogWarning($"[Benevolent Sprites]: Failed to find slot with index {slotIndex}");
                return null;
            }
        }
        public HelperSprite Sprite
        {
            get
            {
                foreach (var sprite in sprites)
                    if (sprite.ObjectIndex == spriteIndex)
                        return sprite;
                MissingIndecies.Add(spriteIndex);
                Debug.LogWarning($"[Benevolent Sprites]: Failed to find sprite with index {spriteIndex}");
                return null;
            }
        }
        public uint SpriteIndex => spriteIndex;
        [SerializeField]
        uint spriteIndex;
        [SerializeField]
        uint boxIndex;
        [SerializeField]
        int slotIndex;
        public Message_Sprite_Create(HelperSprite sprite)
        {
            spriteIndex = sprite.ObjectIndex;
            boxIndex = sprite.box.ObjectIndex;
            slotIndex = sprite.box.GetInventoryReference().allSlots.IndexOf(sprite.store);
        }
        public override void Use() => CreateSprite(Box, Slot, SpriteIndex);
    }

    [Serializable]
    class Message_Sprite_SetTarget : Message_Sprite
    {
        public Vector3 Offset => targetOffset;
        public MonoBehaviour_ID Target => Sprite.FindTarget(targetIndex);
        public HelperSprite Sprite
        {
            get
            {
                foreach (var sprite in sprites)
                    if (sprite.ObjectIndex == spriteIndex)
                        return sprite;
                MissingIndecies.Add(spriteIndex);
                Debug.LogWarning($"[Benevolent Sprites]: Failed to find sprite with index {spriteIndex}");
                return null;
            }
        }
        [SerializeField]
        uint spriteIndex;
        [SerializeField]
        uint targetIndex;
        [SerializeField]
        SerializableVector3 targetOffset;
        public Message_Sprite_SetTarget(HelperSprite sprite, MonoBehaviour_ID Target, Vector3 TargetOffset)
        {
            spriteIndex = sprite.ObjectIndex;
            targetIndex = Target.ObjectIndex;
            targetOffset = TargetOffset;
        }
        public override void Use() => Sprite.SetTargetNetwork(Target, Offset);
    }

    [Serializable]
    class Message_Sprite_Kill : Message_Sprite
    {
        public HelperSprite Sprite
        {
            get
            {
                foreach (var sprite in sprites)
                    if (sprite.ObjectIndex == spriteIndex)
                        return sprite;
                Debug.LogWarning($"[Benevolent Sprites]: Failed to find sprite with index {spriteIndex}");
                return null;
            }
        }
        [SerializeField]
        uint spriteIndex;
        public Message_Sprite_Kill(HelperSprite sprite)
        {
            spriteIndex = sprite.ObjectIndex;
        }
        public override void Use() => Sprite.Kill();
    }

    [Serializable]
    class Message_Sprite_Recreate : Message_Sprite_Create
    {
        public Vector3 Offset => targetOffset;
        public Vector3 Position => position;
        public uint TargetIndex => targetIndex;
        [SerializeField]
        uint targetIndex;
        [SerializeField]
        SerializableVector3 targetOffset;
        [SerializeField]
        SerializableVector3 position;
        [SerializeField]
        RGD_ItemInstance[] holding;
        [SerializeField]
        string saveData;
        public Message_Sprite_Recreate(HelperSprite sprite,bool saving) : base(sprite)
        {
            targetIndex = sprite.targetObject.GetComponent<MonoBehaviour_ID>().ObjectIndex;
            targetOffset = sprite.targetPosition;
            position = sprite.gameObject.transform.localPosition;
            if (saving)
            {
                holding = sprite.holding.Select(x => new RGD_ItemInstance(x)).ToArray();
                saveData = sprite.GenerateData();
            }
        }
        public override void Use()
        {
            base.Use();
            try
            {
                var sprite = Sprite;
                sprite.gameObject.transform.localPosition = Position;
                sprite.SetTarget(sprite.FindTarget(TargetIndex).transform, Offset);
                if (holding != null)
                    foreach (var item in holding)
                        sprite.holding.Add(item.GetRealInstance());
                if (saveData != null)
                    sprite.RestoreData(saveData);
            } catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }

    [Serializable]
    class Message_Sprite_PlantSeed : Message_Sprite
    {
        public Cropplot Plot
        {
            get
            {
                foreach (var plot in PlantManager.allCropplots)
                    if (plot.ObjectIndex == cropIndex)
                        return plot;
                Debug.LogWarning($"[Benevolent Sprites]: Failed to find crop with index {cropIndex}");
                return null;
            }
        }
        public int ItemIndex => itemIndex;
        public uint ObjectIndex => objectIndex;
        public bool WaterCrop => water;
        [SerializeField]
        uint cropIndex;
        [SerializeField]
        int itemIndex;
        [SerializeField]
        uint objectIndex;
        [SerializeField]
        bool water;
        public Message_Sprite_PlantSeed(Cropplot Plot, int ItemIndex, uint ObjectIndex, bool WaterCrop)
        {
            cropIndex = Plot.ObjectIndex;
            itemIndex = ItemIndex;
            objectIndex = ObjectIndex;
            water = WaterCrop;
        }
        public override void Use()
        {
            var crop = Plot;
            crop.plantManager().PlantSeed(crop, crop.plantManager().GetPlantByIndex(ItemIndex), ObjectIndex, WaterCrop, false);
        }
    }

    [Serializable]
    class Message_Sprite_ClearCooker : Message_Sprite
    {
        public Block_CookingStand Stand
        {
            get
            {
                foreach (var stand in stands)
                    if (stand.ObjectIndex == standIndex)
                        return stand;
                Debug.LogWarning($"[Benevolent Sprites]: Failed to find cooking stand with index {standIndex}");
                return null;
            }
        }
        public int Slot => slotIndex;
        [SerializeField]
        uint standIndex;
        [SerializeField]
        int slotIndex;
        public Message_Sprite_ClearCooker(Block_CookingStand Stand, CookingSlot Slot)
        {
            standIndex = Stand.ObjectIndex;
            slotIndex = Stand.GetIndexOfCookingSlot(Slot);
        }
        public override void Use()
        {
            var stand = Stand;
            Patch_PlayerAnimator.disable = true;
            stand.CollectItem(stand.cookingSlots[Slot], MessageValues.DummyPlayer);
            Patch_PlayerAnimator.disable = false;
        }
    }

    [Serializable]
    class Message_Sprite_InsertCooker : Message_Sprite
    {
        public CookingSlotTargets Slots => new CookingSlotTargets() { slotInds = slotInds, ObjectIndex = objInd };
        public Item_Base Item => ItemManager.GetItemByIndex(itemIndex);
        [SerializeField]
        int[] slotInds;
        [SerializeField]
        uint objInd;
        [SerializeField]
        int itemIndex;
        public Message_Sprite_InsertCooker(CookingSlotTargets Slots, Item_Base Item)
        {
            slotInds = Slots.slotInds;
            objInd = Slots.ObjectIndex;
            itemIndex = Item.UniqueIndex;
        }
        public override void Use()
        {
            Patch_PlayerAnimator.disable = true;
            Slots.stand.InsertItem(Item, Slots.Slots.ToArray(), MessageValues.DummyPlayer);
            Patch_PlayerAnimator.disable = false;
        }
    }

    [Serializable]
    class Message_Sprite_AddFuel : Message_Sprite
    {
        public Fuel Fuel
        {
            get
            {
                if (type == 1)
                    foreach (var stand in stands)
                        if (stand.ObjectIndex == objectIndex)
                            return stand.fuel;
                if (type == 2)
                    return NetworkIDManager.GetNetworkIDFromObjectIndex<CookingTable>(objectIndex).GetComponentInChildren<FuelNetwork>().Fuel;
                Debug.LogWarning($"[Benevolent Sprites]: Failed to find cooking stand or table with index {objectIndex}");
                return null;
            }
        }
        public int Amount => amount;
        [SerializeField]
        uint objectIndex;
        [SerializeField]
        int type;
        [SerializeField]
        int amount;
        public Message_Sprite_AddFuel(Block_CookingStand Stand, int Amount)
        {
            objectIndex = Stand.ObjectIndex;
            amount = Amount;
            type = 1;
        }
        public Message_Sprite_AddFuel(CookingTable Table, int Amount)
        {
            objectIndex = Table.ObjectIndex;
            amount = Amount;
            type = 2;
        }
        public override void Use() => Fuel.AddFuel(Amount);
    }

    [Serializable]
    class Message_Sprite_InsertTable : Message_Sprite
    {
        public CookingTable Table => NetworkIDManager.GetNetworkIDFromObjectIndex<CookingTable>(tableIndex);
        public SO_CookingTable_Recipe Recipe => GetRecipeFromIndex(recipeIndex);
        [SerializeField]
        uint tableIndex;
        [SerializeField]
        int recipeIndex;
        public Message_Sprite_InsertTable(CookingTable Table, SO_CookingTable_Recipe Recipe)
        {
            tableIndex = Table.ObjectIndex;
            recipeIndex = (int)Recipe.RecipeIndex;
        }
        public override void Use() => Table.ForceStart(Recipe);
    }

    [Serializable]
    class Message_Sprite_ClearTable : Message_Sprite
    {
        public CookingTable Table => NetworkIDManager.GetNetworkIDFromObjectIndex<CookingTable>(tableIndex);
        public int Amount => amount;
        [SerializeField]
        uint tableIndex;
        [SerializeField]
        int amount;
        public Message_Sprite_ClearTable(CookingTable Table, int Amount)
        {
            tableIndex = Table.ObjectIndex;
            amount = Amount;
        }
        public override void Use() => Table.Remove(Amount);
    }

    [Serializable]
    class Message_Sprite_ClearAll : Message_Sprite
    {
        public ItemCollector Collector
        {
            get
            {
                foreach (var collector in Object.FindObjectsOfType<ItemCollector>())
                    if (collector.ObjectIndex == collectorIndex)
                        return collector;
                Debug.LogWarning($"[Benevolent Sprites]: Failed to find collector with index {collectorIndex}");
                return null;
            }
        }
        [SerializeField]
        uint collectorIndex;
        public Message_Sprite_ClearAll(ItemCollector Collector) => collectorIndex = Collector.ObjectIndex;
        public override void Use() => Collector.ClearCollectedItems(null);
    }

    [Serializable]
    class Message_Sprite_ClearFish : Message_Sprite
    {
        public Block netBlock
        {
            get
            {
                foreach (var block in BlockCreator.GetPlacedBlocks())
                    if (block.ObjectIndex == fisherIndex)
                        return block;
                Debug.LogWarning($"[Benevolent Sprites]: Failed to find fishing net with index {fisherIndex}");
                return null;
            }
        }
        [SerializeField]
        uint fisherIndex;
        public Message_Sprite_ClearFish(Block block) => fisherIndex = block.ObjectIndex;
        public override void Use()
        {
            var net = CleanerSprite.GetFishNetFromBlock(netBlock.transform);
            foreach (var c in net.FishingNet_CaughtFish())
                if (c && arashiObjectEnabler.IsAssignableFrom(c.GetType()))
                    c.FishObjectEnabler_DisableModels();
            var a = net.FishingNet_CaughtFishItem();
            for (int i = 0; i < a.Length; i++)
                a[i] = null;
            net.FishingNet_FishCount(0);
            net.FishingNet_SetNetFull(false);
        }
    }

    [Serializable]
    class Message_Sprite_RequestMissing : Message_Sprite
    {
        public uint[] MissingIndecies;
        public ulong steamId = ComponentManager<Raft_Network>.Value.LocalSteamID.m_SteamID;
        public Message_Sprite_RequestMissing(uint[] missing) => MissingIndecies = missing;
        public override void Use()
        {
            var msgs = new List<Message>();
            foreach (var s in sprites)
                if (MissingIndecies.Contains(s.ObjectIndex))
                    msgs.Add(new Message_Sprite_Recreate(s, false).Message);
            new Packet_Multiple(EP2PSend.k_EP2PSendReliable) { messages = msgs.ToArray() }.Send(new CSteamID(steamId));
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class AccessorAttribute : Attribute
    {
        public readonly Type targetType;
        public readonly string targetName;
        public readonly AccessorType targetMember;
        public readonly Type[] targetParameters;
        public AccessorAttribute(AccessorType member, Type type, string name, Type[] parameters = null)
        {
            targetMember = member;
            targetType = type;
            targetName = name;
            targetParameters = parameters;
        }

        public static void ApplyAll()
        {
            var h = new Harmony("com.aidanamite.BenevolentSprites.Accessors");
            var p = new HarmonyMethod(typeof(AccessorAttribute).GetMethod(nameof(Transpiler), ~BindingFlags.Default));
            foreach (var t in typeof(BenevolentSprites).Assembly.GetTypes())
                foreach (var m in t.GetMethods(~BindingFlags.Default))
                {
                    var a = m.GetCustomAttribute<AccessorAttribute>();
                    if (a != null && a.targetType != null && a.targetName != null)
                    {
                        target[m] = a.targetMember == AccessorType.Field ? (MemberInfo)a.targetType.GetField(a.targetName, ~BindingFlags.Default) : a.targetParameters == null ? a.targetType.GetMethod(a.targetName, ~BindingFlags.Default) : a.targetType.GetMethods(~BindingFlags.Default).FirstOrDefault(x => x.Name== a.targetName && x.GetParameters().SequenceEquals(a.targetParameters,(y,z) => y.ParameterType == z));
                        if (target[m] == null)
                            Debug.LogError($"Failed to find accessor target for {m.DeclaringType.FullName}::{m}");
                        else
                            try
                            {
                                h.Patch(m, transpiler: p);
                            }
                            catch (Exception e)
                            {
                                Debug.LogError($"Failed to apply accessor for {m.DeclaringType.FullName}::{m}\nReason: {(e is TargetInvocationException ? e.InnerException : e)}");
                            }
                    }
                }
        }

        static Dictionary<MethodBase, MemberInfo> target = new Dictionary<MethodBase, MemberInfo>();
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase method, ILGenerator iL)
        {
            var k = method.GetParameters().Length;
            var code = new List<CodeInstruction>();
            var i = method.IsStatic ? 0 : 1;
            var l = 0;
            var targetF = target[method] as FieldInfo;
            var targetM = target[method] as MethodInfo;
            if (!(targetF?.IsStatic ?? targetM.IsStatic) && k == 0)
            {
                Debug.LogError($"Failed to apply accessor for {method.DeclaringType}::{method}\nReason: Accessor for non-static field/method must have at least 1 parameter");
                return instructions;
            }
            var j = targetM?.GetParameters() ?? new ParameterInfo[Math.Min(targetF.IsStatic ? 1 : 2,k)];
            if (targetM != null && !targetM.IsStatic)
                j = new ParameterInfo[] { null }.Concat(j).ToArray();
            foreach (var p in j)
            {
                if (l >= k)
                {
                    var pt = p?.ParameterType ?? targetM.DeclaringType;
                    object pv = null;
                    if (p != null && p.HasDefaultValue)
                    {
                        pv = p.DefaultValue;
                        if (pt.IsEnum)
                            pv = pv.GetType().GetField("value__", ~BindingFlags.Default).GetValue(pv);
                    }
                    if (pt.IsEnum)
                        pt = Enum.GetUnderlyingType(pt);
                    if (pt == typeof(byte) || pt == typeof(sbyte) || pt == typeof(short) || pt == typeof(ushort) || pt == typeof(int) || pt == typeof(uint))
                        code.Add(new CodeInstruction(OpCodes.Ldc_I4, pv));
                    else if (pt == typeof(long) || pt == typeof(ulong))
                        code.Add(new CodeInstruction(OpCodes.Ldc_I8, pv));
                    else if (pt == typeof(float))
                        code.Add(new CodeInstruction(OpCodes.Ldc_R4, pv));
                    else if (pt == typeof(double))
                        code.Add(new CodeInstruction(OpCodes.Ldc_R8, pv));
                    else if (pv == null)
                        code.Add(new CodeInstruction(OpCodes.Ldnull));
                    else
                    {
                        var c = pt.GetConstructor(new Type[0]);
                        if (c != null)
                            code.Add(new CodeInstruction(OpCodes.Newobj, c));
                        else
                            code.AddRange(new[] {
                                    new CodeInstruction(OpCodes.Ldtoken, pt),
                                    new CodeInstruction(OpCodes.Call, typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle))),
                                    new CodeInstruction(OpCodes.Call, typeof(FormatterServices).GetMethod(nameof(FormatterServices.GetUninitializedObject)))
                                });
                    }
                    continue;
                }
                if (i <= 3)
                {
                    code.Add(new CodeInstruction(i == 0 ? OpCodes.Ldarg_0 : i == 1 ? OpCodes.Ldarg_1 : i == 2 ? OpCodes.Ldarg_2 : OpCodes.Ldarg_3));
                    i++;
                }
                else
                    code.Add(new CodeInstruction(OpCodes.Ldarg_S, i++));
                l++;
            }
            var ret = (method as MethodInfo)?.ReturnType;
            var hasRet = ret != typeof(void) && ret != null;
            if (targetF != null)
            {
                if (l > (targetF.IsStatic ? 0 : 1))
                {
                    LocalBuilder loc = null;
                    if (hasRet)
                    {
                        loc = iL.DeclareLocal(targetF.FieldType);
                        code.AddRange(new[]
                        {
                            new CodeInstruction(OpCodes.Stloc_S, loc),
                            new CodeInstruction(OpCodes.Ldloc_S, loc)
                        });
                    }
                    code.Add(new CodeInstruction(targetF.IsStatic ? OpCodes.Stsfld : OpCodes.Stfld, targetF));
                    if (hasRet)
                        code.Add(new CodeInstruction(OpCodes.Ldloc_S, loc));
                }
                else if (hasRet)
                    code.Add(new CodeInstruction(targetF.IsStatic ? OpCodes.Ldsfld : OpCodes.Ldfld, targetF));
            }
            else if (targetM != null)
                code.Add(new CodeInstruction(targetM.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, targetM));
            if (!hasRet)
            {
                if (targetM != null && targetM.ReturnType != typeof(void) && targetM.ReturnType != null)
                    code.Add(new CodeInstruction(OpCodes.Pop));
                code.Add(new CodeInstruction(OpCodes.Nop));
            }
            code.Add(new CodeInstruction(OpCodes.Ret));
            return code;
        }
    }
    public enum AccessorType
    {
        Field,
        Method
    }


    [HarmonyPatch(typeof(BlockCreator), "RemoveBlockCoroutine", methodType: MethodType.Enumerator )]
    class Patch_DestroyBlocks
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = instructions.ToList();
            code.InsertRange(code.FindIndex(code.FindIndex(x => x.operand is MethodInfo m && m.Name == "DestroyBlock"), x => x.operand is MethodInfo m && m.Name == "ToList") + 1, new[]
            {
                new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(Patch_DestroyBlocks),nameof(OnBlocksRemoved)))
            });
            return code;
        }
        static void OnBlocksRemoved(List<Block> blocks)
        {
            // do stuff
        }
    }
}