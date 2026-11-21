using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Enums.Damage;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;

namespace DemonicPatron
{
    static class Content
    {
        const string ArchetypeGuid = "d3a10c1100014e6a9b2c000000000001";
        const string SelectionGuid = "d3a10c1100014e6a9b2c000000000002";

        const string RogueGuid = "299aa766dee3cbf4790da4efb8c72484";
        const string EldritchScoundrelGuid = "57f93dd8423c97c49989501281296c4a";
        const string RogueProgressionGuid = "b57b2a75a5abcaf47a01cf84672b50e9";
        const string DangerSenseGuid = "0bcbe9e450b0e7b428f08f66c53c5136";
        const string FinesseTrainingGuid = "b78d146cea711a84598f0acef69462ea";
        const string SickenedGuid = "4e42460798665fd4cb9173ffa7ada323";
        const string StaggeredGuid = "df3950af5a783bd4d91ab73eb8fa0fd3";
        const string DazedGuid = "d2e35b870e4ac574d9873b36402487e5";
        const string VengefulCometsGuid = "0e1272506f9f4480b7c3e7e1e53b6439";
        const string ChainLightningGuid = "645558d63604747428d55f0dd3a4cb58";
        const string FireballGuid = "2d81362af43aeac4387a3d4fced489c3";
        const string AcidArrowGuid = "9a46dfd390f943647ab4395fc997936d";

        const string ArchetypeText =
            "Through a secret pact with a demon, you have gained elemental power. That power makes you a more capable rogue.\n"
            + "A demonic patron follows the training of an eldritch scoundrel. You lose danger sense and finesse training. You still gain Weapon Finesse.\n"
            + "At 3rd level you choose a demonic patron. The patron's gifts appear on their own progression: elemental damage at 3rd level, a sneak-attack strike at 11th level, and a greater gift at 16th level.";

        const string SelectionText =
            "Choose the demon who answers your pact. The patron's progression is separate from your rogue features.\n"
            + "Flauros grants fire. Sithhud grants cold. Pazuzu grants electricity. Aldinach grants acid.";

