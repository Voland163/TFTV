using Base.Defs;
using PhoenixPoint.Common.Core;
using PhoenixPoint.Common.Entities;
using PhoenixPoint.Common.Entities.GameTagsTypes;
using PhoenixPoint.Geoscape.Entities;
using PhoenixPoint.Geoscape.Entities.PhoenixBases;
using PhoenixPoint.Geoscape.Entities.Research;
using PhoenixPoint.Geoscape.Entities.Sites;
using PhoenixPoint.Geoscape.Levels.Factions;
using PhoenixPoint.Tactical.Entities.Abilities;
using System;
using System.Collections.Generic;
using System.Linq;
using static TFTV.TFTVDrills.DrillsHarmony;
using static TFTV.TFTVDrills.DrillsDefs;

namespace TFTV.TFTVDrills
{
    internal static class DrillsUnlock
    {

        private static readonly DefCache DefCache = TFTVMain.Main.DefCache;
        private static readonly DefRepository Repo = TFTVMain.Repo;
        private static readonly SharedData Shared = TFTVMain.Shared;

        internal sealed class DrillClassLevelRequirement
        {
            public ClassTagDef ClassTag;
            public int MinimumLevel = 1;

        }

        internal sealed class DrillWeaponProficiencyRequirement
        {
            public List<TacticalAbilityDef> ProficiencyAbilities { get; set; } = new List<TacticalAbilityDef>();

        }

        internal sealed class DrillUnlockCondition
        {
            public bool AlwaysAvailable;
            public List<string> RequiredResearchIds { get; } = new List<string>();
            public List<DrillClassLevelRequirement> ClassLevelRequirements { get; } = new List<DrillClassLevelRequirement>();
            public List<DrillWeaponProficiencyRequirement> WeaponProficiencyRequirements { get; } = new List<DrillWeaponProficiencyRequirement>();
            public static DrillUnlockCondition AlwaysUnlocked()
            {
                return new DrillUnlockCondition { AlwaysAvailable = true };
            }
        }

        private static readonly Dictionary<TacticalAbilityDef, DrillUnlockCondition> DrillUnlockConditions = new Dictionary<TacticalAbilityDef, DrillUnlockCondition>();

        public static List<TacticalAbilityDef> GetAvailableDrills(GeoPhoenixFaction faction, GeoCharacter viewer)
        {
            var results = new List<TacticalAbilityDef>();
            if (Drills == null || Drills.Count == 0)
            {
                return results;
            }

            /* if (!HasFunctioningTrainingFacility(faction))
             {

                 TFTVLogger.Always($"GetAvailableDrills: !HasFunctioningTrainingFacility(faction)");
                 return results;
             }*/


            foreach (var ability in Drills)
            {
                if (ability == null)
                {
                    continue;
                }

                if (DrillsUnlock.CharacterHasDrill(viewer, ability))
                {
                    continue;
                }

                if (DrillsUnlock.IsDrillUnlocked(faction, viewer, ability))
                {
                    results.Add(ability);
                }
            }

            return results;
        }

        public static bool IsDrillUnlocked(GeoPhoenixFaction faction, GeoCharacter viewer, TacticalAbilityDef ability)
        {
            if (ability == null)
            {
                return false;
            }

            if (!HasFunctioningTrainingFacility(faction))
            {

                //  TFTVLogger.Always($"IsDrillUnlocked: !HasFunctioningTrainingFacility(faction)");
                return false;
            }

            if (!DrillUnlockConditions.TryGetValue(ability, out var condition) || condition == null)
            {
                return true;
            }

            if (condition.AlwaysAvailable)
            {
                return true;
            }

            if (!MeetsResearchRequirements(faction, condition))
            {
                return false;
            }

            if (!MeetsClassLevelRequirements(viewer, condition))
            {
                return false;
            }

            if (!MeetsWeaponProficiencyRequirements(viewer, condition))
            {
                return false;
            }


            return true;
        }

