﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Harmony;
using Oculus.Newtonsoft.Json;
using UnityEngine;
using UWE;

namespace ResourceOverload
{
    [HarmonyPatch(typeof(LootDistributionData))]
    [HarmonyPatch("GetBiomeLoot")]
    public static class CustomLootDistributionData
    {
        public static SortedDictionary<string, SortedDictionary<string, float>> techs = new SortedDictionary<string, SortedDictionary<string, float>>();
        public static SortedDictionary<BiomeType, LootDistributionData.DstData> customDSTDistribution = new SortedDictionary<BiomeType, LootDistributionData.DstData>();
        public static List<TechType> randomizedResources = new List<TechType>() 
        { 
            TechType.AluminumOxide, TechType.Sulphur, TechType.Diamond, 
            TechType.Kyanite, TechType.Lead, TechType.Lithium , TechType.Magnetite, 
            TechType.Nickel, TechType.Quartz, TechType.Silver, TechType.UraniniteCrystal, 
            TechType.Salt, TechType.AcidMushroom, TechType.BloodOil, TechType.JellyPlant, 
            TechType.DrillableAluminiumOxide, TechType.DrillableCopper, TechType.DrillableDiamond,
            TechType.DrillableGold, TechType.DrillableKyanite, TechType.DrillableLead,
            TechType.DrillableLithium, TechType.DrillableMagnetite, TechType.DrillableMercury,
            TechType.DrillableNickel, TechType.DrillableQuartz, TechType.DrillableSalt,
            TechType.DrillableSilver, TechType.DrillableSulphur, TechType.DrillableTitanium,
            TechType.DrillableUranium, TechType.TimeCapsule, TechType.Titanium, 
            TechType.Copper, TechType.Gold
        };
        public static List<TechType> randomizedChunks = new List<TechType>()
        {
            TechType.LimestoneChunk, TechType.SandstoneChunk, TechType.ShaleChunk
        };
        public static List<TechType> randomizedFragments = new List<TechType>();
        public static bool changed = false;

        [HarmonyPostfix]
        public static void Postfix(LootDistributionData __instance, BiomeType biome, ref bool __result, ref LootDistributionData.DstData data)
        {
            string path;
            if (Config.Randomization)
            {
                path = Path.GetDirectoryName(Assembly.GetAssembly(typeof(Entry)).Location) + "/RandomizerCache";
            }
            else
            {
                path = Path.GetDirectoryName(Assembly.GetAssembly(typeof(Entry)).Location) + "/Cache";
            }

            if (!File.Exists(path) || Config.RegenSpawns || changed || customDSTDistribution.Count == 0)
            {
                GenerateCustomData(__instance);

                using (StreamWriter writer = new StreamWriter(path))
                {
                    writer.Write(JsonConvert.SerializeObject(customDSTDistribution, Formatting.Indented));
                }
            }

            if (__result)
                __result = customDSTDistribution.TryGetValue(biome, out data);
        }

        private static void GenerateCustomData(LootDistributionData __instance)
        {
            Config.techProbability = new SortedList<string, float>();
            customDSTDistribution = new SortedDictionary<BiomeType, LootDistributionData.DstData>();
            foreach (BiomeType bio in Enum.GetValues(typeof(BiomeType)))
            {
                if (__instance.dstDistribution.ContainsKey(bio))
                {
                    customDSTDistribution[bio] = new LootDistributionData.DstData() { prefabs = new List<LootDistributionData.PrefabData>() };

                    CheckSettings(bio);
                    if (Config.Randomization)
                        Randomizer(__instance, bio);
                    LoadOriginalDistribution(__instance, bio);
                    GenerateMissingConfiguration(bio);
                }
            }

            string path;
            if (Config.Randomization)
            {
                path = Path.GetDirectoryName(Assembly.GetAssembly(typeof(Entry)).Location) + "/RandomizerConfig.json";
            }
            else
            {
                path = Path.GetDirectoryName(Assembly.GetAssembly(typeof(Entry)).Location) + "/Config.json";
            }
            if (!string.IsNullOrEmpty(path))
            {
                if (Config.resetDefaults || !File.Exists(path) || changed)
                {
                    using (StreamWriter writer = new StreamWriter(path))
                    {
                        writer.Write(JsonConvert.SerializeObject(techs, Formatting.Indented));
                    }
                }
            }
            if (Config.RegenSpawns || Config.resetDefaults)
            {
                Config.resetDefaults = false;
                Config.RegenSpawns = false;
            }
        }

