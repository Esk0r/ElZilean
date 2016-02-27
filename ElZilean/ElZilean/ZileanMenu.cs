namespace ElZilean
{
    using System;
    using System.Drawing;
    using System.Linq;

    using LeagueSharp;
    using LeagueSharp.Common;

    public class ZileanMenu
    {
        #region Static Fields

        public static Menu Menu;

        #endregion

        #region Public Methods and Operators

        public static void Initialize()
        {
            Menu = new Menu("ElZilean", "menu", true);

            var orbwalkerMenu = new Menu("Orbwalker", "orbwalker");
            {
                Zilean.Orbwalker = new Orbwalking.Orbwalker(orbwalkerMenu);
            }

            Menu.AddSubMenu(orbwalkerMenu);

            var targetSelector = new Menu("Target Selector", "TargetSelector");
            {
                TargetSelector.AddToMenu(targetSelector);
            }

            Menu.AddSubMenu(targetSelector);

            var comboMenu = Menu.AddSubMenu(new Menu("Combo", "Combo"));
            {
                comboMenu.AddItem(new MenuItem("ElZilean.Combo.Q", "Use Q").SetValue(true));
                comboMenu.AddItem(new MenuItem("ElZilean.Combo.E", "Use E").SetValue(true));
                comboMenu.AddItem(
                    new MenuItem("ElZilean.Combo.W", "Use W to reset Q when target is marked").SetValue(true));
                comboMenu.AddItem(new MenuItem("ElZilean.Combo.Ignite", "Use Ignite").SetValue(true));
                comboMenu.AddItem(
                    new MenuItem("ElZilean.Trick", "Q at nearby units").SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));
            }

            var harassMenu = Menu.AddSubMenu(new Menu("Harass", "Harass"));
            {
                harassMenu.AddItem(new MenuItem("ElZilean.Harass.Q", "Use Q").SetValue(true));
                harassMenu.AddItem(new MenuItem("ElZilean.Harass.E", "Use E").SetValue(true));
                harassMenu.AddItem(
                    new MenuItem("ElZilean.hitChance", "Hitchance").SetValue(
                        new StringList(new[] { "Low", "Medium", "High", "Very High" }, 3)));

                harassMenu.SubMenu("AutoHarass")
                    .AddItem(
                        new MenuItem("ElZilean.AutoHarass", "[Toggle] Auto harass").SetValue(
                            new KeyBind("U".ToCharArray()[0], KeyBindType.Toggle)));
                harassMenu.SubMenu("AutoHarass").AddItem(new MenuItem("spacespacespace", ""));
                harassMenu.SubMenu("AutoHarass")
                    .AddItem(new MenuItem("ElZilean.UseQAutoHarass", "Use Q").SetValue(true));
                harassMenu.SubMenu("AutoHarass")
                    .AddItem(new MenuItem("ElZilean.UseEAutoHarass", "Use E").SetValue(false));
                harassMenu.SubMenu("AutoHarass").AddItem(new MenuItem("spacespacespassce", ""));
                harassMenu.SubMenu("AutoHarass")
                    .AddItem(new MenuItem("ElZilean.harass.mana", "Min % mana for autoharass"))
                    .SetValue(new Slider(55));
            }

            var clearMenu = Menu.AddSubMenu(new Menu("Laneclear", "LC"));
            {
                clearMenu.AddItem(new MenuItem("ElZilean.Clear.Q", "Use Q").SetValue(true));
                clearMenu.AddItem(new MenuItem("ElZilean.Clear.W", "Use W to reset bomb").SetValue(true));
            }

            var castUltMenu = Menu.AddSubMenu(new Menu("Ult settings", "ElZilean.Ally.Ult"));
            {
                castUltMenu.AddItem(new MenuItem("ElZilean.useult", "Use ult on ally").SetValue(true));
                castUltMenu.AddItem(new MenuItem("ElZilean.Ally.HP", "Ally Health %")).SetValue(new Slider(25, 1, 100));
                foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsAlly && !hero.IsMe))
                {
                    castUltMenu.AddItem(
                        new MenuItem("ElZilean.Cast.Ult.Ally" + hero.CharData.BaseSkinName, hero.CharData.BaseSkinName)
                            .SetValue(true));
                }

                castUltMenu.AddItem(new MenuItem("422442fsaasssfs4242f", ""));
                castUltMenu.AddItem(new MenuItem("ElZilean.R", "Cast R")).SetValue(true);
                castUltMenu.AddItem(new MenuItem("ElZilean.HP", "Self Health %")).SetValue(new Slider(25, 1, 100));
            }

            var miscMenu = Menu.AddSubMenu(new Menu("Misc", "Misc"));
            {
                miscMenu.AddItem(new MenuItem("ElZilean.Draw.off", "[Drawing] Drawings off").SetValue(false));
                miscMenu.AddItem(new MenuItem("ElZilean.Draw.Q", "Draw Q").SetValue(new Circle()));
                miscMenu.AddItem(new MenuItem("ElZilean.Draw.W", "Draw W").SetValue(new Circle()));
                miscMenu.AddItem(new MenuItem("ElZilean.Draw.E", "Draw E").SetValue(new Circle()));
                miscMenu.AddItem(new MenuItem("ElZilean.Draw.R", "Draw R").SetValue(new Circle()));

                var dmgAfterE = new MenuItem("ElZilean.DrawComboDamage", "Draw combo damage").SetValue(true);
                var drawFill =
                    new MenuItem("ElZilean.DrawColour", "Fill colour", true).SetValue(new Circle(true, Color.Goldenrod));
                miscMenu.AddItem(drawFill);
                miscMenu.AddItem(dmgAfterE);

                DamageIndicator.DamageToUnit = Zilean.GetComboDamage;
                DamageIndicator.Enabled = dmgAfterE.GetValue<bool>();
                DamageIndicator.Fill = drawFill.GetValue<Circle>().Active;
                DamageIndicator.FillColor = drawFill.GetValue<Circle>().Color;

                dmgAfterE.ValueChanged +=
                    delegate(object sender, OnValueChangeEventArgs eventArgs)
                        {
                            DamageIndicator.Enabled = eventArgs.GetNewValue<bool>();
                        };

                drawFill.ValueChanged += delegate(object sender, OnValueChangeEventArgs eventArgs)
                    {
                        DamageIndicator.Fill = eventArgs.GetNewValue<Circle>().Active;
                        DamageIndicator.FillColor = eventArgs.GetNewValue<Circle>().Color;
                    };
            }

            miscMenu.AddItem(new MenuItem("ElZilean.SupportMode", "Support mode").SetValue(false));

            var sssMenu = Menu.AddSubMenu(new Menu("Super Secret Settings", "SSS"));
            {
                sssMenu.AddItem(
                    new MenuItem("FleeActive", "Flee").SetValue(new KeyBind("A".ToCharArray()[0], KeyBindType.Press)));
            }

            var credits = Menu.AddSubMenu(new Menu("Credits", "jQuery"));
            {
                credits.AddItem(new MenuItem("ElZilean.Paypal", "if you would like to donate via paypal:"));
                credits.AddItem(new MenuItem("ElZilean.Email", "info@zavox.nl"));
            }

            Menu.AddItem(new MenuItem("422442fsaafs4242f", ""));
            Menu.AddItem(new MenuItem("422442fsaafsf", "Version: 1.0.1.5"));
            Menu.AddItem(new MenuItem("fsasfafsfsafsa", "Made By jQuery"));

            Menu.AddToMainMenu();

            Console.WriteLine("Menu Loaded");
        }

        #endregion
    }
}