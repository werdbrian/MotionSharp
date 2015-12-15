﻿using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;


namespace e.Motion_Katarina{
    class Program {

        #region Declaration
        private static Spell Q, W, E, R;
        private static Orbwalking.Orbwalker _orbwalker;
        private static Menu _menu;
        private static int whenToCancelR;
        private static Obj_AI_Hero Player { get { return ObjectManager.Player; } }
        private static Obj_AI_Hero qTarget;
        private static readonly Obj_AI_Hero[] AllEnemy = HeroManager.Enemies.ToArray();
        private static bool WardJumpReady;
        private static SpellSlot IgniteSpellSlot;
        #endregion



        static void Game_OnGameLoad(EventArgs args) {
            //Wird aufgerufen, wenn LeagueSharp Injected
            if (Player.ChampionName != "Katarina")
            {
                return;
            }
            #region Spells
            Q = new Spell(SpellSlot.Q, 675, TargetSelector.DamageType.Magical);
            W = new Spell(SpellSlot.W, 375, TargetSelector.DamageType.Magical);
            E = new Spell(SpellSlot.E, 700, TargetSelector.DamageType.Magical);
            R = new Spell(SpellSlot.R, 550, TargetSelector.DamageType.Magical);
            //Get Ignite
            if (Player.Spellbook.GetSpell(SpellSlot.Summoner1).Name.Contains("summonerdot"))
            {
                IgniteSpellSlot = SpellSlot.Summoner1;
            }
            if (Player.Spellbook.GetSpell(SpellSlot.Summoner2).Name.Contains("summonerdot"))
            {
                IgniteSpellSlot = SpellSlot.Summoner2;
            }


            #endregion

            Utility.HpBarDamageIndicator.Enabled = true;
            Utility.HpBarDamageIndicator.DamageToUnit = CalculateDamage;

            
            #region Menu
            _menu = new Menu("e.Motion Katarina", "motion.katarina", true);

            //Orbwalker-Menü
            Menu orbwalkerMenu = new Menu("Orbwalker", "motion.katarina.orbwalker");
            _orbwalker = new Orbwalking.Orbwalker(orbwalkerMenu);
            _menu.AddSubMenu(orbwalkerMenu);

            //Combo-Menü
            Menu comboMenu = new Menu("Combo", "motion.katarina.Combo");
            comboMenu.AddItem(new MenuItem("motion.katarina.Combo.useq", "Use Q").SetValue(true));
            comboMenu.AddItem(new MenuItem("motion.katarina.Combo.usew", "Use W").SetValue(true));
            comboMenu.AddItem(new MenuItem("motion.katarina.Combo.usee", "Use E").SetValue(true));
            comboMenu.AddItem(new MenuItem("motion.katarina.Combo.user", "Use R").SetValue(true));
            _menu.AddSubMenu(comboMenu);

            //Harrass-Menü
            Menu harassMenu = new Menu("Harass", "motion.katarina.harrass");
            harassMenu.AddItem(new MenuItem("motion.katarina.harrass.useq", "Use Q").SetValue(true));
            harassMenu.AddItem(new MenuItem("motion.katarina.harrass.usew", "Use W").SetValue(true));
            harassMenu.AddItem(new MenuItem("motion.katarina.harrass.autoharrass", "Automatic Harrass").SetValue(true));
            harassMenu.AddItem(new MenuItem("motion.katarina.harrass.autoharrasskey","Toogle Harrass").SetValue(new KeyBind("N".ToCharArray()[0], KeyBindType.Toggle)));
            _menu.AddSubMenu(harassMenu);
            
            //Laneclear-Menü
            Menu laneclear = new Menu("Laneclear", "motion.katarina.laneclear");
            laneclear.AddItem(new MenuItem("motion.katarina.laneclear.useq", "Use Q").SetValue(true));
            laneclear.AddItem(new MenuItem("motion.katarina.laneclear.usew", "Use W").SetValue(true));
            laneclear.AddItem(new MenuItem("motion.katarina.laneclear.minw", "Minimum Minions to use W").SetValue(new Slider(3,1,6)));
            laneclear.AddItem(new MenuItem("motion.katarina.laneclear.minwlasthit", "Minimum Minions to Lasthit with W").SetValue(new Slider(2, 0, 6)));
            _menu.AddSubMenu(laneclear);

            //Jungleclear-Menü
            Menu jungleclear = new Menu("Jungleclear", "motion.katarina.jungleclear");
            jungleclear.AddItem(new MenuItem("motion.katarina.jungleclear.useq", "Use Q").SetValue(true));
            jungleclear.AddItem(new MenuItem("motion.katarina.jungleclear.usew", "Use W").SetValue(true));
            jungleclear.AddItem(new MenuItem("motion.katarina.jungleclear.usee", "Use E").SetValue(true));
            _menu.AddSubMenu(jungleclear);

            //Lasthit-Menü
            Menu lasthit = new Menu("Lasthit", "motion.katarina.lasthit");
            lasthit.AddItem(new MenuItem("motion.katarina.lasthit.useq", "Use Q").SetValue(true));
            lasthit.AddItem(new MenuItem("motion.katarina.lasthit.usew", "Use W").SetValue(true));
            _menu.AddSubMenu(lasthit);

            //KS-Menü
            Menu ksMenu = new Menu("Killsteal", "motion.katarina.killsteal");
            ksMenu.AddItem(new MenuItem("motion.katarina.killsteal.useq", "Use Q").SetValue(true));
            ksMenu.AddItem(new MenuItem("motion.katarina.killsteal.usew", "Use W").SetValue(true));
            ksMenu.AddItem(new MenuItem("motion.katarina.killsteal.usee", "Use E").SetValue(true));
            ksMenu.AddItem(new MenuItem("motion.katarina.killsteal.usef", "Use Ignite").SetValue(true));
            ksMenu.AddItem(new MenuItem("motion.katarina.killsteal.wardjump", "KS with Wardjump").SetValue(true));
            _menu.AddSubMenu(ksMenu);

            //Misc-Menü
            Menu miscMenu = new Menu("Miscellanious", "motion.katarina.misc");
            miscMenu.AddItem(new MenuItem("motion.katarina.misc.wardjump", "Use Wardjump").SetValue(true));
            miscMenu.AddItem(new MenuItem("motion.katarina.misc.wardjumpkey", "Wardjump Key").SetValue(new KeyBind("Z".ToCharArray()[0], KeyBindType.Press)));
            miscMenu.AddItem(new MenuItem("motion.katarina.misc.noRCancel", "Prevent R Cancel").SetValue(true).SetTooltip("This is preventing you from cancelling R accidentally within the first 0.4 seconds of cast"));
            miscMenu.AddItem(new MenuItem("motion.katarina.misc.kswhileult", "Do Killsteal while Ulting").SetValue(true));
            _menu.AddSubMenu(miscMenu);

            //alles zum Hauptmenü hinzufügen
            _menu.AddToMainMenu();

            #endregion
            Game.PrintChat("<font color='#bb0000'>e</font>.<font color='#0000cc'>Motion</font> Katarina loaded");

            #region Subscriptions
            Game.OnUpdate += Game_OnUpdate;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            Obj_AI_Base.OnIssueOrder += Obj_AI_Base_OnIssueOrder;

            #endregion
        }