        #region Settings
        private static void CheckSettings(BiomeType bio)
        {
            if (Config.resetDefaults)
            {
                ResetSettings(bio);
            }
            else
            {
                LoadSettings(bio);
            }
        }

        private static void LoadSettings(BiomeType bio)
        {
            string path;
            if (Config.Randomization)
            {
                path = Path.GetDirectoryName(Assembly.GetAssembly(typeof(Entry)).Location) + "/RandomizerConfig.json";
            }
            else
            {
                path = Path.GetDirectoryName(Assembly.GetAssembly(typeof(Entry)).Location) + "/Config.json";
            }
            if (!string.IsNullOrEmpty(path))
            {
                if (File.Exists(path))
                {
                    using (StreamReader reader = new StreamReader(path))
                    {
                        techs = JsonConvert.DeserializeObject<SortedDictionary<string, SortedDictionary<string, float>>>(reader.ReadToEnd());
                    }
                }
            }
            if(techs.Count == 0)
            {
                foreach (TechType type in Enum.GetValues(typeof(TechType)))
                {
                    string tech0;
                    if (Config.Randomization)
                    {
                        tech0 = TechTypeExtensions.GetOrFallback(Language.main, type, type);
                    }
                    else
                    {
                        tech0 = TechTypeExtensions.GetOrFallback(Language.main, type, type) + "| " + bio.AsString().Split('_')[0];
                    }
                    if (PlayerPrefs.HasKey(tech0 + ":TechProbability"))
                    {
                        Config.techProbability[tech0] = PlayerPrefs.GetFloat(tech0 + ":TechProbability");
                    }
                }
            }
            else
            {
                foreach (string type in techs.Keys)
                {
                    string tech0;
                    if (Config.Randomization)
                    {
                        tech0 = type;
                    }
                    else
                    {
                        tech0 = type + "| " + bio.AsString().Split('_')[0];
                    }
                    if (PlayerPrefs.HasKey(tech0 + ":TechProbability"))
                    {
                        techs[type].TryGetValue(bio.AsString().Split('_')[0], out float probability);
                        if (PlayerPrefs.GetFloat(tech0 + ":TechProbability") == probability)
                        {
                            Config.techProbability[tech0] = probability;
                        }
                        else if(!Config.RegenSpawns)
                        {
                            Config.techProbability[tech0] = probability;
                            PlayerPrefs.SetFloat(tech0 + ":TechProbability", probability);
                        }
                        else if (Config.RegenSpawns)
                        {
                            changed = true;
                            Config.techProbability[tech0] = PlayerPrefs.GetFloat(tech0 + ":TechProbability");
                        }
                    }
                }
            }
        }

        private static void ResetSettings(BiomeType bio)
        {
            foreach (TechType type in Enum.GetValues(typeof(TechType)))
            {
                string tech0;
                tech0 = TechTypeExtensions.GetOrFallback(Language.main, type, type);
                if (PlayerPrefs.HasKey(tech0 + ":TechProbability"))
                {
                    PlayerPrefs.DeleteKey(tech0 + ":TechProbability");
                }

                tech0 = TechTypeExtensions.GetOrFallback(Language.main, type, type) + "| " + bio.AsString().Split('_')[0];
                if (PlayerPrefs.HasKey(tech0 + ":TechProbability"))
                {
                    PlayerPrefs.DeleteKey(tech0 + ":TechProbability");
                }
            }
        }

