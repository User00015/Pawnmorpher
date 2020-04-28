﻿// Need_Control.cs modified by Iron Wolf for Pawnmorph on 12/07/2019 1:48 PM
// last updated 12/07/2019  1:49 PM

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Pawnmorph.ThingComps;
using Pawnmorph.Utilities;
using RimWorld;
using UnityEngine;
using Verse;
using static Pawnmorph.InstinctUtilities;

namespace Pawnmorph
{
    /// <summary>
    ///     need that represents a sapient animal's control or humanity left
    /// </summary>
    [StaticConstructorOnStartup]
    public class Need_Control : Need_Seeker
    {
        /// <summary>
        ///     delegate for the sapience level changed handle
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="pawn"></param>
        /// <param name="sapienceLevel"></param>
        public delegate void SapienceLevelChangedHandle(Need_Control sender, Pawn pawn, SapienceLevel sapienceLevel);

        private static HashSet<ThingDef> _enabledRaces;

        /// <summary>
        ///     Occurs when the sapience level changes .
        /// </summary>
        public event SapienceLevelChangedHandle SapienceLevelChanged;

        private float _seekerLevel;

        private SapienceLevel _currentLevel;

        private float? _maxLevelCached = null;


        /// <summary>
        ///     Initializes a new instance of the <see cref="Need_Control" /> class.
        /// </summary>
        /// <param name="pawn">The pawn.</param>
        public Need_Control(Pawn pawn) : base(pawn)
        {
            TrySubscribe();
        }

        private bool _subscribed;

        void TrySubscribe()
        {
            if (_subscribed) return;
            var tracker = pawn.GetAspectTracker();
            if (tracker == null) return;
            tracker.AspectAdded += OnAspectAdded;
            tracker.AspectRemoved += AspectRemoved;
            _subscribed = true; 
        }

        private void OnAspectAdded(AspectTracker sender, Aspect _)
        {
            ResetCaches();   
        }

        private void AspectRemoved(AspectTracker sender, Aspect _)
        {
           ResetCaches();
        }

        /// <summary>
        ///     Gets the maximum level.
        /// </summary>
        /// <value>
        ///     The maximum level.
        /// </value>
        public override float MaxLevel
        {
            get
            {
                if (!pawn.IsLoadingOrSpawning()) //make sure we don't look for stats while the pawn is loading 
                    return Mathf.Max(CalculateNetResistance(pawn) / AVERAGE_MAX_SAPIENCE, 0.01f);
                return AVERAGE_RESISTANCE / AVERAGE_MAX_SAPIENCE;
            }
        }

        /// <summary>
        ///     Gets the current instant level.
        /// </summary>
        /// <value>
        ///     The current instant level.
        /// </value>
        public override float CurInstantLevel => _seekerLevel;


        /// <summary>
        ///     Gets the seeker level.
        /// </summary>
        /// <value>
        ///     The seeker level.
        /// </value>
        public float SeekerLevel => _seekerLevel;

        [NotNull]
        private static HashSet<ThingDef> EnabledRaces
        {
            get
            {
                if (_enabledRaces == null)
                {
                    _enabledRaces = new HashSet<ThingDef>();
                    foreach (ThingDef race in DefDatabase<ThingDef>.AllDefs.Where(t => t.race?.Humanlike == true))
                    {
                        if (!DefDatabase<MutagenDef>.AllDefs.Any(m => m.CanTransform(race))) continue;
                        _enabledRaces.Add(race);
                    }
                }

                return _enabledRaces;
            }
        }

        /// <summary>
        ///     Adds the instinct change to this need
        /// </summary>
        /// <param name="instinctChange">The instinct change.</param>
        public void AddInstinctChange(int instinctChange)
        {
            _maxLevelCached = null;
            _seekerLevel += CalculateControlChange(pawn, instinctChange) / AVERAGE_MAX_SAPIENCE;
            _seekerLevel = Mathf.Clamp(_seekerLevel, 0, Mathf.Min(MaxLevel, Limit));
        }

        /// <summary>
        ///     Draws the GUI.
        /// </summary>
        /// <param name="rect">The rect.</param>
        /// <param name="maxThresholdMarkers">The maximum threshold markers.</param>
        /// <param name="customMargin">The custom margin.</param>
        /// <param name="drawArrows">if set to <c>true</c> [draw arrows].</param>
        /// <param name="doTooltip">if set to <c>true</c> [do tooltip].</param>
        public override void DrawOnGUI(Rect rect, int maxThresholdMarkers = 2147483647, float customMargin = -1,
                                       bool drawArrows = true,
                                       bool doTooltip = true)
        {
            if (threshPercents == null || _maxLevelCached == null)
            {
                _maxLevelCached = _maxLevelCached ?? MaxLevel;

                float mLevel = _maxLevelCached.Value;
                _seekerLevel = Mathf.Clamp(_seekerLevel, 0, mLevel);
                CurLevel = Mathf.Clamp(CurLevel, 0, mLevel); //make sure the levels fall within the correct bounds 
                threshPercents = threshPercents ?? new List<float>();
                foreach (VTuple<SapienceLevel, float> sapienceLevelThreshold in FormerHumanUtilities.SapienceLevelThresholds)
                {
                    float thresh = sapienceLevelThreshold.Second / mLevel;
                    if (thresh > 1) continue;
                    threshPercents.Add(thresh);
                }
            }

            base.DrawOnGUI(rect, maxThresholdMarkers, customMargin, drawArrows, doTooltip);
        }

