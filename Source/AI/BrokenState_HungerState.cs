using RimWorld;
using System;
using Verse.AI;
using Verse;

namespace SK_Enviro.AI
{
    public class BrokenState_HungerState : BrokenState
    {
        public override void PostStart()
        {
       //     ConceptDecider.TeachOpportunity(ConceptDefOf., OpportunityType.Critical);
        }

        public override bool ForceHostileTo(Thing t)
        {
            return t.Faction != null && this.ForceHostileTo(t.Faction);
        }

        public override bool ForceHostileTo(Faction f)
        {
            return f.def.humanlikeFaction;
        }
    }
}