        private static void GenerateMissingConfiguration(BiomeType biomeType)
        {
            try
            {
                foreach (string type in techs.Keys)
                {
                    string tech0;
                    if (Config.Randomization)
                    {
                        tech0 = type;
                    }
                    else
                    {
                        tech0 = type + "| " + biomeType.AsString().Split('_')[0];
                    }
                    if (!Config.techProbability.ContainsKey(tech0) && techs.ContainsKey(type))
                    {
                        if (type == TechType.TimeCapsule.AsString())
                        {
                            Config.techProbability[tech0] = techs[type][biomeType.AsString().Split('_')[0]] * 1000;
                        }
                        else
                        {
                            Config.techProbability[tech0] = techs[type][biomeType.AsString().Split('_')[0]] * 100;
                        }
                    }
                }
            }
            catch(Exception)
            {

            }
        }

        #endregion

        private static void Randomizer(LootDistributionData __instance, BiomeType bio)
        {
            foreach (BiomeType b in Enum.GetValues(typeof(BiomeType)))
            {
                if (!bio.AsString().Contains("Fragment") && __instance.dstDistribution.TryGetValue(b, out var d))
                {
                    for (int i = 0; i < d.prefabs.Count; i++)
                    {
                        LootDistributionData.PrefabData prefabData = d.prefabs[i];
                        if (prefabData.classId.ToLower() != "none")
                        {
                            if (WorldEntityDatabase.TryGetInfo(prefabData.classId, out WorldEntityInfo wei))
                            {
                                if (randomizedChunks.Contains(wei.techType) && CheckBiomeForTechTypes(__instance, bio, randomizedChunks))
                                {
                                    AddPrefabToCustomData(prefabData, bio, wei);
                                    continue;
                                }
                                if (randomizedResources.Contains(wei.techType) && CheckBiomeForTechTypes(__instance, bio, randomizedResources))
                                {
                                    AddPrefabToCustomData(prefabData, bio, wei);
                                    continue;
                                }
                                if(randomizedFragments.Count == 0)
                                {
                                    foreach(TechType techType in Enum.GetValues(typeof(TechType)))
                                    {
                                        if (techType.AsString().Contains("Fragment"))
                                            randomizedFragments.Add(techType);
                                    }
                                }
                                if (randomizedFragments.Contains(wei.techType) && CheckBiomeForTechTypes(__instance, bio, randomizedFragments))
                                {
                                    AddPrefabToCustomData(prefabData, bio, wei);
                                    continue;
                                }
                            }
                        }
                    }
                }
            }
            for (int j = 0; j < customDSTDistribution[bio].prefabs.Count; j++)
            {
                LootDistributionData.PrefabData prefabData = customDSTDistribution[bio].prefabs[j];
                prefabData.probability = 0.1f;
            }
        }

        private static void AddPrefabToCustomData(LootDistributionData.PrefabData prefabData, BiomeType bio, WorldEntityInfo wei)
        {
            if (!customDSTDistribution[bio].prefabs.Contains(prefabData))
            {
                if (!techs.ContainsKey(TechTypeExtensions.GetOrFallback(Language.main, wei.techType, wei.techType)))
                    techs[TechTypeExtensions.GetOrFallback(Language.main, wei.techType, wei.techType)] = new SortedDictionary<string, float>();

                string tech0;
                if (Config.Randomization)
                {
                    tech0 = TechTypeExtensions.GetOrFallback(Language.main, wei.techType, wei.techType);
                }
                else
                {
                    tech0 = TechTypeExtensions.GetOrFallback(Language.main, wei.techType, wei.techType) + "| " + bio.AsString().Split('_')[0];
                }
                if (!techs[TechTypeExtensions.GetOrFallback(Language.main, wei.techType, wei.techType)].ContainsKey(bio.AsString().Split('_')[0]) || Config.techProbability.ContainsKey(tech0))
                {
                    if (Config.techProbability.ContainsKey(tech0))
                    {
                        if (wei.techType == TechType.TimeCapsule)
                        {
                            prefabData.probability = Config.techProbability[tech0] / 1000;
                            techs[TechTypeExtensions.GetOrFallback(Language.main, wei.techType, wei.techType)][bio.AsString().Split('_')[0]] = prefabData.probability;
                        }
                        else
                        {
                            prefabData.probability = Config.techProbability[tech0] / 100;
                            techs[TechTypeExtensions.GetOrFallback(Language.main, wei.techType, wei.techType)][bio.AsString().Split('_')[0]] = prefabData.probability;
                        }
                        customDSTDistribution[bio].prefabs.Add(prefabData);
                    }
                    else
                    {
                        techs[TechTypeExtensions.GetOrFallback(Language.main, wei.techType, wei.techType)][bio.AsString().Split('_')[0]] = prefabData.probability;
                        customDSTDistribution[bio].prefabs.Add(prefabData);
                    }
                }
                else if (prefabData.probability > 0 && (prefabData.probability < techs[TechTypeExtensions.GetOrFallback(Language.main, wei.techType, wei.techType)][bio.AsString().Split('_')[0]] || techs[TechTypeExtensions.GetOrFallback(Language.main, wei.techType, wei.techType)][bio.AsString().Split('_')[0]] == 0 || techs[TechTypeExtensions.GetOrFallback(Language.main, wei.techType, wei.techType)][bio.AsString().Split('_')[0]] == 1))
                {
                    techs[TechTypeExtensions.GetOrFallback(Language.main, wei.techType, wei.techType)][bio.AsString().Split('_')[0]] = prefabData.probability;
                    customDSTDistribution[bio].prefabs.Add(prefabData);
                }
            }
        }