        public static void SetUnlockCondition(TacticalAbilityDef ability, DrillUnlockCondition condition)
        {
            if (ability == null)
            {
                return;
            }


            DrillUnlockConditions[ability] = condition ?? DrillUnlockCondition.AlwaysUnlocked();
        }

        internal static bool CharacterHasDrill(GeoCharacter soldier, TacticalAbilityDef drill)
        {
            if (soldier?.Progression?.Abilities == null || drill == null)
            {
                return false;
            }

            return soldier.Progression.Abilities.Contains(drill);
        }

        internal static IEnumerable<string> GetMissingRequirementDescriptions(GeoPhoenixFaction faction, GeoCharacter viewer, TacticalAbilityDef ability)
        {
            if (ability == null)
            {
                yield break;
            }

            if (!DrillUnlockConditions.TryGetValue(ability, out var condition) || condition == null)
            {
                yield break;
            }

            if (condition.AlwaysAvailable)
            {
                yield break;
            }

            if (!MeetsResearchRequirements(faction, condition))
            {
                foreach (var researchId in condition.RequiredResearchIds)
                {
                    if (string.IsNullOrEmpty(researchId))
                    {
                        continue;
                    }

                    bool completed = faction?.Research?.HasCompleted(researchId) ?? false;
                    if (completed)
                    {
                        continue;
                    }

                    string researchName = TryGetResearchName(researchId);
                    if (!string.IsNullOrEmpty(researchName))
                    {
                        yield return $"Research: {researchName}";
                    }
                }
            }

            if (!MeetsClassLevelRequirements(viewer, condition))
            {
                foreach (var requirement in condition.ClassLevelRequirements)
                {
                    if (requirement == null)
                    {
                        continue;
                    }

                    bool satisfied = false;
                    if (viewer != null && MeetsSingleClassRequirement(viewer, requirement))
                    {
                        satisfied = true;
                    }

                    if (satisfied)
                    {
                        continue;
                    }

                    yield return BuildClassRequirementMessage(requirement);
                }
            }

            if (!MeetsWeaponProficiencyRequirements(viewer, condition))
            {
                foreach (var requirement in condition.WeaponProficiencyRequirements)
                {
                    if (requirement?.ProficiencyAbilities == null || requirement.ProficiencyAbilities.Count == 0)
                    {
                        continue;
                    }

                    bool satisfied = false;
                    if (viewer != null && SoldierHasWeaponProficiency(viewer, requirement.ProficiencyAbilities))
                    {
                        satisfied = true;
                    }

                    if (satisfied)
                    {
                        continue;
                    }

                    yield return BuildWeaponProficiencyRequirementMessage(requirement);
                }
            }
        }

