namespace ElZilean
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using LeagueSharp;
    using LeagueSharp.Common;

    internal enum Spells
    {
        Q,

        W,

        E,

        R
    }

    internal class Zilean
    {
        #region Static Fields

        public static Orbwalking.Orbwalker Orbwalker;

        public static Dictionary<Spells, Spell> spells = new Dictionary<Spells, Spell>
                                                             {
                                                                 { Spells.Q, new Spell(SpellSlot.Q, 900) },
                                                                 { Spells.W, new Spell(SpellSlot.W, 0) },
                                                                 { Spells.E, new Spell(SpellSlot.E, 700) },
                                                                 { Spells.R, new Spell(SpellSlot.R, 900) }
                                                             };

        private static SpellSlot ignite;

        #endregion

        #region Public Properties

        public static Obj_AI_Hero Player => ObjectManager.Player;

        #endregion

        #region Public Methods and Operators

        public static void Game_OnGameLoad(EventArgs args)
        {
            if (ObjectManager.Player.CharData.BaseSkinName != "Zilean")
            {
                return;
            }

            spells[Spells.Q].SetSkillshot(0.3f, 210f, 2000f, false, SkillshotType.SkillshotCircle);
            ignite = Player.GetSpellSlot("summonerdot");

            ZileanMenu.Initialize();
            Game.OnUpdate += OnGameUpdate;
            Drawing.OnDraw += Drawings.Drawing_OnDraw;
            Orbwalking.BeforeAttack += OrbwalkingBeforeAttack;
        }

        public static float GetComboDamage(Obj_AI_Base enemy)
        {
            try
            {
                float damage = 0;

                if (!Player.IsWindingUp)
                {
                    damage += (float)ObjectManager.Player.GetAutoAttackDamage(enemy, true);
                }

                if (spells[Spells.Q].IsReady())
                {
                    damage += spells[Spells.Q].GetDamage(enemy);
                }

                return damage;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }

            return 0;
        }

        #endregion

        #region Methods

        private static void Combo()
        {
            var qTarget =
                HeroManager.Enemies.Find(x => x.HasBuff("ZileanQEnemyBomb") && x.IsValidTarget(spells[Spells.Q].Range));
            var target = qTarget ?? TargetSelector.GetTarget(spells[Spells.Q].Range, TargetSelector.DamageType.Magical);

            if (!target.IsValidTarget())
            {
                return;
            }

            TargetSelector.SetTarget(target);
            Orbwalker.ForceTarget(target);

            if (MenuCheck("ElZilean.Combo.E") && spells[Spells.E].IsReady()
                && target.IsValidTarget(spells[Spells.E].Range))
            {
                spells[Spells.E].Cast(target);
            }

            var zileanQEnemyBomb =
               HeroManager.Enemies.Find(x => x.HasBuff("ZileanQEnemyBomb") && x.IsValidTarget(spells[Spells.Q].Range));

            if (MenuCheck("ElZilean.Combo.Q") && spells[Spells.Q].IsReady()
                && target.IsValidTarget(spells[Spells.Q].Range))
            {
                var pred = spells[Spells.Q].GetPrediction(target);
                if (pred.Hitchance >= HitChance.High)
                {
                    spells[Spells.Q].Cast(pred.UnitPosition);
                }
            }

            if (MenuCheck("ElZilean.Combo.W") && zileanQEnemyBomb != null)
            {
                Utility.DelayAction.Add(100, () => { spells[Spells.W].Cast(); });
            }

            if (MenuCheck("ElZilean.Combo.Ignite") && target.IsValidTarget(600f)
                && IgniteDamage(target) >= target.Health)
            {
                Player.Spellbook.CastSpell(ignite, target);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        private static float ComboDamage(Obj_AI_Base target)
        {
            try
            {
                float damage = 0;

                if (!Player.IsWindingUp)
                {
                    damage += (float)ObjectManager.Player.GetAutoAttackDamage(target, true);
                }

                if (spells[Spells.Q].IsReady())
                {
                    damage += spells[Spells.Q].GetDamage(target);
                }

                if (ignite != SpellSlot.Unknown || Player.Spellbook.CanUseSpell(ignite) == SpellState.Ready)
                {
                    damage += (float)Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite);
                }

                return damage;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }

            return 0;
        }

        private static void Flee()
        {
            Orbwalking.MoveTo(Game.CursorPos);

            if (spells[Spells.E].IsReady())
            {
                spells[Spells.E].Cast();
            }

            if (spells[Spells.W].IsReady())
            {
                spells[Spells.W].Cast();
            }
        }

        private static void Harass()
        {
            var target = TargetSelector.GetTarget(spells[Spells.Q].Range, TargetSelector.DamageType.Magical);
            if (target == null)
            {
                return;
            }

            if (MenuCheck("ElZilean.Harass.Q") && spells[Spells.Q].IsReady()
                && target.IsValidTarget(spells[Spells.Q].Range))
            {
                var pred = spells[Spells.Q].GetPrediction(target);
                if (pred.Hitchance >= HitChance.High)
                {
                    spells[Spells.Q].Cast(pred.UnitPosition);
                }
            }

            if (MenuCheck("ElZilean.Harass.E") && spells[Spells.E].IsReady()
                && target.IsValidTarget(spells[Spells.E].Range))
            {
                spells[Spells.E].Cast(target);
            }
        }

        private static float IgniteDamage(Obj_AI_Hero target)
        {
            if (ignite == SpellSlot.Unknown || Player.Spellbook.CanUseSpell(ignite) != SpellState.Ready)
            {
                return 0f;
            }
            return (float)Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite);
        }

        private static void LaneClear()
        {
            var minion = MinionManager.GetMinions(Player.ServerPosition, spells[Spells.Q].Range).FirstOrDefault();
            if (minion == null)
            {
                return;
            }

            var bestFarmLocation =
                MinionManager.GetBestCircularFarmLocation(
                    MinionManager.GetMinions(spells[Spells.Q].Range).Select(m => m.ServerPosition.To2D()).ToList(),
                    spells[Spells.Q].Width,
                    spells[Spells.Q].Range);

            if (MenuCheck("ElZilean.Clear.Q") && minion.IsValidTarget() && spells[Spells.Q].IsReady())
            {
                spells[Spells.Q].Cast(bestFarmLocation.Position);
            }

            if (MenuCheck("ElZilean.Clear.W") && !spells[Spells.Q].IsReady())
            {
                spells[Spells.W].Cast();
            }
        }

        private static bool MenuCheck(string menuName)
        {
            return ZileanMenu.Menu.Item(menuName).IsActive();
        }

        private static void OnGameUpdate(EventArgs args)
        {
            switch (Orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    Combo();
                    break;
                case Orbwalking.OrbwalkingMode.LaneClear:
                    LaneClear();
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    Harass();
                    break;
            }

            UltAlly();
            SelfUlt();

            if (ZileanMenu.Menu.Item("FleeActive").GetValue<KeyBind>().Active)
            {
                Flee();
            }

            if (ZileanMenu.Menu.Item("ElZilean.AutoHarass").GetValue<KeyBind>().Active)
            {
                var target = TargetSelector.GetTarget(spells[Spells.Q].Range, TargetSelector.DamageType.Magical);
                if (!target.IsValidTarget())
                {
                    return;
                }

                if (Player.ManaPercent <= ZileanMenu.Menu.Item("ElZilean.harass.mana").GetValue<Slider>().Value)
                {
                    return;
                }

                if (MenuCheck("ElZilean.UseQAutoHarass") && spells[Spells.Q].IsReady()
                    && target.IsValidTarget(spells[Spells.Q].Range))
                {
                    var prediction = spells[Spells.Q].GetPrediction(target);
                    if (prediction.Hitchance >= HitChance.VeryHigh)
                    {
                        spells[Spells.Q].Cast(target);
                    }
                }

                if (MenuCheck("ElZilean.UseEAutoHarass") && spells[Spells.E].IsReady()
                    && target.IsValidTarget(spells[Spells.E].Range))
                {
                    spells[Spells.E].Cast(target);
                }
            }
        }

        private static void OrbwalkingBeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (ZileanMenu.Menu.Item("AA.Block").IsActive() && Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                args.Process = false;
            }
            else
            {
                if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && spells[Spells.Q].IsReady() && Player.Distance(args.Target) < spells[Spells.Q].Range - 100)
                {
                    args.Process = false;
                }
            }

            if (ZileanMenu.Menu.Item("ElZilean.SupportMode").IsActive())
            {
                if (args.Target is Obj_AI_Minion)
                {
                    args.Process = false;
                }
            }
        }

        private static void SelfUlt()
        {
            if (Player.IsRecalling() || Player.InFountain() || Player.IsInvulnerable || Player.HasBuffOfType(BuffType.SpellImmunity)
               || Player.HasBuffOfType(BuffType.Invulnerability))
            {
                return;
            }

            var useSelftHp = ZileanMenu.Menu.Item("ElZilean.HP").GetValue<Slider>().Value;
            if (MenuCheck("ElZilean.R") && (Player.Health / Player.MaxHealth) * 100 <= useSelftHp
                && spells[Spells.R].IsReady() && Player.CountEnemiesInRange(650) > 0)
            {
                spells[Spells.R].Cast(Player);
            }
        }

        private static void UltAlly()
        {
            if (Player.IsRecalling() || Player.InFountain())
            {
                return;
            }

            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsAlly && !x.IsMe))
            {
                if (MenuCheck("ElZilean.useult")
                    && ((hero.Health / hero.MaxHealth) * 100
                        <= ZileanMenu.Menu.Item("ElZilean.Ally.HP").GetValue<Slider>().Value)
                    && spells[Spells.R].IsReady() && Player.CountEnemiesInRange(1000) > 0
                    && (hero.Distance(Player.ServerPosition) <= spells[Spells.R].Range))
                {
                    if (ZileanMenu.Menu.Item("ElZilean.Cast.Ult.Ally" + hero.CharData.BaseSkinName) != null
                        && ZileanMenu.Menu.Item("ElZilean.Cast.Ult.Ally" + hero.CharData.BaseSkinName).IsActive())
                    {
                        if (hero.IsInvulnerable || hero.HasBuffOfType(BuffType.SpellImmunity) || hero.HasBuffOfType(BuffType.Invulnerability))
                        {
                            return;
                        }

                        spells[Spells.R].Cast(hero);
                    }
                }
            }
        }

        #endregion
    }
}