        public static void Create()
        {
            var rogue = BlueprintUtil.Load<BlueprintCharacterClass>(RogueGuid);
            var scoundrel = BlueprintUtil.Load<BlueprintArchetype>(EldritchScoundrelGuid);
            var danger = BlueprintUtil.Load<BlueprintFeature>(DangerSenseGuid);
            var finesse = BlueprintUtil.Load<BlueprintFeature>(FinesseTrainingGuid);
            var sickened = BlueprintUtil.Load<BlueprintBuff>(SickenedGuid);
            var staggered = BlueprintUtil.Load<BlueprintBuff>(StaggeredGuid);
            var dazed = BlueprintUtil.Load<BlueprintBuff>(DazedGuid);
            var comets = BlueprintUtil.Load<BlueprintAbility>(VengefulCometsGuid);
            var chain = BlueprintUtil.Load<BlueprintAbility>(ChainLightningGuid);
            var fireball = BlueprintUtil.Load<BlueprintAbility>(FireballGuid);
            var acid = BlueprintUtil.Load<BlueprintAbility>(AcidArrowGuid);
            var rogueProgression = BlueprintUtil.Load<BlueprintProgression>(RogueProgressionGuid);

            if (rogue == null || scoundrel == null || danger == null || finesse == null)
            {
                Main.Log("Demonic Patron needs the rogue class, Eldritch Scoundrel, danger sense, and finesse training.");
                return;
            }

            if (HasArchetype(rogue, ArchetypeGuid))
            {
                return;
            }

            var patrons = new List<BlueprintProgression>
            {
                CreatePatron(rogue, rogueProgression, fireball, new PatronSpec
                {
                    Key = "Flauros",
                    Name = "Flauros",
                    Summary = "Flauros, demon of flame, anoints your weapons with fire. Every third sneak attack bursts, and the flame grows hotter at 16th level.",
                    GuidRoot = "d3a10c1100014e6a9b2c00000000002",
                    Energy = DamageEnergyType.Fire,
                    ElementName = "Flauros's Flame",
                    ElementText = "Your weapons burn with Flauros's fire. While this ability is on, each weapon you wield deals an extra 1d6 fire damage. You can turn it off. At 16th level, Profane Fire raises this to 2d6.",
                    StrikeName = "Fiery Strike",
                    StrikeText = "Every 3 successful sneak attacks, the target and each enemy within 10 feet take 2d6 fire damage. The count carries across your turns in the same combat and clears when combat ends. A miss does not reset it. At 16th level the burst deals 4d6 fire damage.",
                    CapName = "Profane Fire",
                    CapText = "Flauros's flame deepens. Your elemental damage increases by 1d6 fire, to 2d6, and Fiery Strike's burst deals an additional 2d6 fire damage, to 4d6.",
                    Kind = PatronStrikeKind.Flame,
                    Threshold = 3,
                    Persists = true,
                    CounterText = "Sneak attacks counted toward the next Fiery Strike. This count carries across your turns and ends when combat ends."
                }),
                CreatePatron(rogue, rogueProgression, comets, new PatronSpec
                {
                    Key = "Sithhud",
                    Name = "Sithhud",
                    Summary = "Sithhud, demon of frost, rimes your weapons. Two sneak attacks in one turn can freeze a foe in place, and battle itself calls down vengeful comets.",
                    GuidRoot = "d3a10c1100014e6a9b2c00000000001",
                    Energy = DamageEnergyType.Cold,
                    ElementName = "Sithhud's Frost",
                    ElementText = "Your weapons are rimed with Sithhud's cold. While this ability is on, each weapon you wield deals an extra 1d6 cold damage. You can turn it off.",
                    StrikeName = "Freezing Strike",
                    StrikeText = "Every 2 successful sneak attacks in the same turn, the target must succeed at a Reflex save or be staggered for 1 round. The DC is 10 + half your rogue level + your Intelligence modifier. The count resets at the start of your next turn and does not carry over. It also ends when combat ends.",
                    CapName = "Frost Rebuke",
                    CapText = "When you enter a battle, you gain the effect of vengeful comets without spending a spell slot. It is cast at your highest caster level.",
                    Kind = PatronStrikeKind.Freeze,
                    Threshold = 2,
                    Persists = false,
                    Condition = staggered,
                    Spell = comets,
                    CounterText = "Sneak attacks counted toward the next Freezing Strike. This count resets at the start of your next turn."
                }),
                CreatePatron(rogue, rogueProgression, chain, new PatronSpec
                {
                    Key = "Pazuzu",
                    Name = "Pazuzu",
                    Summary = "Pazuzu, demon of lightning, charges your weapons. Two sneak attacks in one turn can daze a foe, and a blow against a dazed foe calls chain lightning.",
                    GuidRoot = "d3a10c1100014e6a9b2c00000000004",
                    Energy = DamageEnergyType.Electricity,
                    ElementName = "Pazuzu's Static",
                    ElementText = "Your weapons crackle with Pazuzu's lightning. While this ability is on, each weapon you wield deals an extra 1d6 electricity damage. You can turn it off.",
                    StrikeName = "Static Strike",
                    StrikeText = "Every 2 successful sneak attacks in the same turn, the target must succeed at a Will save or be dazed for 1 round. The DC is 10 + half your rogue level + your Intelligence modifier. The count resets at the start of your next turn and does not carry over. It also ends when combat ends.",
                    CapName = "Static Discharge",
                    CapText = "When you hit a creature that is already dazed, that creature is struck by chain lightning. The spell is cast at your highest caster level and does not spend a spell slot. This happens only once on each of your turns.",
                    Kind = PatronStrikeKind.Lightning,
                    Threshold = 2,
                    Persists = false,
                    Condition = dazed,
                    Spell = chain,
                    CounterText = "Sneak attacks counted toward the next Static Strike. This count resets at the start of your next turn."
                }),
                CreatePatron(rogue, rogueProgression, acid, new PatronSpec
                {
                    Key = "Aldinach",
                    Name = "Aldinach",
                    Summary = "Aldinach, demon of poison, venoms your weapons with acid. Two sneak attacks in one turn can sicken a foe, and a sickened corpse bursts.",
                    GuidRoot = "d3a10c1100014e6a9b2c00000000003",
                    Energy = DamageEnergyType.Acid,
                    ElementName = "Aldinach's Venom",
                    ElementText = "Your weapons drip with Aldinach's venom. While this ability is on, each weapon you wield deals an extra 1d6 acid damage. You can turn it off.",
                    StrikeName = "Sickening Strike",
                    StrikeText = "Every 2 successful sneak attacks in the same turn, the target must succeed at a Fortitude save or be sickened for 1d3 rounds. The DC is 10 + half your rogue level + your Intelligence modifier. The count resets at the start of your next turn and does not carry over. It also ends when combat ends.",
                    CapName = "Sickened Contagion",
                    CapText = "When you kill a sickened enemy, the corpse bursts. Each enemy within 10 feet takes 2d6 acid damage and is sickened for 2 rounds.",
                    Kind = PatronStrikeKind.Poison,
                    Threshold = 2,
                    Persists = false,
                    Condition = sickened,
                    Contagion = true,
                    CounterText = "Sneak attacks counted toward the next Sickening Strike. This count resets at the start of your next turn."
                })
            };

            var selection = CreateSelection(patrons, fireball);
            var archetype = BuildArchetype(scoundrel, rogue, chain ?? fireball);
            var added = BlueprintUtil.CloneEntries(scoundrel.AddFeatures);
            var removed = BlueprintUtil.CloneEntries(scoundrel.RemoveFeatures);
            foreach (var level in new[] { 3, 6, 9, 12, 15, 18 })
            {
                BlueprintUtil.AddAtLevel(removed, level, danger);
            }

            foreach (var level in new[] { 3, 11, 19 })
            {
                BlueprintUtil.AddAtLevel(removed, level, finesse);
            }

            BlueprintUtil.AddAtLevel(added, 3, selection);
            archetype.AddFeatures = added.OrderBy(entry => entry.Level).ToArray();
            archetype.RemoveFeatures = removed.OrderBy(entry => entry.Level).ToArray();
            BlueprintUtil.Register(archetype);
            AddArchetype(rogue, archetype);
            Main.Log("Demonic Patron is ready.");
        }

