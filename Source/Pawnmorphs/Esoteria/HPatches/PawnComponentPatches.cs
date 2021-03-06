﻿// PawnComponentPatches.cs created by Iron Wolf for Pawnmorph on 11/27/2019 1:00 PM
// last updated 11/27/2019  1:00 PM

using System;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Noise;

#pragma warning disable 1591

#if true
namespace Pawnmorph.HPatches
{
    public static class PawnComponentPatches
    {
        [HarmonyPatch(typeof(PawnComponentsUtility))]
        [HarmonyPatch("AddAndRemoveDynamicComponents")]
        public static class AddRemoveComponentsPatch
        {
            internal static void Postfix(Pawn pawn)
            {
                var sState = pawn.GetSapienceState(); 
                sState?.AddOrRemoveDynamicComponents();
            }


            private static void RemoveSapientAnimalComponents(Pawn pawn)
            {
                if (pawn.drafter == null) return; 

                //remove the drafter component if the animal is now feral 
                pawn.drafter.Drafted = false;
                
                if (pawn.MapHeld != null)
                {
                    pawn.equipment?.DropAllEquipment(pawn.PositionHeld, pawn.Faction?.IsPlayer != true);
                    pawn.apparel?.DropAll(pawn.PositionHeld, pawn.Faction?.IsPlayer != true);
                }
                else
                {
                    pawn.equipment?.DestroyAllEquipment();
                    pawn.apparel?.DestroyAll();
                }

                pawn.ownership?.UnclaimAll();
                pawn.workSettings?.EnableAndInitializeIfNotAlreadyInitialized();
                pawn.workSettings?.DisableAll();
                pawn.ownership = null;
                pawn.drafter = null;
                pawn.apparel = null;
                pawn.foodRestriction = null; 
                pawn.equipment = null;
                pawn.royalty = null;
                pawn.guest = null;
                pawn.guilt = null; 
                pawn.drugs = null; 
                pawn.story = null;
                pawn.abilities = null; 
                pawn.skills = null;
                pawn.timetable = null; 
                pawn.workSettings = null;
                pawn.outfits = null; 
                var saComp = pawn.GetComp<Comp_SapientAnimal>();
                if (saComp != null)
                {
                    pawn.AllComps.Remove(saComp); 
                }
            }

            private static void AddSapientAnimalComponents(Pawn pawn)
            {
                //add the drafter and equipment components 
                //if 
                if (pawn.Faction?.IsPlayer == true)
                {
                    if (pawn.drafter == null)
                    {
                        pawn.drafter = new Pawn_DraftController(pawn);
                        pawn.jobs = pawn.jobs ?? new Pawn_JobTracker(pawn);
                    }

                    if (pawn.workSettings == null)
                    {
                        pawn.workSettings = new Pawn_WorkSettings(pawn);
                    }
                }

                pawn.ownership = pawn.ownership ?? new Pawn_Ownership(pawn); 
                pawn.equipment = pawn.equipment ?? new Pawn_EquipmentTracker(pawn);
                pawn.story = pawn.story ?? new Pawn_StoryTracker(pawn); //need to add story component to not break hospitality 
                pawn.apparel = pawn.apparel ?? new  Pawn_ApparelTracker(pawn); //need this to not break thoughts and stuff 
                pawn.skills = pawn.skills ?? new Pawn_SkillTracker(pawn); //need this for thoughts 
                pawn.royalty = pawn.royalty ?? new Pawn_RoyaltyTracker(pawn);// former humans can be royalty  
                pawn.abilities = pawn.abilities ?? new Pawn_AbilityTracker(pawn); 
                pawn.mindState = pawn.mindState ?? new Pawn_MindState(pawn);
                pawn.drugs = pawn.drugs ?? new Pawn_DrugPolicyTracker(pawn);
                pawn.guest = pawn.guest ?? new Pawn_GuestTracker(pawn);
                pawn.outfits = pawn.outfits ?? new Pawn_OutfitTracker(pawn); 
                pawn.guilt = pawn.guilt ?? new Pawn_GuiltTracker(); 
                pawn.foodRestriction = pawn.foodRestriction ?? new Pawn_FoodRestrictionTracker(pawn); 
                pawn.timetable = pawn.timetable ?? new Pawn_TimetableTracker(pawn); 
                Comp_SapientAnimal nComp = pawn.GetComp<Comp_SapientAnimal>();
                bool addedComp = false;
                
                if (nComp == null)
                {
                    addedComp = true; 
                    nComp = new Comp_SapientAnimal {parent = pawn};
                    pawn.AllComps.Add(nComp); 

                }

                //now initialize the comp 
                if (addedComp)
                {
                    nComp.Initialize(new CompProperties());//just pass in empty props 
                }
                
            }
        }
    }
}
#endif