        internal static bool HasFunctioningTrainingFacility(GeoPhoenixFaction faction)
        {
            try
            {
                if (faction?.Bases == null)
                {
                    return false;
                }

                PhoenixFacilityDef trainingFacilityDef = DefCache.GetDef<PhoenixFacilityDef>("TrainingFacility_PhoenixFacilityDef");
                if (trainingFacilityDef == null)
                {
                    return false;
                }

                foreach (GeoPhoenixBase phoenixBase in faction.Bases)
                {
                    if (phoenixBase?.Layout?.Facilities == null)
                    {
                        continue;
                    }

                    bool hasFunctioningFacility = phoenixBase.Layout.Facilities.Any(facility =>
                        facility != null &&
                        facility.Def == trainingFacilityDef &&
                        facility.State == GeoPhoenixFacility.FacilityState.Functioning &&
                        facility.IsPowered);

                    if (hasFunctioningFacility)
                    {
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                TFTVLogger.Error(e);
            }

            return false;
        }

        internal static bool MeetsClassLevelRequirements(GeoCharacter viewer, DrillUnlockCondition condition)
        {
            if (condition?.ClassLevelRequirements == null || condition.ClassLevelRequirements.Count == 0)
            {
                return true;
            }

            foreach (var requirement in condition.ClassLevelRequirements)
            {
                if (requirement == null)
                {
                    continue;
                }

                bool satisfied = false;

                if (viewer != null && MeetsSingleClassRequirement(viewer, requirement))
                {
                    satisfied = true;
                }

                if (!satisfied)
                {
                    // TODO: decide whether we should surface unmet requirements to the UI.
                    return false;
                }
            }

            return true;
        }

        internal static bool MeetsResearchRequirements(GeoPhoenixFaction faction, DrillUnlockCondition condition)
        {
            if (condition?.RequiredResearchIds == null || condition.RequiredResearchIds.Count == 0)
            {
                return true;
            }

            foreach (var researchId in condition.RequiredResearchIds)
            {

                if (!faction.Research.HasCompleted(researchId))
                {
                    return false;
                }
            }

            return true;
        }


        internal static bool MeetsSingleClassRequirement(GeoCharacter soldier, DrillClassLevelRequirement requirement)
        {
            if (soldier == null || requirement == null)
            {
                return false;
            }

            if (requirement.ClassTag != null && (soldier.ClassTags == null || !soldier.ClassTags.Contains(requirement.ClassTag)))
            {
                return false;
            }

            if (soldier.LevelProgression == null)
            {
                return false;
            }

            return soldier.LevelProgression.Level >= requirement.MinimumLevel;
        }

        internal static bool MeetsWeaponProficiencyRequirements(GeoCharacter viewer, DrillUnlockCondition condition)
        {
            if (condition?.WeaponProficiencyRequirements == null || condition.WeaponProficiencyRequirements.Count == 0)
            {
                return true;
            }

            foreach (var requirement in condition.WeaponProficiencyRequirements)
            {
                if (requirement?.ProficiencyAbilities == null || requirement.ProficiencyAbilities.Count == 0)
                {
                    continue;
                }

                bool satisfied = false;

                if (viewer != null && SoldierHasWeaponProficiency(viewer, requirement.ProficiencyAbilities))
                {
                    satisfied = true;
                }

                if (!satisfied)
                {
                    return false;
                }
            }

            return true;
        }

        private static string BuildClassRequirementMessage(DrillClassLevelRequirement requirement)
        {
            string className = requirement.ClassTag?.className;
            if (string.IsNullOrEmpty(className))
            {
                className = "operative";
            }

            // string subject = "Selected operative";
            return $"Level: {requirement.MinimumLevel} {className}";
        }

        private static string BuildWeaponProficiencyRequirementMessage(DrillWeaponProficiencyRequirement requirement)
        {
            List<string> abilityNames = requirement?.ProficiencyAbilities?
                .Where(ability => ability != null)
                .Select(ability => ability.ViewElementDef?.DisplayName1?.Localize() ?? ability.name)
                .Where(name => !string.IsNullOrEmpty(name))
                .Distinct()
                .ToList();

            if (abilityNames == null || abilityNames.Count == 0)
            {
                abilityNames = new List<string> { "required weapon proficiency" };
            }

            string abilityRequirement = abilityNames.Count == 1 ? abilityNames[0] : string.Join(" or ", abilityNames);
            string subject = "Selected operative";
            return $"{subject} must have {abilityRequirement}.";
        }

        internal static void ConfigureUnlockConditions()
        {
            var shockDrop = new DrillUnlockCondition();
            shockDrop.WeaponProficiencyRequirements.Add(new DrillWeaponProficiencyRequirement()
            {
                ProficiencyAbilities = new List<TacticalAbilityDef>()
                {
                    DefCache.GetDef<ClassProficiencyAbilityDef>("Rocketeer_AbilityDef"),
                    DefCache.GetDef<ClassProficiencyAbilityDef>("Heavy_ClassProficiency_AbilityDef"),
                },

            });

                        SetUnlockCondition(_shockDrop, shockDrop);

            var pounceProtocol = new DrillUnlockCondition();
            pounceProtocol.ClassLevelRequirements.Add(new DrillClassLevelRequirement
            {
                ClassTag = DefCache.GetDef<ClassTagDef>("Infiltrator_ClassTagDef"),
                MinimumLevel = 5
            });

                        SetUnlockCondition(_pounceProtocol, pounceProtocol);

            var mightMakesRight = new DrillUnlockCondition();
            mightMakesRight.WeaponProficiencyRequirements.Add(new DrillWeaponProficiencyRequirement()
            {
                ProficiencyAbilities = new List<TacticalAbilityDef>()
                {
                    DefCache.GetDef<ClassProficiencyAbilityDef>("Berserker_ClassProficiency_AbilityDef"),
                    DefCache.GetDef<PassiveModifierAbilityDef>("MeleeWeaponTalent_AbilityDef")
                },

            });

            mightMakesRight.ClassLevelRequirements.Add(new DrillClassLevelRequirement { ClassTag = null, MinimumLevel = 5 });
       
                        SetUnlockCondition(_mightMakesRight, mightMakesRight);

            var heavyConditioning = new DrillUnlockCondition();

            heavyConditioning.ClassLevelRequirements.Add(new DrillClassLevelRequirement { ClassTag = DefCache.GetDef<ClassTagDef>("Heavy_ClassTagDef"), MinimumLevel = 4 });
      
                        SetUnlockCondition(_heavyConditioning, heavyConditioning);

            var partingShot = new DrillUnlockCondition();
            partingShot.ClassLevelRequirements.Add(new DrillClassLevelRequirement { ClassTag = null, MinimumLevel = 5 });
            partingShot.WeaponProficiencyRequirements.Add(new DrillWeaponProficiencyRequirement()
            {
                ProficiencyAbilities = new List<TacticalAbilityDef>()
                {
                    DefCache.GetDef<ClassProficiencyAbilityDef>("Sniper_ClassProficiency_AbilityDef"),
                    DefCache.GetDef<ClassProficiencyAbilityDef>("Berserker_ClassProficiency_AbilityDef"),
                    DefCache.GetDef<PassiveModifierAbilityDef>("HandgunsTalent_AbilityDef")
                },

            });
       
                        SetUnlockCondition(_partingShot, partingShot);


            var neurolink = new DrillUnlockCondition();
            
            neurolink.WeaponProficiencyRequirements.Add(new DrillWeaponProficiencyRequirement()
            {
                ProficiencyAbilities = new List<TacticalAbilityDef>()
                {
                    _commandOverlay
                },

            });
            
            neurolink.WeaponProficiencyRequirements.Add(new DrillWeaponProficiencyRequirement()
            {
                ProficiencyAbilities = new List<TacticalAbilityDef>()
                {
                   _remoteControlAbilityDef
                },
            });

            SetUnlockCondition(_neuralLink, neurolink);

            var bulletHell = new DrillUnlockCondition();
            bulletHell.ClassLevelRequirements.Add(new DrillClassLevelRequirement { ClassTag = null, MinimumLevel = 5 });
            bulletHell.WeaponProficiencyRequirements.Add(new DrillWeaponProficiencyRequirement()
            {
                ProficiencyAbilities = new List<TacticalAbilityDef>()
                {
                    DefCache.GetDef<ClassProficiencyAbilityDef>("Assault_ClassProficiency_AbilityDef"),
                    DefCache.GetDef<PassiveModifierAbilityDef>("AssaultRiflesTalent_AbilityDef")
                },

            });
         
                        SetUnlockCondition(_bulletHell, bulletHell);

            var explosiveShoot = new DrillUnlockCondition();
            explosiveShoot.ClassLevelRequirements.Add(new DrillClassLevelRequirement
            {
                ClassTag = null,
                MinimumLevel = 5,

            });

            explosiveShoot.WeaponProficiencyRequirements.Add(new DrillWeaponProficiencyRequirement()
            {
                ProficiencyAbilities = new List<TacticalAbilityDef>()
                { DefCache.GetDef<ClassProficiencyAbilityDef>("Sniper_ClassProficiency_AbilityDef"),
                    DefCache.GetDef<PassiveModifierAbilityDef>("SniperTalent_AbilityDef")

                },
            }
          );
    
                        SetUnlockCondition(_explosiveShot, explosiveShoot);

            var heavySharpshot = new DrillUnlockCondition();

            heavySharpshot.ClassLevelRequirements.Add(new DrillClassLevelRequirement
            {
                ClassTag = null,
                MinimumLevel = 4,

            });

            heavySharpshot.WeaponProficiencyRequirements.Add(new DrillWeaponProficiencyRequirement()
            {
                ProficiencyAbilities = new List<TacticalAbilityDef>()
            {
            DefCache.GetDef<ClassProficiencyAbilityDef>("Heavy_ClassProficiency_AbilityDef"),
                    DefCache.GetDef<PassiveModifierAbilityDef>("HeavyWeaponsTalent_AbilityDef")

            }
            });

                        SetUnlockCondition(_heavySharpshot, heavySharpshot);


            var aksuSprint = new DrillUnlockCondition();
            aksuSprint.ClassLevelRequirements.Add(new DrillClassLevelRequirement
            {
                ClassTag = DefCache.GetDef<ClassTagDef>("Berserker_ClassTagDef"),
                MinimumLevel = 1,

            });

            aksuSprint.RequiredResearchIds.Add("ANU_Berserker_ResearchDef");

                        SetUnlockCondition(_aksuSprint, aksuSprint);

            var packLoyalty = new DrillUnlockCondition();
            packLoyalty.WeaponProficiencyRequirements.Add(new DrillWeaponProficiencyRequirement()
            {
                ProficiencyAbilities = new List<TacticalAbilityDef>() { DefCache.GetDef<ApplyStatusAbilityDef>("PsychicWard_AbilityDef") },


            });
            SetUnlockCondition(_packLoyalty, packLoyalty);

            var viralGrip = new DrillUnlockCondition();
            viralGrip.ClassLevelRequirements.Add(new DrillClassLevelRequirement
            {
                ClassTag = DefCache.GetDef<ClassTagDef>("Priest_ClassTagDef"),
                MinimumLevel = 5,

            });
            viralGrip.RequiredResearchIds.Add("ANU_AdvancedInfectionTech_ResearchDef");
            SetUnlockCondition(_virulentGrip, viralGrip);

            var viralPuppeteer = new DrillUnlockCondition();
            viralPuppeteer.ClassLevelRequirements.Add(new DrillClassLevelRequirement
            {
                ClassTag = DefCache.GetDef<ClassTagDef>("Priest_ClassTagDef"),
                MinimumLevel = 7,

            });
            viralPuppeteer.RequiredResearchIds.Add("ANU_AdvancedInfectionTech_ResearchDef");
    
                        SetUnlockCondition(_viralPuppeteer, viralPuppeteer);


            var ordnanceResupply = new DrillUnlockCondition();
            ordnanceResupply.ClassLevelRequirements.Add(new DrillClassLevelRequirement
            {
                ClassTag = DefCache.GetDef<ClassTagDef>("Technician_ClassTagDef"),
                MinimumLevel = 5,

            });
            SetUnlockCondition(_ordnanceResupply, ordnanceResupply);


            var toxicLink = new DrillUnlockCondition();
            toxicLink.ClassLevelRequirements.Add(new DrillClassLevelRequirement
            {
                ClassTag = DefCache.GetDef<ClassTagDef>("Infiltrator_ClassTagDef"),
                MinimumLevel = 7,

            });
            toxicLink.RequiredResearchIds.Add("SYN_PoisonWeapons_ResearchDef");
            SetUnlockCondition(_toxicLink, toxicLink);


            var causticJamming = new DrillUnlockCondition();
            causticJamming.ClassLevelRequirements.Add(new DrillClassLevelRequirement
            {
                ClassTag = DefCache.GetDef<ClassTagDef>("Infiltrator_ClassTagDef"),
                MinimumLevel = 5,

            });
            causticJamming.RequiredResearchIds.Add("SYN_PoisonWeapons_ResearchDef");
           SetUnlockCondition(_causticJamming, causticJamming);

            var mentorUnlock = new DrillUnlockCondition();
            mentorUnlock.ClassLevelRequirements.Add(new DrillClassLevelRequirement
            {
                ClassTag = null,
                MinimumLevel = 7,

            });
            SetUnlockCondition(_mentorProtocol, mentorUnlock);

            var pintpointToss = new DrillUnlockCondition();
            pintpointToss.ClassLevelRequirements.Add(new DrillClassLevelRequirement
            {
                ClassTag = null,
                MinimumLevel = 3,

            });
            SetUnlockCondition(_pinpointToss, pintpointToss);

            var oneHandedGrip = new DrillUnlockCondition();
            oneHandedGrip.ClassLevelRequirements.Add(new DrillClassLevelRequirement
            {
                ClassTag = null,
                MinimumLevel = 5,

            });
            SetUnlockCondition(OneHandedGrip.OneHandedPenaltyAbilityManager.OneHandedGrip, oneHandedGrip);

            var snapBrace = new DrillUnlockCondition();
            snapBrace.RequiredResearchIds.Add("PX_RiotShield_ResearchDef");
            SetUnlockCondition(_snapBrace, snapBrace);


            var shieldedRiposte = new DrillUnlockCondition();
            shieldedRiposte.RequiredResearchIds.Add("PX_RiotShield_ResearchDef");
            shieldedRiposte.ClassLevelRequirements.Add(new DrillClassLevelRequirement
            {
                ClassTag = DefCache.GetDef<ClassTagDef>("Heavy_ClassTagDef"),
                MinimumLevel = 2,

            });
            SetUnlockCondition(_shieldedRiposte, shieldedRiposte);

            var overRide = new DrillUnlockCondition();
            overRide.ClassLevelRequirements.Add(new DrillClassLevelRequirement
            {
                ClassTag = DefCache.GetDef<ClassTagDef>("Technician_ClassTagDef"),
                MinimumLevel = 6,

            });
            SetUnlockCondition(_override, overRide);
        }

        internal static void EnsureDefaultUnlockConditions()
        {
            if (Drills == null)
            {
                return;
            }

            foreach (var ability in Drills)
            {
                if (ability == null)
                {
                    continue;
                }

                if (!DrillUnlockConditions.ContainsKey(ability))
                {
                    DrillUnlockConditions[ability] = DrillUnlockCondition.AlwaysUnlocked();
                }
            }
        }

        private static bool SoldierHasWeaponProficiency(GeoCharacter soldier, IEnumerable<TacticalAbilityDef> proficiencyAbilities)
        {
            if (soldier?.Progression?.Abilities == null || proficiencyAbilities == null)
            {
                return false;
            }

            foreach (var ability in proficiencyAbilities)
            {
                if (ability != null && soldier.Progression.Abilities.Contains(ability))
                {
                    return true;
                }
            }

            return false;
        }
        private static string TryGetResearchName(string researchId)
        {
            try
            {
                ResearchDef researchDef = null;

                try
                {
                    researchDef = DefCache.GetDef<ResearchDef>(researchId);
                }
                catch
                {
                    // ignored – fall back to id string.
                }

                if (researchDef?.ViewElementDef != null)
                {
                    if (researchDef.ViewElementDef.ResearchName != null)
                    {
                        return researchDef.ViewElementDef.ResearchName.Localize();
                    }

                    if (researchDef.ViewElementDef.DisplayName1 != null)
                    {
                        return researchDef.ViewElementDef.DisplayName1.Localize();
                    }
                }

                return researchId;
            }
            catch (Exception ex)
            {
                TFTVLogger.Error(ex);
                return researchId;
            }
        }
    }
}