        static BlueprintProgression CreatePatron(BlueprintCharacterClass rogue, BlueprintProgression template, BlueprintAbility icon, PatronSpec spec)
        {
            var element = CreateElement(rogue, icon, spec);
            var strike = CreateStrike(rogue, icon, spec);
            var capstone = CreateCapstone(rogue, icon, spec);
            var progression = BlueprintUtil.Create<BlueprintProgression>();
            progression.name = "DemonicPatron" + spec.Key + "Progression";
            progression.AssetGuid = BlueprintGuid.Parse(spec.GuidRoot + "0");
            progression.IsClassFeature = true;
            progression.Ranks = 1;
            progression.GiveFeaturesForPreviousLevels = true;
            progression.ForAllOtherClasses = false;
            if (template != null)
            {
                progression.AlternateProgressionType = template.AlternateProgressionType;
            }

            BlueprintUtil.SetName(progression, "DemonicPatron." + spec.Key, spec.Name, spec.Summary);
            BlueprintUtil.CopyIcon(icon, progression);
            var scaling = new BlueprintProgression.ClassWithLevel();
            scaling.AdditionalLevel = 0;
            BlueprintUtil.Set(scaling, "m_Class", rogue.ToReference<BlueprintCharacterClassReference>());
            BlueprintUtil.Set(progression, "m_Classes", new[] { scaling });
            BlueprintUtil.Set(progression, "m_Archetypes", new BlueprintProgression.ArchetypeWithLevel[0]);
            BlueprintUtil.Set(progression, "m_AlternateProgressionClasses", new BlueprintProgression.ClassWithLevel[0]);
            var features = new List<BlueprintFeatureBase> { element, strike, capstone };
            var entries = new List<LevelEntry>();
            BlueprintUtil.AddAtLevel(entries, 3, element);
            BlueprintUtil.AddAtLevel(entries, 11, strike);
            BlueprintUtil.AddAtLevel(entries, 16, capstone);
            progression.LevelEntries = entries.OrderBy(entry => entry.Level).ToArray();
            var group = new UIGroup();
            var refs = features.Select(feature => feature.ToReference<BlueprintFeatureBaseReference>()).ToList();
            BlueprintUtil.Set(group, "m_Features", refs);
            progression.UIGroups = new[] { group };
            BlueprintUtil.Set(progression, "m_UIDeterminatorsGroup", refs.ToArray());
            BlueprintUtil.Set(progression, "Groups", new FeatureGroup[0]);
            BlueprintUtil.Set(progression, "Components", new BlueprintComponent[0]);
            BlueprintUtil.Register(progression);
            return progression;
        }

