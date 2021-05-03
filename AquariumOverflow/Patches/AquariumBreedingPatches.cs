﻿namespace AquariumOverflow.Patches
{
    using HarmonyLib;
    using QModManager.API;
    using RALIV.Subnautica.AquariumBreeding;

    [HarmonyPatch(typeof(AquariumInfo), nameof(AquariumInfo.Get))]
    internal class AquariumBreedingPatches
    {
        [HarmonyPostfix]
        private static void Postfix(Aquarium aquarium, AquariumInfo __result)
        {
            //Check for valid AquariumInfo as well as make sure the aquarium is full.
            if(__result == null || aquarium.storageContainer.container.HasRoomFor(1, 1))
                return;

            SubRoot subRoot = aquarium.GetComponentInParent<SubRoot>();

            //Ensure the aquarium is built in or on a Cyclops a Base.
            if(subRoot == null)
                return;

            double timePassed = DayNightCycle.main.timePassed;
            bool bioReactorsFull = false;
            bool cyclopsBioReactorsFull = !QModServices.Main.ModPresent("CyclopsBioReactor");
            bool alterraGensFull = !QModServices.Main.ModPresent("FCSEnergySolutions");

            //Checks all types of fish in the aquarium and collects data on how many to be put in the BioReactors
            for(int i = 0; i < __result.BreedInfo.Count; i++)
            {

                //Full stop if all reactors are full.
                if(alterraGensFull && cyclopsBioReactorsFull && bioReactorsFull)
                    break;

                AquariumInfo.AquariumBreedTime nextBreed = __result.BreedInfo[i];

                //skip if not breeding time yet.
                if(nextBreed.BreedTime > timePassed)
                    continue;

                TechType fishType = nextBreed.FishType;

                //Check if fish can go in bioreactors
                if(BaseBioReactor.GetCharge(fishType) <= 0)
                    continue;

                int breedCount = nextBreed.BreedCount;

                if(!alterraGensFull && breedCount > 0)
                    alterraGensFull = AGCompat.TryOverflowIntoAlterraGens(subRoot, fishType, ref breedCount);

                if(!cyclopsBioReactorsFull && breedCount > 0)
                    cyclopsBioReactorsFull = CBRCompat.TryOverflowIntoCyclopsBioreactors(subRoot, fishType, ref breedCount);

                if(!bioReactorsFull && breedCount > 0)
                    bioReactorsFull = Main.TryOverflowIntoBioreactors(subRoot, fishType, ref breedCount);

                nextBreed.BreedTime += 600.0;
                AquariumInfo.Update(aquarium);
            }
        }
    }
}