        /// <summary>
        ///     Exposes the data.
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref _seekerLevel, nameof(_seekerLevel), -1);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (_seekerLevel < 0)
                    _seekerLevel = CurLevel;
                _currentLevel = FormerHumanUtilities.GetQuantizedSapienceLevel(_seekerLevel);
                CurLevel = Mathf.Clamp(CurLevel, 0, MaxLevel);
                //OnSapienceLevelChanges();
            }
        }

        /// <summary>
        ///     Determines whether the control need is enabled for the pawn.
        /// </summary>
        /// <param name="pawn">The pawn.</param>
        /// <returns>
        ///     <c>true</c> if control need is enabled for the given humanoid race; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsEnabledFor([NotNull] Pawn pawn)
        {
            if (pawn == null) throw new ArgumentNullException(nameof(pawn));
            SapienceLevel fhLevel = pawn.GetQuantizedSapienceLevel() ?? SapienceLevel.PermanentlyFeral;
            return fhLevel != SapienceLevel.PermanentlyFeral || EnabledRaces.Contains(pawn.def);
        }

        /// <summary>
        ///     called every so often by the need manager.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        public override void NeedInterval()
        {
            base.NeedInterval();
            if (pawn.IsHashIntervalTick(TimeMetrics.TICKS_PER_REAL_SECOND)) //just every second or so 
            {
                TrySubscribe();
                _seekerLevel = Mathf.Min(_seekerLevel, Limit); 
                float instinctChange = GetInstinctChangePerTick(pawn) * TimeMetrics.TICKS_PER_REAL_SECOND;
                if (Mathf.Abs(instinctChange) > EPSILON) AddInstinctChange(Mathf.CeilToInt(instinctChange));
                //_maxLevelCached = null; 
                SapienceLevel sLevel = FormerHumanUtilities.GetQuantizedSapienceLevel(CurLevel);
                if (sLevel != _currentLevel)
                {
                    _currentLevel = sLevel;
                    OnSapienceLevelChanges();
                }
            }
        }

        /// <summary>
        ///     Notifies that the cached maximum level is dirty
        /// </summary>
        public void NotifyMaxLevelDirty()
        {
            ResetCaches();
        }

        private float? _limit; 

        private void ResetCaches()
        {
            _maxLevelCached = null;
            _limit = null; 
        }

        float Limit
        {
            get
            {
                if (_limit == null)
                {
                    _limit = pawn.GetStatValue(PMStatDefOf.SapienceLimit); 
                }

                return _limit.Value; 
            }
        }


        /// <summary>
        ///     Sets the initial level.
        /// </summary>
        public override void SetInitialLevel()
        {
            CurLevel = AVERAGE_RESISTANCE / AVERAGE_MAX_SAPIENCE;
            _seekerLevel = AVERAGE_RESISTANCE / AVERAGE_MAX_SAPIENCE;
        }

        /// <summary>
        ///     Sets the initial level.
        /// </summary>
        /// <param name="sapiencePercent">The sapience level.</param>
        public void SetInitialLevel(float sapiencePercent)
        {
            _seekerLevel = Mathf.Clamp(sapiencePercent, 0, 1) * MaxLevel;
            CurLevel = _seekerLevel;
        }

        /// <summary>
        ///     Sets the sapience.
        /// </summary>
        /// <param name="sapience">The sapience.</param>
        public void SetSapience(float sapience)
        {
            _seekerLevel = Mathf.Clamp(sapience, 0, Mathf.Min(MaxLevel, Limit));
            CurLevel = _seekerLevel;

            SapienceLevel cLevel = FormerHumanUtilities.GetQuantizedSapienceLevel(CurLevel);
            if (_currentLevel != cLevel)
            {
                _currentLevel = cLevel;
                OnSapienceLevelChanges();
            }
        }


        private void OnSapienceLevelChanges()
        {
            SapienceTracker fTracker = pawn.GetSapienceTracker();
            if (fTracker == null)
            {
                Log.Error($"{pawn.Name} has the sapience need but not tracker!");
                return;
            }

            fTracker.SapienceLevel = _currentLevel;

            if (pawn.needs != null)
                pawn.needs.AddOrRemoveNeedsAsAppropriate();
            else
                Log.Warning($"{pawn.Name} does not have needs!");

            PawnComponentsUtility.AddAndRemoveDynamicComponents(pawn);

            if (pawn.Faction == Faction.OfPlayer) Find.ColonistBar?.MarkColonistsDirty();

            SapienceLevelChanged?.Invoke(this, pawn, _currentLevel);
        }
    }
}