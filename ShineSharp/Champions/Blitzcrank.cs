﻿using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using ShineCommon;

namespace ShineSharp.Champions
{
    public class Blitzcrank : BaseChamp
    {
        public Blitzcrank()
            : base("Blitzcrank")
        {
        }

        public override void CreateConfigMenu()
        {
            combo = new Menu("Combo", "Combo");
            combo.AddItem(new MenuItem("CUSEQ", "Use Q").SetValue(true));
            combo.AddItem(new MenuItem("CUSEW", "Use W").SetValue(true));
            combo.AddItem(new MenuItem("CUSEE", "Use E").SetValue(true));
            combo.AddItem(new MenuItem("CUSERGRAB", "Use R If Grabbed").SetValue(true));
            combo.AddItem(new MenuItem("CUSERHIT", "Use R If Enemies >=").SetValue(new Slider(2, 1, 5)));
            //
            Menu nograb = new Menu("Grab Filter", "autograb");
            foreach (Obj_AI_Hero enemy in HeroManager.Enemies)
                nograb.AddItem(new MenuItem("nograb" + enemy.ChampionName, string.Format("Dont Grab {0}", enemy.ChampionName)).SetValue(false));
            //
            combo.AddSubMenu(nograb);

            harass = new Menu("Harass", "Harass");
            harass.AddItem(new MenuItem("HUSEQ", "Use Q").SetValue(true));
            harass.AddItem(new MenuItem("HUSEE", "Use E").SetValue(true));
            harass.AddItem(new MenuItem("HMANA", "Min. Mana Percent").SetValue(new Slider(50, 100, 0)));

            laneclear = new Menu("LaneClear", "LaneClear");
            laneclear.AddItem(new MenuItem("LUSER", "Use R").SetValue(true));
            laneclear.AddItem(new MenuItem("LMANA", "Min. Mana Percent").SetValue(new Slider(50, 100, 0)));


            misc = new Menu("Misc", "Misc");
            //
            Menu autograb = new Menu("Auto Grab (Q)", "autograb");
            foreach (Obj_AI_Hero enemy in HeroManager.Enemies)
                autograb.AddItem(new MenuItem("noautograb" + enemy.ChampionName, string.Format("Dont Grab {0}", enemy.ChampionName)).SetValue(false));
            autograb.AddItem(new MenuItem("autograbimmo", "Auto Grab Immobile Target").SetValue(true));
            autograb.AddItem(new MenuItem("MAUTOQHP", "Min. HP Percent").SetValue(new Slider(40, 1, 100)));
            autograb.AddItem(new MenuItem("MAUTOQ", "Enabled").SetValue(true));
            //
            misc.AddSubMenu(autograb);
            //
            Menu interrupt = new Menu("Auto Interrupt", "aintrpt");
            interrupt.AddItem(new MenuItem("MINTQ", "Use Q").SetValue(true));
            interrupt.AddItem(new MenuItem("MINTE", "Use E").SetValue(true));
            interrupt.AddItem(new MenuItem("MINTR", "Use R").SetValue(true));
            interrupt.AddItem(new MenuItem("MINTEN", "Enabled").SetValue(true));
            //
            misc.AddSubMenu(interrupt);
    
            Config.AddSubMenu(combo);
            Config.AddSubMenu(harass);
            Config.AddSubMenu(laneclear);
            Config.AddSubMenu(misc);
            Config.AddToMainMenu();

            OrbwalkingFunctions[(int) Orbwalking.OrbwalkingMode.Combo] += Combo;
            OrbwalkingFunctions[(int) Orbwalking.OrbwalkingMode.Mixed] += Harass;
            OrbwalkingFunctions[(int) Orbwalking.OrbwalkingMode.LaneClear] += LaneClear;
            OrbwalkingFunctions[(int) Orbwalking.OrbwalkingMode.LastHit] += LastHit;
            BeforeOrbWalking += BeforeOrbwalk;

            Interrupter2.OnInterruptableTarget += Interrupter_OnPossibleToInterrupt;
            Obj_AI_Base.OnBuffAdd += Obj_AI_Base_OnBuffAdd;
        }

        public override void SetSpells()
        {
            Spells[Q] = new Spell(SpellSlot.Q, 1000f);
            Spells[Q].SetSkillshot(0.25f, 70f, 1800f, true, SkillshotType.SkillshotLine);
            Spells[W] = new Spell(SpellSlot.W, 200);
            Spells[E] = new Spell(SpellSlot.E, 125);
            Spells[R] = new Spell(SpellSlot.R, 550f);
        }


        public void BeforeOrbwalk()
        {
            #region Auto Harass

            if (Spells[Q].IsReady() && Config.Item("MAUTOQ").GetValue<bool>() && Config.Item("MAUTOQHP").GetValue<Slider>().Value >= ObjectManager.Player.HealthPercent && !ObjectManager.Player.UnderTurret())
            {
                var t = (from enemy in HeroManager.Enemies
                    where enemy.IsValidTarget(Spells[Q].Range - 50)
                    orderby TargetSelector.GetPriority(enemy) descending
                    select enemy).FirstOrDefault();
                if (t != null && !Config.Item("noautograb" + t.ChampionName).GetValue<bool>())
                    CastSkillshot(t, Spells[Q], HitChance.VeryHigh);
            }

            #endregion
        }