        static void Game_OnUpdate(EventArgs args) {
            Demark();
            if (Player.IsDead || Player.IsRecalling())
            {
                return;
            }
            if (HasRBuff())
            {
                _orbwalker.SetAttack(false);
                _orbwalker.SetMovement(false);
                if(_menu.Item("motion.katarina.misc.kswhileult").GetValue<bool>())
                    Killsteal();
                return;
            }
            _orbwalker.SetAttack(true);
            _orbwalker.SetMovement(true);
            Killsteal();
            Combo();
            Lasthit();
            Harass();
            LaneClear();
            JungleClear();
            if (_menu.Item("motion.katarina.misc.wardjumpkey").GetValue<KeyBind>().Active && _menu.Item("motion.katarina.misc.wardjump").GetValue<bool>())
            {
                WardJump(Game.CursorPos);
            }
        }
        



        static bool HasRBuff()
        {
            return Player.HasBuff("KatarinaR") || Player.IsChannelingImportantSpell() || Player.HasBuff("katarinarsound");
        }



        static void Main(string[] args) {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }
        


        static void Combo()
        {
            if (_orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Combo)
                return;
            Obj_AI_Hero target = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);
            if(target != null && !target.IsZombie)
            {
                if(_menu.Item("motion.katarina.Combo.useq").GetValue<bool>() && Q.IsReady() && target.IsValidTarget(Q.Range))
                {
                    Q.Cast(target);
                    qTarget = target;
                }
                if (_menu.Item("motion.katarina.Combo.usew").GetValue<bool>() && W.IsReady() && target.IsValidTarget(W.Range - 10) && (target != qTarget || (R.IsReady() && _menu.Item("motion.katarina.Combo.user").GetValue<bool>())))
                {
                    W.Cast(target);
                }
                if (_menu.Item("motion.katarina.Combo.user").GetValue<bool>() && R.IsReady() && target.IsValidTarget(375))
                {
                    R.Cast();
                    _orbwalker.SetAttack(false);
                    _orbwalker.SetMovement(false);
                    whenToCancelR = Utils.GameTimeTickCount + 400;
                }
                if (_menu.Item("motion.katarina.Combo.usee").GetValue<bool>() 
                    && E.IsReady() 
                    && target.IsValidTarget(E.Range) 
                    && (!R.IsReady() || !_menu.Item("motion.katarina.Combo.user").GetValue<bool>() || !target.IsValidTarget(375)) 
                    && (W.IsReady() || R.IsReady() || target != qTarget))
                {
                    E.Cast(target);
                }
            }
        }


