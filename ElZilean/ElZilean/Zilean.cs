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

        private static SpellSlot _ignite;

        #endregion

        #region Public Properties

        public static Obj_AI_Hero Player
        {
            get
            {
                return ObjectManager.Player;
            }
        }

        #endregion

        #region Public Methods and Operators

        public static void Game_OnGameLoad(EventArgs args)
        {
            if (ObjectManager.Player.CharData.BaseSkinName != "Zilean")
            {
                return;
            }

            spells[Spells.Q].SetSkillshot(0.3f, 210f, 2000f, false, SkillshotType.SkillshotCircle);
            _ignite = Player.GetSpellSlot("summonerdot");

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

        private static void TrickEnemy()
        {
            Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            var target = TargetSelector.GetSelectedTarget();
            if (target == null) return;
            var pred = spells[Spells.Q].GetSPrediction(target);
            if (target.Distance(Player) > spells[Spells.Q].Range) return;
            if (pred.HitChance == HitChance.VeryHigh)
            {
                spells[Spells.Q].Cast(pred.UnitPosition);
                spells[Spells.W].Cast();
                spells[Spells.Q].Cast(pred.UnitPosition);
            }else
            {
                var bestTarget = ObjectManager.Get<Obj_AI_Base>().Where(unit => unit.Distance(target) <= 425 && unit.IsValidTarget() && unit.NetworkId != target.NetworkId).OrderBy(enemy => enemy.Distance(target)).FirstOrDefault();
                if(bestTarget == null)return;
                if (bestTarget.IsMinion)
                {
                    spells[Spells.Q].Cast(bestTarget.Position);
                    spells[Spells.W].Cast();
                    spells[Spells.Q].Cast(bestTarget.Position);
                }
                else
                {
                    var temp = bestTarget as Obj_AI_Hero;
                    if (temp == null) return;
                    pred = spells[Spells.Q].GetSPrediction(temp);
                    if(pred.HitChance >= HitChance.Medium)
                    {
                        spells[Spells.Q].Cast(bestTarget.Position);
                        spells[Spells.W].Cast();
                        spells[Spells.Q].Cast(bestTarget.Position);
                    }
                }
            }


        }
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

            if (MenuCheck("ElZilean.Combo.Q") && spells[Spells.Q].IsReady()
                && target.IsValidTarget(spells[Spells.Q].Range))
            {
                var pred = spells[Spells.Q].GetPrediction(target);
                if (pred.Hitchance >= HitChance.High)
                {
                    spells[Spells.Q].Cast(pred.UnitPosition);
                }
            }

            var zileanQEnemyBomb =
                HeroManager.Enemies.Find(x => x.HasBuff("ZileanQEnemyBomb") && x.IsValidTarget(spells[Spells.Q].Range));

            if (MenuCheck("ElZilean.Combo.W") && zileanQEnemyBomb != null)
            {
                Utility.DelayAction.Add(500, () => { spells[Spells.W].Cast(); });
            }

            if (MenuCheck("ElZilean.Combo.Ignite") && target.IsValidTarget(600f)
                && IgniteDamage(target) >= target.Health)
            {
                Player.Spellbook.CastSpell(_ignite, target);
            }
        }

        private static void Flee()
        {
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
            if (_ignite == SpellSlot.Unknown || Player.Spellbook.CanUseSpell(_ignite) != SpellState.Ready)
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
            if (ZileanMenu._menu.Item("ElZilean.Trick").GetValue<KeyBind>().Active) TrickEnemy();
            if (ZileanMenu.Menu.Item("FleeActive").GetValue<KeyBind>().Active)
            {
                Flee();
            }

            if (ZileanMenu.Menu.Item("ElZilean.AutoHarass").GetValue<KeyBind>().Active)
            {
                var target = TargetSelector.GetTarget(spells[Spells.Q].Range, TargetSelector.DamageType.Magical);
                if (target == null || !target.IsValid)
                {
                    return;
                }

                var mana = ZileanMenu.Menu.Item("ElZilean.harass.mana").GetValue<Slider>().Value;

                if (!(Player.ManaPercent >= mana))
                {
                    return;
                }
                if (MenuCheck("ElZilean.UseQAutoHarass") && spells[Spells.Q].IsReady()
                    && Player.Distance(target) <= spells[Spells.Q].Range)
                {
                    var prediction = spells[Spells.Q].GetPrediction(target);
                    if (prediction.Hitchance >= HitChance.VeryHigh)
                    {
                        spells[Spells.Q].Cast(target);
                    }
                }

                if (MenuCheck("ElZilean.UseEAutoHarass") && spells[Spells.E].IsReady()
                    && Player.Distance(target) <= spells[Spells.E].Range)
                {
                    spells[Spells.E].Cast(target);
                }
            }
        }

        private static void OrbwalkingBeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (ZileanMenu.Menu.Item("ElZilean.SupportMode").GetValue<bool>())
            {
                if (args.Target is Obj_AI_Minion)
                {
                    args.Process = false;
                }
            }
        }

        private static void SelfUlt()
        {
            if (Player.IsRecalling() || Player.InFountain())
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
            var allyMinHp = ZileanMenu.Menu.Item("ElZilean.Ally.HP").GetValue<Slider>().Value;

            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsAlly && !x.IsMe))
            {
                var getAllies = ZileanMenu.Menu.Item("ElZilean.Cast.Ult.Ally" + hero.CharData.BaseSkinName);

                if (Player.IsRecalling() || Player.InFountain())
                {
                    return;
                }
                if (MenuCheck("ElZilean.useult") && ((hero.Health / hero.MaxHealth) * 100 <= allyMinHp)
                    && spells[Spells.R].IsReady() && Player.CountEnemiesInRange(1000) > 0
                    && (hero.Distance(Player.ServerPosition) <= spells[Spells.R].Range))
                {
                    if (getAllies != null && getAllies.GetValue<bool>())
                    {
                        spells[Spells.R].Cast(hero);
                    }
                }
            }
        }

        #endregion
    }
}