        static BlueprintFeature CreateElement(BlueprintCharacterClass rogue, BlueprintAbility icon, PatronSpec spec)
        {
            var buff = BlueprintUtil.Create<BlueprintBuff>();
            buff.name = "DemonicPatron" + spec.Key + "ElementBuff";
            buff.AssetGuid = BlueprintGuid.Parse(spec.GuidRoot + "2");
            BlueprintUtil.SetName(buff, "DemonicPatron." + spec.Key + ".ElementBuff", spec.ElementName, spec.ElementText);
            BlueprintUtil.CopyIcon(icon, buff);
            var damage = new PatronElementalDamage
            {
                name = spec.Key + "ElementalDamage",
                Energy = spec.Energy,
                UpgradedDice = 2
            };
            BlueprintUtil.Own(damage, buff);
            BlueprintUtil.Set(buff, "Components", new BlueprintComponent[] { damage });
            BlueprintUtil.Register(buff);

            var toggle = BlueprintUtil.Create<BlueprintActivatableAbility>();
            toggle.name = "DemonicPatron" + spec.Key + "ElementToggle";
            toggle.AssetGuid = BlueprintGuid.Parse(spec.GuidRoot + "3");
            BlueprintUtil.SetName(toggle, "DemonicPatron." + spec.Key + ".ElementToggle", spec.ElementName, spec.ElementText);
            BlueprintUtil.CopyIcon(icon, toggle);
            BlueprintUtil.Set(toggle, "m_Buff", buff.ToReference<BlueprintBuffReference>());
            toggle.IsOnByDefault = true;
            toggle.DoNotTurnOffOnRest = true;
            toggle.ActivationType = AbilityActivationType.Immediately;
            toggle.DeactivateIfCombatEnded = false;
            toggle.OnlyInCombat = false;
            toggle.DeactivateIfOwnerDisabled = false;
            toggle.DeactivateIfOwnerUnconscious = false;
            BlueprintUtil.Set(toggle, "Components", new BlueprintComponent[0]);
            BlueprintUtil.Register(toggle);

            var feature = CreateFeature(spec.GuidRoot + "1", spec.Key + "Element", spec.ElementName, spec.ElementText, icon);
            var facts = new AddFacts { name = spec.Key + "ElementFacts" };
            BlueprintUtil.Set(facts, "m_Facts", new[] { toggle.ToReference<BlueprintUnitFactReference>() });
            BlueprintUtil.Own(facts, feature);
            BlueprintUtil.Set(feature, "Components", new BlueprintComponent[] { facts });
            BlueprintUtil.Register(feature);
            spec.ElementDamage = damage;
            return feature;
        }

