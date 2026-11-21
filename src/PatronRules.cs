using System;
using System.Collections.Generic;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Controllers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums.Damage;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.Utility;

namespace DemonicPatron
{
    public enum PatronStrikeKind
    {
        Freeze,
        Flame,
        Poison,
        Lightning
    }

    public class PatronElementalDamage : UnitFactComponentDelegate,
        IInitiatorRulebookHandler<RuleCalculateWeaponStats>,
        IRulebookHandler<RuleCalculateWeaponStats>,
        IInitiatorRulebookSubscriber
    {
        public DamageEnergyType Energy;
        public BlueprintFeature Upgrade;
        public int UpgradedDice = 2;

        public void OnEventAboutToTrigger(RuleCalculateWeaponStats evt)
        {
            if (evt?.Weapon == null || !Wields(evt.Weapon))
            {
                return;
            }

            var dice = Upgrade != null && Owner.Descriptor.HasFact(Upgrade) ? UpgradedDice : 1;
            evt.DamageDescription.Add(new DamageDescription
            {
                TypeDescription = new DamageTypeDescription
                {
                    Type = DamageType.Energy,
                    Energy = Energy,
                    Common = new DamageTypeDescription.CommomData(),
                    Physical = new DamageTypeDescription.PhysicalData()
                },
                Dice = new DiceFormula(dice, DiceType.D6),
                SourceFact = Fact
            });
        }

        public void OnEventDidTrigger(RuleCalculateWeaponStats evt)
        {
        }

        bool Wields(ItemEntity weapon)
        {
            var body = Owner?.Body;
            if (body == null)
            {
                return false;
            }

            if (body.PrimaryHand.HasWeapon)
            {
                if (body.PrimaryHand.MaybeWeapon == weapon)
                {
                    return true;
                }
            }
            else if (body.EmptyHandWeapon == weapon)
            {
                return true;
            }

            if (body.SecondaryHand.MaybeWeapon == weapon)
            {
                return true;
            }

            if (body.AdditionalLimbs == null)
            {
                return false;
            }

            foreach (var limb in body.AdditionalLimbs)
            {
                if (limb?.MaybeWeapon == weapon)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public class PatronSneakStrike : UnitFactComponentDelegate,
        IInitiatorRulebookHandler<RuleAttackWithWeapon>,
        IRulebookHandler<RuleAttackWithWeapon>,
        IInitiatorRulebookSubscriber,
        IUnitNewCombatRoundHandler,
        IUnitCombatHandler
    {
        public PatronStrikeKind Kind;
        public int Threshold = 2;
        public bool Persists;
        public BlueprintCharacterClass RogueClass;
        public BlueprintBuff Counter;
        public BlueprintBuff Condition;
        public BlueprintFeature Upgrade;
        public int BurstDice = 2;
        public int UpgradedBurstDice = 4;

        public void OnEventAboutToTrigger(RuleAttackWithWeapon evt)
        {
        }

        public void OnEventDidTrigger(RuleAttackWithWeapon evt)
        {
            var roll = evt?.AttackRoll;
            if (roll == null || roll.IsFake || !roll.IsHit || !IsSneak(roll) || Counter == null)
            {
                return;
            }

            var buff = Owner.Buffs.GetBuff(Counter);
            if (buff == null)
            {
                buff = Owner.Buffs.AddBuff(Counter, Owner, null, null);
                buff?.SetRank(1, false);
            }
            else
            {
                buff.AddRank(1);
            }

            if (buff == null || buff.GetRank() < Threshold)
            {
                return;
            }

            buff.Remove();
            try
            {
                Resolve(evt.Target);
            }
            catch (Exception ex)
            {
                Main.Log(Kind + " strike failed: " + ex.Message);
            }
        }

        public void HandleNewCombatRound(UnitEntityData unit)
        {
            if (!Persists && unit == Owner)
            {
                ClearCounter();
            }
        }

        public void HandleUnitJoinCombat(UnitEntityData unit)
        {
        }

        public void HandleUnitLeaveCombat(UnitEntityData unit)
        {
            if (unit == Owner)
            {
                ClearCounter();
            }
        }

        void ClearCounter()
        {
            if (Counter != null)
            {
                Owner?.Buffs?.GetBuff(Counter)?.Remove();
            }
        }

        void Resolve(UnitEntityData target)
        {
            if (target == null)
            {
                return;
            }

            switch (Kind)
            {
                case PatronStrikeKind.Freeze:
                    if (FailsSave(target, SavingThrowType.Reflex))
                    {
                        Apply(Owner, target, Condition, 1);
                    }
                    break;
                case PatronStrikeKind.Poison:
                    if (FailsSave(target, SavingThrowType.Fortitude))
                    {
                        Apply(Owner, target, Condition, PatronRules.Roll(Owner, DiceType.D3));
                    }
                    break;
                case PatronStrikeKind.Lightning:
                    if (FailsSave(target, SavingThrowType.Will))
                    {
                        Apply(Owner, target, Condition, 1);
                    }
                    break;
                case PatronStrikeKind.Flame:
                    var dice = Upgrade != null && Owner.Descriptor.HasFact(Upgrade) ? UpgradedBurstDice : BurstDice;
                    PatronRules.DamageAround(Owner, target, DamageEnergyType.Fire, dice, Fact);
                    break;
            }
        }

        bool FailsSave(UnitEntityData target, SavingThrowType save)
        {
            var dc = 10 + PatronRules.RogueLevel(Owner, RogueClass) / 2 + PatronRules.IntelligenceModifier(Owner);
            return !Rulebook.Trigger(new RuleSavingThrow(target, save, dc)).IsPassed;
        }

        static void Apply(UnitEntityData caster, UnitEntityData target, BlueprintBuff buff, int rounds)
        {
            if (buff == null || caster == null || target?.Descriptor?.State == null || target.Descriptor.State.IsDead || rounds < 1)
            {
                return;
            }

            target.Buffs.AddBuff(buff, caster, TimeSpan.FromSeconds(6 * rounds), null);
        }

        static bool IsSneak(RuleAttackRoll roll)
        {
            return roll.IsSneakAttack && !roll.ImmuneToSneakAttack && !roll.FortificationNegatesSneakAttack;
        }
    }

    public class FrostRebuke : UnitFactComponentDelegate, IUnitCombatHandler
    {
        public BlueprintAbility Spell;
        public BlueprintCharacterClass RogueClass;
        bool castThisCombat;

        public void HandleUnitJoinCombat(UnitEntityData unit)
        {
            if (unit != Owner || castThisCombat || Spell == null)
            {
                return;
            }

            castThisCombat = true;
            try
            {
                PatronRules.Cast(Owner, Spell, Owner, PatronRules.CasterLevel(Owner, RogueClass));
            }
            catch (Exception ex)
            {
                Main.Log("Frost Rebuke failed: " + ex.Message);
            }
        }

        public void HandleUnitLeaveCombat(UnitEntityData unit)
        {
            if (unit == Owner)
            {
                castThisCombat = false;
            }
        }
    }

    public class StaticDischarge : UnitFactComponentDelegate,
        IInitiatorRulebookHandler<RuleAttackWithWeapon>,
        IRulebookHandler<RuleAttackWithWeapon>,
        IInitiatorRulebookSubscriber,
        IUnitNewCombatRoundHandler,
        IUnitCombatHandler
    {
        public BlueprintAbility Spell;
        public BlueprintBuff Dazed;
        public BlueprintCharacterClass RogueClass;
        bool wasDazed;
        bool usedThisTurn;

        public void OnEventAboutToTrigger(RuleAttackWithWeapon evt)
        {
            wasDazed = PatronRules.HasBuffOrCondition(evt?.Target, Dazed, UnitCondition.Dazed);
        }

        public void OnEventDidTrigger(RuleAttackWithWeapon evt)
        {
            var roll = evt?.AttackRoll;
            if (!wasDazed || usedThisTurn || Spell == null || roll == null || roll.IsFake || !roll.IsHit)
            {
                return;
            }

            usedThisTurn = true;
            try
            {
                PatronRules.Cast(Owner, Spell, evt.Target, PatronRules.CasterLevel(Owner, RogueClass));
            }
            catch (Exception ex)
            {
                Main.Log("Static Discharge failed: " + ex.Message);
            }
        }

        public void HandleNewCombatRound(UnitEntityData unit)
        {
            if (unit == Owner)
            {
                usedThisTurn = false;
            }
        }

        public void HandleUnitJoinCombat(UnitEntityData unit)
        {
        }

        public void HandleUnitLeaveCombat(UnitEntityData unit)
        {
            if (unit == Owner)
            {
                usedThisTurn = false;
            }
        }
    }

    public class SickenedContagion : UnitFactComponentDelegate,
        IInitiatorRulebookHandler<RuleDealDamage>,
        IRulebookHandler<RuleDealDamage>,
        IInitiatorRulebookSubscriber,
        IUnitCombatHandler
    {
        public BlueprintBuff Sickened;
        readonly HashSet<UnitEntityData> marked = new HashSet<UnitEntityData>();
        readonly HashSet<UnitEntityData> exploded = new HashSet<UnitEntityData>();

        public void OnEventAboutToTrigger(RuleDealDamage evt)
        {
            if (!evt.IsFake && PatronRules.HasBuffOrCondition(evt.Target, Sickened, UnitCondition.Sickened))
            {
                marked.Add(evt.Target);
            }
        }

        public void OnEventDidTrigger(RuleDealDamage evt)
        {
            if (evt?.Target == null)
            {
                return;
            }

            var wasSickened = marked.Remove(evt.Target);
            if (!wasSickened || !PatronRules.IsDead(evt.Target))
            {
                return;
            }

            if (!exploded.Add(evt.Target))
            {
                return;
            }

            try
            {
                var victims = PatronRules.EnemiesWithin(Owner, evt.Target, 10);
                foreach (var victim in victims)
                {
                    PatronRules.DealEnergy(Owner, victim, DamageEnergyType.Acid, 2, Fact);
                }

                foreach (var victim in victims)
                {
                    if (!PatronRules.IsDead(victim))
                    {
                        victim.Buffs.AddBuff(Sickened, Owner, TimeSpan.FromSeconds(12), null);
                    }
                }
            }
            catch (Exception ex)
            {
                Main.Log("Sickened Contagion failed: " + ex.Message);
            }
        }

        public void HandleUnitJoinCombat(UnitEntityData unit)
        {
        }

        public void HandleUnitLeaveCombat(UnitEntityData unit)
        {
            if (unit == Owner)
            {
                marked.Clear();
                exploded.Clear();
            }
        }
    }

    static class PatronRules
    {
        public static int RogueLevel(UnitEntityData unit, BlueprintCharacterClass rogue)
        {
            var level = rogue == null ? 0 : unit?.Descriptor?.Progression?.GetClassLevel(rogue) ?? 0;
            return level < 1 ? 1 : level;
        }

        public static int IntelligenceModifier(UnitEntityData unit)
        {
            var stat = unit?.Descriptor?.Stats?.Intelligence;
            return stat == null ? 0 : stat.Bonus;
        }

        public static int CasterLevel(UnitEntityData unit, BlueprintCharacterClass rogue)
        {
            var best = 0;
            var books = unit?.Descriptor?.Spellbooks;
            if (books != null)
            {
                foreach (var book in books)
                {
                    if (book != null && book.CasterLevel > best)
                    {
                        best = book.CasterLevel;
                    }
                }
            }

            return Math.Max(best, RogueLevel(unit, rogue));
        }

        public static int Roll(UnitEntityData unit, DiceType dice)
        {
            var result = Rulebook.Trigger(new RuleRollDice(unit, new DiceFormula(1, dice))).Result;
            return result < 1 ? 1 : result;
        }

        public static bool IsDead(UnitEntityData unit)
        {
            return unit?.Descriptor?.State == null || unit.Descriptor.State.IsDead;
        }

        public static bool HasBuffOrCondition(UnitEntityData unit, BlueprintBuff buff, UnitCondition condition)
        {
            if (unit?.Descriptor == null)
            {
                return false;
            }

            if (buff != null && unit.Buffs.GetBuff(buff) != null)
            {
                return true;
            }

            return unit.Descriptor.State.HasCondition(condition);
        }

        public static void Cast(UnitEntityData caster, BlueprintAbility spell, UnitEntityData target, int casterLevel)
        {
            if (caster?.Descriptor == null || spell == null || target == null)
            {
                return;
            }

            var data = new AbilityData(spell, caster.Descriptor);
            data.OverrideCasterLevel = casterLevel;
            data.SaveSpellbookSlot = true;
            var context = data.CreateExecutionContext(new TargetWrapper(target), data.CalculateParams());
            new AbilityExecutionProcess(context).InstantDeliver();
        }

        public static void DamageAround(UnitEntityData caster, UnitEntityData center, DamageEnergyType energy, int dice, Kingmaker.EntitySystem.EntityFact source)
        {
            foreach (var enemy in EnemiesWithin(caster, center, 10))
            {
                DealEnergy(caster, enemy, energy, dice, source);
            }
        }

        public static void DealEnergy(UnitEntityData caster, UnitEntityData target, DamageEnergyType energy, int dice, Kingmaker.EntitySystem.EntityFact source)
        {
            if (IsDead(target))
            {
                return;
            }

            var damage = new EnergyDamage(new DiceFormula(dice, DiceType.D6), energy);
            damage.SourceFact = source;
            Rulebook.Trigger(new RuleDealDamage(caster, target, damage));
        }

        public static List<UnitEntityData> EnemiesWithin(UnitEntityData caster, UnitEntityData center, int feet)
        {
            var result = new List<UnitEntityData>();
            var units = Game.Instance?.State?.Units;
            if (caster == null || center == null || units == null)
            {
                return result;
            }

            var radius = feet * 0.3048f + 0.05f;
            foreach (var unit in units)
            {
                if (unit == null || unit == caster || IsDead(unit) || !caster.IsEnemy(unit))
                {
                    continue;
                }

                if (center.DistanceTo(unit) <= radius)
                {
                    result.Add(unit);
                }
            }

            return result;
        }
    }
}