        public static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (args.SData.Name == "KatarinaQ" && args.Target.GetType() == typeof(Obj_AI_Hero))
            {
                qTarget = (Obj_AI_Hero) args.Target;
            }
            if (sender.IsMe && WardJumpReady)
            {
                E.Cast((Obj_AI_Base)args.Target);
                WardJumpReady = false;
            }
        }


        static void Demark()
        {
            if ((qTarget!=null && qTarget.HasBuff("katarinaqmark")) || Q.Cooldown < 3)
            {
                qTarget = null;
            }
        }


        #region WardJumping
        static void WardJump(Vector3 where,bool move = true)
        {
            if(move)
                Player.IssueOrder(GameObjectOrder.MoveTo, where);
            if (!E.IsReady())
            {
                return;
            }
            Vector3 wardJumpPosition = Player.Position.Distance(where) < 600 ? where : Player.Position.Extend(where, 600);
            var lstGameObjects = ObjectManager.Get<Obj_AI_Base>().ToArray();
            Obj_AI_Base entityToWardJump = lstGameObjects.FirstOrDefault(obj =>
                obj.Position.Distance(wardJumpPosition) < 150
                && (obj is Obj_AI_Minion || obj is Obj_AI_Hero)
                && !obj.IsMe && !obj.IsDead
                && obj.Position.Distance(Player.Position) < E.Range);

            if (entityToWardJump != null)
            {
                E.Cast(entityToWardJump);
            }
            else
            {
                int wardId = GetWardItem();


                if (wardId != -1 && !wardJumpPosition.IsWall())
                {
                    WardJumpReady = true;
                    PutWard(wardJumpPosition.To2D(), (ItemId)wardId);
                }
            }

        }

        public static int GetWardItem()
        {
            int[] wardItems = { 3340, 3350, 3205, 3207, 2049, 2045, 2044, 3361, 3154, 3362, 3160, 2043 };
            foreach (var id in wardItems.Where(id => Items.HasItem(id) && Items.CanUseItem(id)))
                return id;
            return -1;
        }

        public static void PutWard(Vector2 pos, ItemId warditem)
        {

            foreach (var slot in Player.InventoryItems.Where(slot => slot.Id == warditem))
            {
                ObjectManager.Player.Spellbook.CastSpell(slot.SpellSlot, pos.To3D());
                return;
            }
        }
        #endregion
        //Calculating Damage
        static float CalculateDamage(Obj_AI_Hero target)
        {
            double damage = 0d;
            if (Q.IsReady())
            {
                damage += ObjectManager.Player.GetSpellDamage(target, SpellSlot.Q) + ObjectManager.Player.GetSpellDamage(target, SpellSlot.Q, 1);
            }
            if (target.HasBuff("katarinaqmark"))
            {
                damage += ObjectManager.Player.GetSpellDamage(target, SpellSlot.Q, 1);
            }
            if (W.IsReady())
            {
                damage += ObjectManager.Player.GetSpellDamage(target, SpellSlot.W);
            }
            if (E.IsReady())
            {
                damage += ObjectManager.Player.GetSpellDamage(target, SpellSlot.E);
            }
            if (R.IsReady() || (ObjectManager.Player.GetSpell(R.Slot).State == SpellState.Surpressed && R.Level > 0))
            {
                damage += ObjectManager.Player.GetSpellDamage(target, SpellSlot.R);
            }
            if (Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite) > 0)
            {
                damage += Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite);
                damage -= target.HPRegenRate*2.5;
            }
            return (float)damage;
        }

        #region Killsteal
        static int CanKill(Obj_AI_Hero target, bool useq, bool usew, bool usee, bool usef)
        {
            double damage = 0;
            if (!useq && !usew && !usee &&!usef)
                return 0;
            if (Q.IsReady() && useq)
            {
                damage += ObjectManager.Player.GetSpellDamage(target, SpellSlot.Q);
                if ((W.IsReady() && usew) || (E.IsReady() && usee))
                {
                    damage += ObjectManager.Player.GetSpellDamage(target, SpellSlot.Q, 1);
                }

            }
            if (target.HasBuff("katarinaqmark"))
            {
                damage += ObjectManager.Player.GetSpellDamage(target, SpellSlot.Q, 1);
            }
            if (W.IsReady() && usew)
            {
                damage += ObjectManager.Player.GetSpellDamage(target, SpellSlot.W);
            }
            if (E.IsReady() && usee)
            {
                damage += ObjectManager.Player.GetSpellDamage(target, SpellSlot.E);
            }
            if (damage >= target.Health)
            {
                return 1;
            }
            if (Player.GetSummonerSpellDamage(target,Damage.SummonerSpell.Ignite) > 0 && !target.HasBuff("summonerdot") && !HasRBuff())
            {
                damage += Player.GetSummonerSpellDamage(target,Damage.SummonerSpell.Ignite);
                damage -= target.HPRegenRate*2.5;
            }
            return damage >= target.Health? 2 : 0;

        }

        private static void Killsteal()
        {
            foreach (Obj_AI_Hero enemy in AllEnemy)
            {
                if (enemy == null)
                    return;
                if (CanKill(enemy, false, _menu.Item("motion.katarina.killsteal.usew").GetValue<bool>(), false, false)==1 && enemy.IsValidTarget(390))
                {
                    W.Cast(enemy);
                    return;
                }
                if (CanKill(enemy, false, false, _menu.Item("motion.katarina.killsteal.usee").GetValue<bool>(), false)==1 && enemy.IsValidTarget(700))
                {
                    E.Cast(enemy);
                    return;
                }
                if (CanKill(enemy, _menu.Item("motion.katarina.killsteal.useq").GetValue<bool>(), false, false, false)==1 && enemy.IsValidTarget(675))
                {
                    Q.Cast(enemy);
                    qTarget = enemy;
                    return;
                }
                int cankill = CanKill(enemy, _menu.Item("motion.katarina.killsteal.useq").GetValue<bool>(),_menu.Item("motion.katarina.killsteal.usew").GetValue<bool>(),_menu.Item("motion.katarina.killsteal.usee").GetValue<bool>(),_menu.Item("motion.katarina.killsteal.usef").GetValue<bool>());
                if (( cankill==1 || cankill == 2) && enemy.IsValidTarget(Q.Range))
                {
                    if (cankill == 2 && enemy.IsValidTarget(600))
                        Player.Spellbook.CastSpell(IgniteSpellSlot,enemy);
                    if (Q.IsReady())
                        Q.Cast(enemy);
                    if (E.IsReady() && (W.IsReady() || qTarget != enemy))
                        E.Cast(enemy);
                    if (W.IsReady() && enemy.IsValidTarget(390) && qTarget != enemy)
                        W.Cast();
                    return;
                }
                //KS with Wardjump
                cankill = CanKill(enemy, true, false, false,_menu.Item("motion.katarina.killsteal.usef").GetValue<bool>());
                if (_menu.Item("motion.katarina.killsteal.wardjump").GetValue<bool>() && (cankill ==1 || cankill ==2) && enemy.IsValidTarget(1300) && Q.IsReady() && E.IsReady())
                {
                    WardJump(enemy.Position, false);
                    if (cankill == 2 && enemy.IsValidTarget(600))
                        Player.Spellbook.CastSpell(IgniteSpellSlot, enemy);
                    if (enemy.IsValidTarget(675))
                        Q.Cast(enemy);
                    return;
                }
            }
        }
        #endregion

        #region Harrass

        private static void Harass()
        {
            Obj_AI_Hero target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            if (target != null && (_orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed || (_menu.Item("motion.katarina.harrass.autoharrass").GetValue<bool>() && _menu.Item("motion.katarina.harrass.autoharrasskey").GetValue<KeyBind>().Active)) && target != qTarget)
            {
                if (Q.IsReady())
                    Q.Cast(target);
                if (W.IsReady() && null != TargetSelector.GetTarget(W.Range - 10, TargetSelector.DamageType.Magical))
                    W.Cast();
            }
        }

        #endregion

        #region Lasthit

        private static void Lasthit()
        {
            if (_orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.LastHit)
                return;
            Obj_AI_Base[] sourroundingMinions;
            if (_menu.Item("motion.katarina.lasthit.usew").GetValue<bool>() && W.IsReady())
            {
                sourroundingMinions = MinionManager.GetMinions(Player.Position, W.Range - 5).ToArray();
                //Only Cast W when minion is not killable with Autoattacks
                if (sourroundingMinions.Any(minion => !minion.IsDead && HealthPrediction.GetHealthPrediction(minion, (Player.CanAttack ? Utils.GameTimeTickCount + 25 + Game.Ping/2 : Orbwalking.LastAATick + (int) Player.AttackDelay*1000) + (int) Player.AttackCastDelay*1000) <= 0 && _orbwalker.GetTarget() != minion && W.GetDamage(minion) > minion.Health))
                {
                    W.Cast();
                }
            }
            if (_menu.Item("motion.katarina.lasthit.useq").GetValue<bool>() && Q.IsReady())
            {
                sourroundingMinions = MinionManager.GetMinions(Player.Position, Q.Range).ToArray();
                foreach (var minion in sourroundingMinions.Where(minion => !minion.IsDead && Q.GetDamage(minion) > minion.Health))
                {
                    Q.Cast(minion);
                    break;
                }
            }
        }
        #endregion

        #region LaneClear
        private static void LaneClear()
        {
            if (_orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.LaneClear)
                return;
            Obj_AI_Base[] sourroundingMinions;
            if (_menu.Item("motion.katarina.laneclear.usew").GetValue<bool>() && W.IsReady())
            {
                sourroundingMinions = MinionManager.GetMinions(Player.Position, W.Range - 5).ToArray();
                if (sourroundingMinions.GetLength(0) >= _menu.Item("motion.katarina.laneclear.minw").GetValue<Slider>().Value)
                {
                    int lasthittable = sourroundingMinions.Count(minion => W.GetDamage(minion) + (minion.HasBuff("katarinaqmark")? Q.GetDamage(minion,1) : 0) > minion.Health);
                    if (lasthittable >= _menu.Item("motion.katarina.laneclear.minwlasthit").GetValue<Slider>().Value)
                    {
                        W.Cast();
                    }
                }
            }
            if (_menu.Item("motion.katarina.laneclear.useq").GetValue<bool>() && Q.IsReady())
            {
                sourroundingMinions = MinionManager.GetMinions(Player.Position, Q.Range - 5).ToArray();
                foreach (var minion in sourroundingMinions.Where(minion => !minion.IsDead))
                {
                    Q.Cast(minion);
                    break;
                }
            }
        }
        #endregion

        #region Jungleclear

        private static void JungleClear()
        {
            Obj_AI_Base[] sourroundingMinions;
            if (_orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.LaneClear)
                return;
            if (_menu.Item("motion.katarina.jungleclear.useq").GetValue<bool>() && Q.IsReady())
            {
                sourroundingMinions = MinionManager.GetMinions(Player.Position, Q.Range, MinionTypes.All, MinionTeam.Neutral).ToArray();
                float maxhealth = 0;
                int chosenminion = 0;
                if (sourroundingMinions.GetLength(0) >= 1)
                {
                    for(int i = 0;i < sourroundingMinions.Length; i++)
                    {
                        if (maxhealth < sourroundingMinions[i].MaxHealth)
                        {
                            maxhealth = sourroundingMinions[i].MaxHealth;
                            chosenminion = i;
                        }
                    }
                    Q.Cast(sourroundingMinions[chosenminion]);
                }
            }
            if (_menu.Item("motion.katarina.jungleclear.usew").GetValue<bool>() && W.IsReady())
            {
                sourroundingMinions = MinionManager.GetMinions(Player.Position, W.Range - 5, MinionTypes.All,MinionTeam.Neutral).ToArray();
                if (sourroundingMinions.GetLength(0) >= 1)
                {
                    W.Cast();
                }
            }
            if (_menu.Item("motion.katarina.jungleclear.usee").GetValue<bool>() && E.IsReady())
            {
                sourroundingMinions = MinionManager.GetMinions(Player.Position, E.Range, MinionTypes.All, MinionTeam.Neutral).ToArray();
                float maxhealth = 0;
                int chosenminion = 0;
                if (sourroundingMinions.GetLength(0) >= 1)
                {
                    for (int i = 0; i < sourroundingMinions.Length; i++)
                    {
                        if (maxhealth < sourroundingMinions[i].MaxHealth)
                        {
                            maxhealth = sourroundingMinions[i].MaxHealth;
                            chosenminion = i;
                        }
                    }
                    E.Cast(sourroundingMinions[chosenminion]);
                }
            }
        }
        #endregion
        private static void Obj_AI_Base_OnIssueOrder(Obj_AI_Base sender, GameObjectIssueOrderEventArgs args)
        {
            if (sender.IsMe && HasRBuff() && Utils.GameTimeTickCount <= whenToCancelR && _menu.Item("motion.katarina.misc.noRCancel").GetValue<bool>())
                args.Process = false;
        }

    }
}