        static BlueprintFeature CreateStrike(BlueprintCharacterClass rogue, BlueprintAbility icon, PatronSpec spec)
        {
            var counter = BlueprintUtil.Create<BlueprintBuff>();
            counter.name = "DemonicPatron" + spec.Key + "Counter";
            counter.AssetGuid = BlueprintGuid.Parse(spec.GuidRoot + "5");
            counter.Stacking = StackingType.Rank;
            counter.Ranks = 5;
            BlueprintUtil.SetName(counter, "DemonicPatron." + spec.Key + ".Counter", spec.StrikeName, spec.CounterText);
            BlueprintUtil.CopyIcon(icon, counter);
            BlueprintUtil.Set(counter, "Components", new BlueprintComponent[0]);
            BlueprintUtil.Register(counter);

            var feature = CreateFeature(spec.GuidRoot + "4", spec.Key + "Strike", spec.StrikeName, spec.StrikeText, icon);
            var strike = new PatronSneakStrike
            {
                name = spec.Key + "SneakStrike",
                Kind = spec.Kind,
                Threshold = spec.Threshold,
                Persists = spec.Persists,
                RogueClass = rogue,
                Counter = counter,
                Condition = spec.Condition,
                BurstDice = 2,
                UpgradedBurstDice = 4
            };
            BlueprintUtil.Own(strike, feature);
            BlueprintUtil.Set(feature, "Components", new BlueprintComponent[] { strike });
            BlueprintUtil.Register(feature);
            spec.Strike = strike;
            return feature;
        }

        static BlueprintFeature CreateCapstone(BlueprintCharacterClass rogue, BlueprintAbility icon, PatronSpec spec)
        {
            var feature = CreateFeature(spec.GuidRoot + "6", spec.Key + "Capstone", spec.CapName, spec.CapText, icon);
            var components = new List<BlueprintComponent>();
            if (spec.Kind == PatronStrikeKind.Flame)
            {
                spec.ElementDamage.Upgrade = feature;
                spec.Strike.Upgrade = feature;
            }
            else if (spec.Kind == PatronStrikeKind.Freeze)
            {
                var rebuke = new FrostRebuke
                {
                    name = "FrostRebuke",
                    Spell = spec.Spell,
                    RogueClass = rogue
                };
                BlueprintUtil.Own(rebuke, feature);
                components.Add(rebuke);
            }
            else if (spec.Kind == PatronStrikeKind.Lightning)
            {
                var discharge = new StaticDischarge
                {
                    name = "StaticDischarge",
                    Spell = spec.Spell,
                    Dazed = spec.Condition,
                    RogueClass = rogue
                };
                BlueprintUtil.Own(discharge, feature);
                components.Add(discharge);
            }
            else if (spec.Contagion)
            {
                var contagion = new SickenedContagion
                {
                    name = "SickenedContagion",
                    Sickened = spec.Condition
                };
                BlueprintUtil.Own(contagion, feature);
                components.Add(contagion);
            }

            BlueprintUtil.Set(feature, "Components", components.ToArray());
            BlueprintUtil.Register(feature);
            return feature;
        }

        static BlueprintFeature CreateFeature(string guid, string asset, string title, string description, BlueprintAbility icon)
        {
            var feature = BlueprintUtil.Create<BlueprintFeature>();
            feature.name = "DemonicPatron" + asset;
            feature.AssetGuid = BlueprintGuid.Parse(guid);
            feature.IsClassFeature = true;
            feature.Ranks = 1;
            BlueprintUtil.SetName(feature, "DemonicPatron." + asset, title, description);
            BlueprintUtil.CopyIcon(icon, feature);
            BlueprintUtil.Set(feature, "Groups", new FeatureGroup[0]);
            return feature;
        }