        public void Combo()
        {
            bool chase = false;
            if (Spells[R].IsReady() && Spells[Q].IsReady() && Config.Item("CUSERGRAB").GetValue<bool>())
            {
                var t = HeroManager.Enemies.Where(p => p.IsValidTarget(Spells[R].Range + 100)).OrderBy(q => TargetSelector.GetPriority(q)).LastOrDefault();
                if (t != null)
                {
                    if (Config.Item("nograb" + t.ChampionName).GetValue<bool>())
                        return;
                    chase = true;
                    if(Spells[W].IsReady())
                        Spells[W].Cast();
                    if(ObjectManager.Player.ServerPosition.Distance(t.ServerPosition) <= Spells[R].Range - 10)
                        Spells[R].Cast();
                }
            }
            if (!chase)
            {
                if (Spells[Q].IsReady() && Config.Item("CUSEQ").GetValue<bool>())
                {
                    var t = TargetSelector.GetTarget(Spells[Q].Range - 50, TargetSelector.DamageType.Magical);
                    if (t != null)
                    {
                        if (Config.Item("nograb" + t.ChampionName).GetValue<bool>())
                            return;
                        CastSkillshot(t, Spells[Q], HitChance.High);
                    }
                }
                
                if (Spells[R].IsReady())
                {
                    var t = HeroManager.Enemies.Where(p => p.IsValidTarget(Spells[R].Range)).OrderBy(q => q.ServerPosition.Distance(ObjectManager.Player.ServerPosition)).FirstOrDefault();
                    if (t != null)
                    {
                        if (t.HasBuffOfType(BuffType.Knockup) && t.IsValidTarget(Spells[R].Range) && Config.Item("CUSERGRAB").GetValue<bool>())
                            Spells[R].Cast();
                        else
                            Spells[R].CastIfWillHit(t, Config.Item("CUSERHIT").GetValue<Slider>().Value);
                    }
                }
            }
        }

        public void Harass()
        {
            if (ObjectManager.Player.ManaPercent < Config.Item("HMANA").GetValue<Slider>().Value)
                return;

            if (Spells[Q].IsReady() && Config.Item("HUSEQ").GetValue<bool>())
            {
                var target = TargetSelector.GetTarget(Spells[Q].Range - 30, TargetSelector.DamageType.Magical);
                if (target != null)
                    CastSkillshot(target, Spells[Q]);
            }

            if (Spells[E].IsReady() && Config.Item("HUSEE").GetValue<bool>())
            {
                var target = TargetSelector.GetTarget(Spells[E].Range, TargetSelector.DamageType.Physical);
                Spells[E].Cast();
            }
        }

        public void LaneClear()
        {
            if (ObjectManager.Player.ManaPercent < Config.Item("LMANA").GetValue<Slider>().Value)
                return;

            if (Spells[R].IsReady() && Config.Item("LUSER").GetValue<bool>())
            {
                var t =
                    (from minion in
                         MinionManager.GetMinions(Spells[R].Range, MinionTypes.All, MinionTeam.Enemy,
                             MinionOrderTypes.MaxHealth)
                     where
                         minion.IsValidTarget(Spells[R].Range) && Spells[R].GetDamage(minion) >= minion.Health
                     orderby minion.Health ascending
                     select minion);
                if (t != null && t.Count() >= 3)
                    Spells[R].Cast(t.FirstOrDefault().ServerPosition);
            }
        }

        public void LastHit()
        {
            //if (Spells[E].IsReady() && Config.Item("MLASTE").GetValue<bool>())
            //{
            //    var t =
            //        (from minion in
            //             MinionManager.GetMinions(Spells[E].Range, MinionTypes.All, MinionTeam.Enemy,
            //                 MinionOrderTypes.MaxHealth)
            //         where
            //             minion.IsValidTarget(Spells[E].Range) && Spells[E].GetDamage(minion) >= minion.Health &&
            //             (!minion.UnderTurret() &&
            //              minion.Distance(ObjectManager.Player.Position) > ObjectManager.Player.AttackRange)
            //         orderby minion.Health ascending
            //         select minion).FirstOrDefault();
            //}
        }

        public override void Orbwalking_BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (args.Unit.IsMe && Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                if (Spells[E].IsReady())
                    Spells[E].Cast();
            }
        }

        private void Obj_AI_Base_OnBuffAdd(Obj_AI_Base sender, Obj_AI_BaseBuffAddEventArgs args)
        {
            if (args.Buff.Caster.IsAlly && (args.Buff.Type == BuffType.Snare || args.Buff.Type == BuffType.Stun) && sender.IsChampion())
            {
                if (Spells[Q].IsReady() && sender.IsValidTarget(Spells[Q].Range) && Config.Item("autograbimmo").GetValue<bool>() && !Config.Item("noautograb" + (sender as Obj_AI_Hero).ChampionName).GetValue<bool>())
                    Spells[Q].Cast(sender.ServerPosition);
            }
        }

        private void Interrupter_OnPossibleToInterrupt(Obj_AI_Hero sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            if (Config.Item("MINTEN").GetValue<bool>())
            {
                if (Config.Item("MINTQ").GetValue<bool>() && Spells[Q].IsReady())
                    CastSkillshot(sender, Spells[Q], HitChance.Low);

                if (Config.Item("MINTE").GetValue<bool>() && Spells[E].IsReady() && sender.Distance(ObjectManager.Player.ServerPosition) <= Spells[E].RangeSqr)
                {
                    Spells[E].Cast();
                    ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, sender);
                }

                if (Config.Item("MINTR").GetValue<bool>() && Spells[R].IsReady() && sender.Distance(ObjectManager.Player.ServerPosition) <= Spells[R].RangeSqr)
                    Spells[R].Cast();
            }
        }
    }
}