        private static bool CheckBiomeForTechTypes(LootDistributionData lootDistributionData, BiomeType biomeType, List<TechType> techTypes)
        {
            foreach (LootDistributionData.PrefabData prefab in lootDistributionData.dstDistribution[biomeType].prefabs)
            {
                if (WorldEntityDatabase.TryGetInfo(prefab.classId, out WorldEntityInfo wei2))
                {
                    if (techTypes.Contains(wei2.techType))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static void LoadOriginalDistribution(LootDistributionData lootDistributionData, BiomeType biomeType)
        {
            foreach (LootDistributionData.PrefabData prefabData in lootDistributionData.dstDistribution[biomeType].prefabs)
            {
                if (WorldEntityDatabase.TryGetInfo(prefabData.classId, out WorldEntityInfo wei))
                {
                    if (wei.techType != TechType.None)
                    {
                        string tech0;
                        if (Config.Randomization)
                        {
                            tech0 = TechTypeExtensions.GetOrFallback(Language.main, wei.techType, wei.techType);
                        }
                        else
                        {
                            tech0 = TechTypeExtensions.GetOrFallback(Language.main, wei.techType, wei.techType) + "| " + biomeType.AsString().Split('_')[0];
                        }

                        if (!Config.techProbability.ContainsKey(tech0))
                        {
                            if (!techs.ContainsKey(TechTypeExtensions.GetOrFallback(Language.main, wei.techType, wei.techType)))
                                techs[TechTypeExtensions.GetOrFallback(Language.main, wei.techType, wei.techType)] = new SortedDictionary<string, float>();

                            if (!techs[TechTypeExtensions.GetOrFallback(Language.main, wei.techType, wei.techType)].ContainsKey(biomeType.AsString().Split('_')[0]))
                                techs[TechTypeExtensions.GetOrFallback(Language.main, wei.techType, wei.techType)][biomeType.AsString().Split('_')[0]] = prefabData.probability;
                            customDSTDistribution[biomeType].prefabs.Add(prefabData);
                            continue;
                        }
                        else
                        {
                            if (!techs.ContainsKey(TechTypeExtensions.GetOrFallback(Language.main, wei.techType, wei.techType)))
                                techs[TechTypeExtensions.GetOrFallback(Language.main, wei.techType, wei.techType)] = new SortedDictionary<string, float>();

                            prefabData.probability = Config.techProbability[tech0] / 100;
                            techs[TechTypeExtensions.GetOrFallback(Language.main, wei.techType, wei.techType)][biomeType.AsString().Split('_')[0]] = prefabData.probability;
                            customDSTDistribution[biomeType].prefabs.Add(prefabData);
                            continue;
                        }
                    }
                    else
                    {
                        customDSTDistribution[biomeType].prefabs.Add(prefabData);
                        continue;
                    }
                }
                else
                {
                    customDSTDistribution[biomeType].prefabs.Add(prefabData);
                }
            }
        }

    }
}