        static BlueprintFeatureSelection CreateSelection(List<BlueprintProgression> patrons, BlueprintAbility icon)
        {
            var selection = BlueprintUtil.Create<BlueprintFeatureSelection>();
            selection.name = "DemonicPatronSelection";
            selection.AssetGuid = BlueprintGuid.Parse(SelectionGuid);
            selection.IsClassFeature = true;
            selection.Ranks = 1;
            selection.Obligatory = true;
            selection.IgnorePrerequisites = true;
            selection.Mode = SelectionMode.Default;
            selection.Group = FeatureGroup.None;
            selection.Group2 = FeatureGroup.None;
            BlueprintUtil.SetName(selection, "DemonicPatron.Selection", "Demonic Patron", SelectionText);
            BlueprintUtil.CopyIcon(icon, selection);
            var references = patrons.Select(patron => patron.ToReference<BlueprintFeatureReference>()).ToArray();
            BlueprintUtil.Set(selection, "m_Features", references);
            BlueprintUtil.Set(selection, "m_AllFeatures", references);
            BlueprintUtil.Set(selection, "Groups", new FeatureGroup[0]);
            BlueprintUtil.Set(selection, "Components", new BlueprintComponent[0]);
            try
            {
                BlueprintUtil.Set(selection, "ShowThisSelection", true);
            }
            catch (MissingFieldException)
            {
            }

            BlueprintUtil.Register(selection);
            return selection;
        }

        static BlueprintArchetype BuildArchetype(BlueprintArchetype scoundrel, BlueprintCharacterClass rogue, BlueprintAbility icon)
        {
            var archetype = BlueprintUtil.Create<BlueprintArchetype>();
            BlueprintUtil.CopyMergedFields(scoundrel, archetype);
            archetype.name = "DemonicPatronArchetype";
            archetype.AssetGuid = BlueprintGuid.Parse(ArchetypeGuid);
            BlueprintUtil.Set(archetype, "m_ParentClass", rogue);
            BlueprintUtil.Set(archetype, "m_HiddenInUI", false);
            archetype.LocalizedName = BlueprintUtil.MakeString("DemonicPatron.Archetype.Name", "Demonic Patron");
            archetype.LocalizedDescription = BlueprintUtil.MakeString("DemonicPatron.Archetype.Desc", ArchetypeText);
            archetype.LocalizedDescriptionShort = BlueprintUtil.MakeString(
                "DemonicPatron.Archetype.Short",
                "An eldritch scoundrel bound to a demon, trading danger sense and finesse training for a patron's elemental gifts.");
            BlueprintUtil.CopySprite(icon, archetype, "m_Icon", "m_Icon");
            return archetype;
        }

        static bool HasArchetype(BlueprintCharacterClass rogue, string guid)
        {
            var field = HarmonyLib.AccessTools.Field(typeof(BlueprintCharacterClass), "m_Archetypes");
            var current = field?.GetValue(rogue) as BlueprintArchetypeReference[];
            return current != null && current.Any(reference => reference != null && reference.Guid == BlueprintGuid.Parse(guid));
        }

        static void AddArchetype(BlueprintCharacterClass rogue, BlueprintArchetype archetype)
        {
            var field = HarmonyLib.AccessTools.Field(typeof(BlueprintCharacterClass), "m_Archetypes");
            var current = (BlueprintArchetypeReference[])field.GetValue(rogue) ?? new BlueprintArchetypeReference[0];
            if (current.Any(reference => reference != null && reference.Guid == archetype.AssetGuid))
            {
                return;
            }

            var list = current.ToList();
            list.Add(archetype.ToReference<BlueprintArchetypeReference>());
            field.SetValue(rogue, list.ToArray());
        }

        class PatronSpec
        {
            public string Key;
            public string Name;
            public string Summary;
            public string GuidRoot;
            public DamageEnergyType Energy;
            public string ElementName;
            public string ElementText;
            public string StrikeName;
            public string StrikeText;
            public string CapName;
            public string CapText;
            public string CounterText;
            public PatronStrikeKind Kind;
            public int Threshold;
            public bool Persists;
            public BlueprintBuff Condition;
            public BlueprintAbility Spell;
            public bool Contagion;
            public PatronElementalDamage ElementDamage;
            public PatronSneakStrike Strike;
        }
    }
}
