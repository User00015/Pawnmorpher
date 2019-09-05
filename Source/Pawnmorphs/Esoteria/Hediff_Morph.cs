﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using Multiplayer.API;
using Pawnmorph.Hediffs;
using Pawnmorph.Utilities;
using UnityEngine;
using RimWorld;
using Verse;

namespace Pawnmorph
{
    public class Hediff_Morph : HediffWithComps
    {
        protected virtual int TransformationWarningStage => 1;
        private const string TRANSFORMATION_WARNING_LETTER_ID = "TransformationStageWarning";

        private HediffComp_Single _comp;

        [CanBeNull]
        public HediffComp_Single SingleComp
        {
            get { return _comp ?? (_comp = this.TryGetComp<HediffComp_Single>()); }
        }

        public override string LabelBase
        {
            get
            {
                var labelB = base.LabelBase;
                if (SingleComp != null)
                {
                    return $"{labelB}x{SingleComp.stacks}";
                }

                return labelB; 
            }
        }

        [Unsaved]
        private int _lastStage = -1; //ToDO can we save this?
        public override void PostTick()
        {
            base.PostTick();

            if (_lastStage != CurStageIndex)
            {
                _lastStage = CurStageIndex;
                EnterNextStage();
            }

          
        }

      
        protected virtual void EnterNextStage()
        {


            if (_lastStage == TransformationWarningStage && (pawn.IsColonist || pawn.IsPrisonerOfColony))
            {
                SendLetter();
            }
            
            TryGiveTransformations();
        }

        protected virtual void TryGiveTransformations()
        {



            if (CurStage.hediffGivers == null) return;

            if (MP.IsInMultiplayer)
            {
                Rand.PushState(RandUtilities.MPSafeSeed);
            }

            foreach (HediffGiver_TF tfGiver in CurStage.hediffGivers.OfType<HediffGiver_TF>())
            {
                if (tfGiver.TryTf(pawn, this)) break; //try each one, one by one. break at first one that succeeds  
            }

            if (MP.IsInMultiplayer)
            {
                Rand.PopState();
            }
        }

        private List<HediffGiver_Mutation> _givers;

        public IEnumerable<HediffGiver_Mutation> MutationGivers
        {
            get
            {
                if (_givers == null)
                {
                    var givers = def.stages?.SelectMany(g => g.hediffGivers ?? Enumerable.Empty<HediffGiver>())
                                    .OfType<HediffGiver_Mutation>()
                              ?? Enumerable.Empty<HediffGiver_Mutation>();
                    _givers = givers.ToList();
                }

                return _givers; 
            }
        }

        public override bool TryMergeWith(Hediff other)
        {
            if (!base.TryMergeWith(other)) return false;


            foreach (HediffGiver_Mutation hediffGiverMutation in MutationGivers) //make sure mutations can be re rolled 
            {
                hediffGiverMutation.ClearHediff(this); 
            }

            return true; 
        }

        private void SendLetter()
        {
            var mutagen = this.GetMutagenDef();
            if(mutagen.CanTransform(pawn))
                Messages.Message((TRANSFORMATION_WARNING_LETTER_ID).Translate(pawn),def:MessageTypeDefOf.NegativeHealthEvent);

        }

        /// <summary>
        /// this is called when the transformation hediff is removed naturally (after reaching a severity of 0) 
        /// </summary>
        /// this is only called when the hediff is removed after reaching a severity of zero, not when the pawn it's self is removed 
        protected virtual void OnFinishedTransformation()
        {
            //Log.Message($"{pawn.Name.ToStringFull} has finished transforming!");
            foreach (IPostTfHediffComp postTfHediffComp in comps.OfType<IPostTfHediffComp>())
            {
                postTfHediffComp.FinishedTransformation(pawn, this); 
            }

        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref _lastStage, "lastStage", -1); 
        }


        public override void PostRemoved()
        {
            base.PostRemoved();


            if (Severity <= 0.01f) //don't compare to zero but close to it, sometime hediff is removed naturally before then? 
            {
                OnFinishedTransformation();
            }

        }
    }
}
