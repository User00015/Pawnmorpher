﻿using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Pawnmorph
{
    public class HediffGiver_Esoteric_GenderChanceFixed : HediffGiver
    {
        public float mtbDays;
        public Gender gender;
        public int chance;
        private bool triggered = false;
        public List<BodyPartDef> fixedParts;

        public override void OnIntervalPassed(Pawn pawn, Hediff cause)
        {
            try
            {
                if (pawn.gender == gender || (Rand.RangeInclusive(0, 100) <= chance && !triggered) && pawn.RaceProps.intelligence == Intelligence.Humanlike)
                {
                    if (Rand.MTBEventOccurs(this.mtbDays, 60000f, 60f) && PawnmorphHediffGiverUtility.TryApply(pawn, hediff, fixedParts))
                    {
                        IntermittentMagicSprayer.ThrowMagicPuffDown(pawn.Position.ToVector3(), pawn.MapHeld);
                        if (cause.def.HasComp(typeof(HediffComp_Single)))
                        {
                            pawn.health.RemoveHediff(cause);
                        }
                    }
                }
                else
                {
                    triggered = false;
                }
            }
            catch
            {
            }
        }
    }
}