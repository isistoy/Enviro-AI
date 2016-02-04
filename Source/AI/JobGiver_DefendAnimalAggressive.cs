using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Verse;
using Verse.AI;
using RimWorld;

namespace SK_Enviro.AI
{
    public class JobGiver_DefendAnimal2 : ThinkNode_JobGiver
    {
        public const int THREAT_DISTANCE = 55 * 55;

        protected override Job TryGiveTerminalJob(Pawn pawn)
        {
            TraverseParms traverseParams = TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, true);

            JobDef huntJobDef = Animals_AI.GetHuntForAnimalsJobDef();
            if ((pawn.jobs.curJob == null) || ((pawn.jobs.curJob.def != huntJobDef) && pawn.jobs.curJob.checkOverrideOnExpire))
            {
                Pawn threat = pawn.mindState.meleeThreat;
                Pawn targetOfThreat = pawn;

                if (threat == null)
                {
                    IEnumerable<Pawn> herdMembers = HerdAIUtility_Pets.FindHerdMembers(pawn);
                    foreach (Pawn herdMember in herdMembers)
                    {
                        if (herdMember.mindState.meleeThreat != null)
                        {
                            threat = herdMember.mindState.meleeThreat;
                            pawn.mindState.meleeThreat = threat;
                            targetOfThreat = herdMember;
                            break;
                        }
                    }
                    
                }

                bool foundinjury = false;
                if (pawn.health.hediffSet.GetNaturallyHealingInjuredParts().Any<BodyPartRecord>() && (targetOfThreat.mindState.lastDisturbanceTick - Find.TickManager.TicksGame) > 400)
                {
                foundinjury = true;        
                Log.Message("Somebody attack " + pawn.ToString() + " and we found damage");
                Pawn rangethreat = FindThreat(pawn, traverseParams);
                    if (rangethreat != null)
                    {
                        Log.Message("range threat found= " + rangethreat.ToString());
                        PawnPath pawnPath = PathFinder.FindPath(pawn.Position, rangethreat, TraverseParms.For(pawn, Danger.Deadly, TraverseMode.PassDoors, false), PathEndMode.OnCell);
                        IntVec3 CellInFront;
                        Building_Door door = pawnPath.FirstBlockingBuilding(out CellInFront) as Building_Door;
                        if (door != null)
                        {
                            if (!door.Open)
                            {
                                // release the path
                                pawnPath.ReleaseToPool();
                                return new Job(Animals_AI.GetBashDoorJobDef(), CellInFront, door)
                                {
                                    maxNumMeleeAttacks = 4,
                                    //checkOverrideOnExpire = true,
                                    expiryInterval = 500
                                };
                            }
                        }
                        return new Job(huntJobDef)
                        {
                            targetA = rangethreat,
                            maxNumMeleeAttacks = 4,
                            //checkOverrideOnExpire = true,
                            killIncappedTarget = true,
                            expiryInterval = 500
                        };
                    }
                }
                if (!foundinjury)
                {
                    return null;
                }

                if (threat == null || threat.Dead || threat.Downed
                    || (targetOfThreat.mindState.lastMeleeThreatHarmTick - Find.TickManager.TicksGame) > 300
                    || (targetOfThreat.Position - threat.Position).LengthHorizontalSquared > HerdAIUtility_Pets.HERD_DISTANCE
                    || !GenSight.LineOfSight(pawn.Position, threat.Position))
                {
                    pawn.mindState.meleeThreat = null;
                    return null;
                }
                else
                {
                    return new Job(huntJobDef)
                    {
                        targetA = threat,
                        maxNumMeleeAttacks = 4,
                        killIncappedTarget = true,
                        checkOverrideOnExpire = true,
                        expiryInterval = 500
                    };
                } 
            }
            return null;
        }

        public static Pawn FindThreat(Pawn pawn, TraverseParms traverseParams)
        {
            ThingRequest agressorRequest = ThingRequest.ForGroup(ThingRequestGroup.Pawn);
            Predicate<Thing> availAgressorPredicate = p =>
            {
                Pawn agressor = p as Pawn;
                return isPossibleThreat(agressor, pawn);
            };

            Pawn closestThreat = GenClosest.ClosestThingReachable(pawn.Position, agressorRequest, PathEndMode.Touch, traverseParams, 100f, availAgressorPredicate) as Pawn;
            return closestThreat;
        }

        private static bool isPossibleThreat(Pawn agressor, Pawn hunter)
        {
            return (hunter != agressor)
                && !agressor.Dead
                && !isFriendly(hunter, agressor)
                && !HaveRangedWeapon(hunter, agressor)
                && isNearby(hunter, agressor);
            
        }

        private static bool isFriendly(Pawn hunter, Pawn agressor)
        {
            Pawn pet = (Pawn)hunter;
            if (pet.Faction == Faction.OfColony)
            {
                Pawn agressorT = agressor as Pawn;
                if (agressorT == null)
                {
                    return (agressor.Faction == Faction.OfColony) || (agressor.def == hunter.def) || agressor.IsPrisonerOfColony || agressor.Faction.HostileTo(Faction.OfColony);
                }
                else
                    return (agressor.Faction == Faction.OfColony) || (agressorT.Faction == Faction.OfColony) || agressor.IsPrisonerOfColony || agressorT.Faction.HostileTo(Faction.OfColony);
            }
            else
                return agressor.def == hunter.def;

        }

        private static bool HaveRangedWeapon(Pawn hunter, Pawn agressor)
        {
            Pawn pet = (Pawn)hunter;
            if (agressor.RaceProps.Humanlike)
            {
                if (agressor.Faction == Faction.OfColony)
                {
                    return (agressor.def == hunter.def);
                }
                else
                    return (agressor.def != hunter.def);
            }
            else
                return agressor.def != hunter.def;
        }

        private static bool isNearby(Pawn pawn, Thing thing)
        {
            return (pawn.Position - thing.Position).LengthHorizontalSquared <= THREAT_DISTANCE;
        }
    }
}
