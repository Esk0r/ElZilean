namespace ElZilean
{
    using System;
    using System.Linq;
    using System.Net;

    using LeagueSharp;
    using LeagueSharp.Common;

    using Color = System.Drawing.Color;

    internal class Zilean
    {

        #region Public Properties

        /// <summary>
        ///     Gets or sets the slot.
        /// </summary>
        /// <value>
        ///     The Smitespell
        /// </value>
        private static Spell IgniteSpell { get; set; }

        #endregion

        #region Properties

        /// <summary>
        ///     Gets or sets the E spell
        /// </summary>
        /// <value>
        ///     The E spell
        /// </value>
        private static Spell E { get; set; }

        /// <summary>
        ///     Gets or sets the menu
        /// </summary>
        /// <value>
        ///     The menu
        /// </value>
        private static Menu Menu { get; set; }

        /// <summary>
        ///     Gets or sets the orbwalker
        /// </summary>
        /// <value>
        ///     The orbwalker
        /// </value>
        private static Orbwalking.Orbwalker Orbwalker { get; set; }

        /// <summary>
        ///     Gets the player.
        /// </summary>
        /// <value>
        ///     The player.
        /// </value>
        private static Obj_AI_Hero Player => ObjectManager.Player;

        /// <summary>
        ///     Check if Zilean has speed passive
        /// </summary>
        private static bool HasSpeedBuff => Player.Buffs.Any(x => x.Name.ToLower().Contains("timewarp"));

        /// <summary>
        ///     Gets or sets the Q spell
        /// </summary>
        /// <value>
        ///     The Q spell
        /// </value>
        private static Spell Q { get; set; }

        /// <summary>
        ///     Gets or sets the R spell.
        /// </summary>
        /// <value>
        ///     The R spell
        /// </value>
        private static Spell R { get; set; }

        /// <summary>
        ///     Gets or sets the W spell
        /// </summary>
        /// <value>
        ///     The W spell
        /// </value>
        private static Spell W { get; set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     Fired when the game loads.
        /// </summary>
        /// <param name="args">The <see cref="EventArgs" /> instance containing the event data.</param>
        public static void OnGameLoad(EventArgs args)
        {
            try
            {
                if (Player.ChampionName != "Zilean")
                {
                    return;
                }

                var igniteSlot = Player.GetSpellSlot("summonerdot");
                if (igniteSlot != SpellSlot.Unknown)
                {
                    IgniteSpell = new Spell(igniteSlot, 600f);
                }

                foreach (var ally in HeroManager.Allies)
                {
                    IncomingDamageManager.AddChampion(ally);
                    Console.WriteLine(@"[ELZILEAN] loaded champions: {0}", ally.ChampionName);
                }

                IncomingDamageManager.Skillshots = true;

                Q = new Spell(SpellSlot.Q, 900f);
                W = new Spell(SpellSlot.W, Orbwalking.GetRealAutoAttackRange(Player));
                E = new Spell(SpellSlot.E, 700f);
                R = new Spell(SpellSlot.R, 900f);

                Q.SetSkillshot(0.7f, 140f, int.MaxValue, false, SkillshotType.SkillshotCircle);

                GenerateMenu();

                Game.OnUpdate += OnUpdate;
                Drawing.OnDraw += OnDraw;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        #endregion

        #region Methods

        /// <summary>
        ///     Creates the menu
        /// </summary>
        /// <value>
        ///     Creates the menu
        /// </value>
        private static void GenerateMenu()
        {
            try
            {
                Menu = new Menu("ElZilean", "ElZilean", true);

                var targetselectorMenu = new Menu("Target Selector", "Target Selector");
                {
                    TargetSelector.AddToMenu(targetselectorMenu);
                }

                Menu.AddSubMenu(targetselectorMenu);

                var orbwalkMenu = new Menu("Orbwalker", "Orbwalker");
                {
                    Orbwalker = new Orbwalking.Orbwalker(orbwalkMenu);
                }

                Menu.AddSubMenu(orbwalkMenu);

                var comboMenu = new Menu("Combo", "Combo");
                {
                    comboMenu.AddItem(new MenuItem("ElZilean.Combo.Q", "Use Q").SetValue(true));
                    comboMenu.AddItem(new MenuItem("ElZilean.Combo.W", "Use W").SetValue(true));
                    comboMenu.AddItem(new MenuItem("ElZilean.Combo.W2", "Always reset Q").SetValue(false))
                        .SetTooltip("Always reset Q even when the target is not marked");
                    comboMenu.AddItem(new MenuItem("ElZilean.Combo.E", "Use E").SetValue(true));
                    comboMenu.AddItem(new MenuItem("ElZilean.Ignite", "Use Ignite").SetValue(true));
                }

                Menu.AddSubMenu(comboMenu);

                var harassMenu = new Menu("Harass", "Harass");
                {
                    harassMenu.AddItem(new MenuItem("ElZilean.Harass.Q", "Use Q").SetValue(true));
                    harassMenu.AddItem(new MenuItem("ElZilean.Harass.W", "Use W").SetValue(true));
                }
                Menu.AddSubMenu(harassMenu);

                var ultimateMenu = new Menu("Ultimate", "Ultimate");
                {
                    ultimateMenu.AddItem(new MenuItem("min-health", "Health percentage").SetValue(new Slider(20, 1)));
                    ultimateMenu.AddItem(new MenuItem("min-damage", "Heal on % incoming damage").SetValue(new Slider(20, 1)));
                    ultimateMenu.AddItem(new MenuItem("ElZilean.Ultimate.R", "Use R").SetValue(true));
                    ultimateMenu.AddItem(new MenuItem("blank-line", ""));
                    foreach (var x in HeroManager.Allies)
                    {
                        ultimateMenu.AddItem(new MenuItem($"R{x.ChampionName}", "Use R on " + x.ChampionName))
                            .SetValue(true);
                    }
                }
                Menu.AddSubMenu(ultimateMenu);

                var laneclearMenu = new Menu("Laneclear", "Laneclear");
                {
                    laneclearMenu.AddItem(new MenuItem("ElZilean.laneclear.Q", "Use Q").SetValue(true));
                    laneclearMenu.AddItem(new MenuItem("ElZilean.laneclear.QMouse", "Cast Q to mouse").SetValue(false))
                        .SetTooltip("Cast Q towards your mouse position");
                    laneclearMenu.AddItem(new MenuItem("ElZilean.laneclear.W", "Use W").SetValue(true));
                    laneclearMenu.AddItem(new MenuItem("ElZilean.laneclear.Mana", "Minimum mana").SetValue(new Slider(20)));
                }

                Menu.AddSubMenu(laneclearMenu);

                var fleeMenu = new Menu("Flee", "Flee");
                {
                    fleeMenu.AddItem(
                        new MenuItem("ElZilean.Flee.Key", "Flee key").SetValue(
                            new KeyBind("A".ToCharArray()[0], KeyBindType.Press)));
                    fleeMenu.AddItem(new MenuItem("ElZilean.Flee.Mana", "Minimum mana").SetValue(new Slider(20)));
                }

                Menu.AddSubMenu(fleeMenu);

                var drawingsMenu = new Menu("Drawings", "Drawings");
                {
                    drawingsMenu.AddItem(new MenuItem("ElZilean.Draw.Off", "Disable drawings").SetValue(false));
                    drawingsMenu.AddItem(new MenuItem("ElZilean.Draw.Q", "Draw Q").SetValue(new Circle()));
                }

                Menu.AddSubMenu(drawingsMenu);

                var miscMenu = new Menu("Misc", "Misc");
                {
                    miscMenu.AddItem(new MenuItem("ElZilean.Q.Stun", "Auto Q on stunned targets").SetValue(false));
                }

                Menu.AddSubMenu(miscMenu);


                Menu.AddToMainMenu();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        /// <summary>
        ///     Called when the game draws itself.
        /// </summary>
        /// <param name="args">The <see cref="EventArgs" /> instance containing the event data.</param>
        private static void OnDraw(EventArgs args)
        {
            try
            {
                if (IsActive("ElZilean.Draw.Off"))
                {
                    return;
                }

                if (Menu.Item("ElZilean.Draw.Q").GetValue<Circle>().Active)
                {
                    if (Q.Level > 0)
                    {
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, Q.Range, Color.DodgerBlue);
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        /// <summary>
        ///     The ignite killsteal logic
        /// </summary>
        private static void HandleIgnite()
        {
            try
            {
                if (Player.GetSpellSlot("summonerdot") == SpellSlot.Unknown)
                {
                    return;
                }

                var kSableEnemy =
                    HeroManager.Enemies.FirstOrDefault(
                        hero =>
                        hero.IsValidTarget(550f) && !hero.HasBuff("summonerdot") && !hero.IsZombie
                        && Player.GetSummonerSpellDamage(hero, Damage.SummonerSpell.Ignite) >= hero.Health);

                if (kSableEnemy != null)
                {
                    Player.Spellbook.CastSpell(IgniteSpell.Slot, kSableEnemy);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        /// <summary>
        ///     Gets the active menu item
        /// </summary>
        /// <value>
        ///     The menu item
        /// </value>
        private static bool IsActive(string menuName)
        {
            return Menu.Item(menuName).IsActive();
        }

        /// <summary>
        ///     Combo logic
        /// </summary>
        private static void OnCombo()
        {
            try
            {
                var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
                if (target == null)
                {
                    return;
                }

                if (IsActive("ElZilean.Combo.E") && E.IsReady())
                {
                    if (Player.GetEnemiesInRange(E.Range).Any())
                    {
                        var closestEnemy =
                            Player.GetEnemiesInRange(E.Range)
                                .OrderByDescending(h => (h.PhysicalDamageDealtPlayer + h.MagicDamageDealtPlayer))
                                .FirstOrDefault();

                        if (closestEnemy == null)
                        {
                            return;
                        }

                        if (closestEnemy.HasBuffOfType(BuffType.Stun))
                        {
                            return;
                        }

                        E.Cast(closestEnemy);
                    }
                                       
                    if (Player.GetAlliesInRange(E.Range).Any() && Player.GetEnemiesInRange(800f).Count >= 1)
                    {
                        var closestToTarget = Player.GetAlliesInRange(E.Range)
                          .OrderByDescending(h => (h.PhysicalDamageDealtPlayer + h.MagicDamageDealtPlayer))
                          .FirstOrDefault();

                        E.Cast(closestToTarget);
                    }
                }

                if (IsActive("ElZilean.Combo.Q") && Q.IsReady() && target.IsValidTarget(Q.Range) && !target.IsZombie)
                {
                    var pred = Q.GetPrediction(target);
                    if (pred.Hitchance >= HitChance.VeryHigh)
                    {
                        Q.Cast(pred.CastPosition);
                    }
                }

                if (IsActive("ElZilean.Combo.W") && IsActive("ElZilean.Combo.W2") && W.IsReady() && !Q.IsReady())
                {
                    if (Q.Instance.CooldownExpires - Game.Time < 3)
                    {
                        return;
                    }

                    W.Cast();
                    Console.WriteLine("ALWAYS RESET Q");
                }

                // Check if target has a bomb
                var isBombed =
                HeroManager.Enemies
                    .Find(x => x.HasBuff("ZileanQEnemyBomb") && x.IsValidTarget(Q.Range));

                if (!isBombed.IsValidTarget())
                {
                    return;
                }
               
                if (isBombed != null && isBombed.IsValidTarget(Q.Range))
                {
                    if (Q.Instance.CooldownExpires - Game.Time < 3)
                    {
                        Console.WriteLine("3");
                        return;
                    }

                    if (IsActive("ElZilean.Combo.W"))
                    {
                        W.Cast();
                    }
                }

                if (IsActive("ElZilean.Ignite") && isBombed != null)
                {
                    if (Player.GetSpellSlot("summonerdot") == SpellSlot.Unknown)
                    {
                        return;
                    }

                    if (Q.GetDamage(isBombed) + IgniteSpell.GetDamage(isBombed) > isBombed.Health)
                    {
                        if (isBombed.IsValidTarget(Q.Range))
                        {
                            Player.Spellbook.CastSpell(IgniteSpell.Slot, isBombed);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        /// <summary>
        ///     E Flee to mouse
        /// </summary>
        private static void OnFlee()
        {
            try
            {
                if (E.IsReady() && Player.Mana > Menu.Item("ElZilean.Flee.Mana").GetValue<Slider>().Value)
                {
                    E.Cast();
                }

                if (HasSpeedBuff)
                {
                    Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                }

                if (!E.IsReady() && W.IsReady())
                {
                    if (HasSpeedBuff)
                    {
                        return;
                    }

                    W.Cast();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        /// <summary>
        ///     Harass logic
        /// </summary>
        private static void OnHarass()
        {
            try
            {
                var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
                if (target == null)
                {
                    return;
                }

                if (IsActive("ElZilean.Harass.Q") && Q.IsReady() && target.IsValidTarget(Q.Range))
                {
                    var pred = Q.GetPrediction(target);
                    if (pred.Hitchance >= HitChance.VeryHigh)
                    {
                        Q.Cast(pred.UnitPosition);
                    }
                }

                if (IsActive("ElZilean.Harass.W") && W.IsReady() && !Q.IsReady())
                {
                    W.Cast();
                    Console.WriteLine("Resetted W");
                }

                // Check if target has a bomb
                var isBombed =
                HeroManager.Enemies
                    .FirstOrDefault(x => x.HasBuff("ZileanQEnemyBomb") && x.IsValidTarget(Q.Range));

                if (isBombed.IsValidTarget())
                {
                    if (IsActive("ElZilean.Harass.W"))
                    {
                        W.Cast();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        /// <summary>
        ///     The laneclear logic
        /// </summary>
        private static void OnLaneclear()
        {
            try
            {
                var minion = MinionManager.GetMinions(Player.Position, Q.Range + Q.Width);
                if (minion == null)
                {
                    return;
                }

                if (Player.ManaPercent < Menu.Item("ElZilean.laneclear.Mana").GetValue<Slider>().Value)
                {
                    return;
                }

                var farmLocation =
                   MinionManager.GetBestCircularFarmLocation(
                       MinionManager.GetMinions(Q.Range).Select(x => x.ServerPosition.To2D()).ToList(),
                       Q.Width,
                       Q.Range);

                if (farmLocation.MinionsHit == 0)
                {
                    return;
                }

                if (IsActive("ElZilean.laneclear.Q") && IsActive("ElZilean.laneclear.QMouse") && Q.IsReady())
                {
                    Q.Cast(Game.CursorPos);
                }

                if (IsActive("ElZilean.laneclear.Q") && Q.IsReady() 
                    && !IsActive("ElZilean.laneclear.QMouse") && farmLocation.MinionsHit >= 3)
                {
                    Q.Cast(farmLocation.Position.To3D());
                }

                if (IsActive("ElZilean.laneclear.W") && W.IsReady())
                {
                    W.Cast();
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        /// <summary>
        ///     Gets the stun duration
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        private static float GetStunDuration(Obj_AI_Base target)
        {
            return target.Buffs.Where(b => b.IsActive && Game.Time < b.EndTime &&
                (b.Type == BuffType.Charm ||
                b.Type == BuffType.Knockback ||
                b.Type == BuffType.Stun ||
                b.Type == BuffType.Invulnerability ||
                b.Type == BuffType.Suppression ||
                b.Type == BuffType.Snare)).Aggregate(0f, (current, buff) => Math.Max(current, buff.EndTime)) -
                Game.Time;
        }

        /// <summary>
        ///     Called when the game updates
        /// </summary>
        /// <param name="args">The <see cref="EventArgs" /> instance containing the event data.</param>
        private static void OnUpdate(EventArgs args)
        {
            try
            {
                if (Player.IsDead)
                {
                    return;
                }

                switch (Orbwalker.ActiveMode)
                {
                    case Orbwalking.OrbwalkingMode.Combo:
                        OnCombo();
                        break;
                    case Orbwalking.OrbwalkingMode.Mixed:
                        OnHarass();
                        break;
                    case Orbwalking.OrbwalkingMode.LaneClear:
                        OnLaneclear();
                        break;
                }

                if (IsActive("ElZilean.Ignite"))
                {
                    HandleIgnite();
                }

                if (Menu.Item("ElZilean.Flee.Key").GetValue<KeyBind>().Active)
                {
                    OnFlee();
                }

                if (IsActive("ElZilean.Q.Stun"))
                {
                    var target = HeroManager.Enemies.FirstOrDefault(h => h.IsValidTarget(Q.Range) && GetStunDuration(h) >= Q.Delay);
                    if (target != null)
                    {
                        if (Q.IsReady())
                        {
                            Q.Cast(target.ServerPosition);
                        }
                        Utility.DelayAction.Add(100, () => W.Cast());
                    }
                }

                foreach (var ally in HeroManager.Allies)
                {
                    if (!Menu.Item($"R{ally.ChampionName}").IsActive() || ally.IsRecalling()
                        || ally.IsInvulnerable)
                    {
                        return;
                    }

                    var enemies = ally.CountEnemiesInRange(750f);
                    var totalDamage = IncomingDamageManager.GetDamage(ally) * 1.1f;
                    if (ally.HealthPercent <= Menu.Item("min-health").GetValue<Slider>().Value && !ally.IsDead && enemies >= 1)
                    {
                        if ((int)(totalDamage / ally.Health) > Menu.Item("min-damage").GetValue<Slider>().Value
                            || ally.HealthPercent < Menu.Item("min-health").GetValue<Slider>().Value)
                        {
                            R.Cast(ally);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        #endregion
    }
}