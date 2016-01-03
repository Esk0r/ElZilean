namespace ElZilean
{
    using System;
    using System.Drawing;

    using LeagueSharp;
    using LeagueSharp.Common;

    public class Drawings
    {
        #region Public Methods and Operators

        public static void Drawing_OnDraw(EventArgs args)
        {
            var drawOff = ZileanMenu.Menu.Item("ElZilean.Draw.off").GetValue<bool>();
            var drawQ = ZileanMenu.Menu.Item("ElZilean.Draw.Q").GetValue<Circle>();
            var drawW = ZileanMenu.Menu.Item("ElZilean.Draw.W").GetValue<Circle>();
            var drawE = ZileanMenu.Menu.Item("ElZilean.Draw.E").GetValue<Circle>();
            var drawR = ZileanMenu.Menu.Item("ElZilean.Draw.R").GetValue<Circle>();

            if (drawOff)
            {
                return;
            }

            if (drawQ.Active)
            {
                if (Zilean.spells[Spells.Q].Level > 0)
                {
                    Render.Circle.DrawCircle(
                        ObjectManager.Player.Position,
                        Zilean.spells[Spells.Q].Range,
                        Zilean.spells[Spells.Q].IsReady() ? Color.Green : Color.Red);
                }
            }

            if (drawW.Active)
            {
                if (Zilean.spells[Spells.W].Level > 0)
                {
                    Render.Circle.DrawCircle(
                        ObjectManager.Player.Position,
                        Zilean.spells[Spells.W].Range,
                        Zilean.spells[Spells.W].IsReady() ? Color.Green : Color.Red);
                }
            }

            if (drawE.Active)
            {
                if (Zilean.spells[Spells.E].Level > 0)
                {
                    Render.Circle.DrawCircle(
                        ObjectManager.Player.Position,
                        Zilean.spells[Spells.E].Range,
                        Zilean.spells[Spells.E].IsReady() ? Color.Green : Color.Red);
                }
            }

            if (drawR.Active)
            {
                if (Zilean.spells[Spells.R].Level > 0)
                {
                    Render.Circle.DrawCircle(
                        ObjectManager.Player.Position,
                        Zilean.spells[Spells.R].Range,
                        Zilean.spells[Spells.R].IsReady() ? Color.Green : Color.Red);
                }
            }
        }

        #endregion
    }
}