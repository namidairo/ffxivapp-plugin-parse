﻿// FFXIVAPP.Plugin.Parse ~ ParseHelper.cs
// 
// Copyright © 2007 - 2016 Ryan Wilson - All Rights Reserved
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FFXIVAPP.Plugin.Parse.Enums;
using FFXIVAPP.Plugin.Parse.Models;

namespace FFXIVAPP.Plugin.Parse.Helpers
{
    public static class ParseHelper
    {
        // setup pet info that comes through as "YOU"
        private static List<string> _pets = new List<string>
        {
            "eos",
            "selene",
            "topaz carbuncle",
            "emerald carbuncle",
            "ifrit-egi",
            "titan-egi",
            "garuda-egi",
            "amber carbuncle",
            "carbuncle topaze",
            "carbuncle émeraude",
            "carbuncle ambre",
            "topas-karfunkel",
            "smaragd-karfunkel",
            "bernstein-karfunkel",
            "フェアリー・エオス",
            "フェアリー・セレネ",
            "カーバンクル・トパーズ",
            "カーバンクル・エメラルド",
            "イフリート・エギ",
            "タイタン・エギ",
            "ガルーダ・エギ",
            "カーバンクル・アンバー"
        };

        private static List<string> _healingActions;

        public static List<string> HealingActions
        {
            get
            {
                if (_healingActions == null)
                {
                    _healingActions = new List<string>
                    {
                        "内丹",
                        "Second Wind",
                        "Second souffle",
                        "Chakra",
                        "ケアル",
                        "Cure",
                        "Soin",
                        "Vita",
                        "メディカ",
                        "Medica",
                        "Médica",
                        "Reseda",
                        "ケアルガ",
                        "Cure III",
                        "Méga Soin",
                        "Vitaga",
                        "メディカラ",
                        "Medica II",
                        "Extra Médica",
                        "Resedra",
                        "ケアルラ",
                        "Cure II",
                        "Extra Soin",
                        "Vitra",
                        "鼓舞激励の策",
                        "Adloquium",
                        "Traité du réconfort",
                        "Adloquium",
                        "士気高揚の策",
                        "Succor",
                        "Traité du soulagement",
                        "Kurieren",
                        "フィジク",
                        "Physick",
                        "Médecine",
                        "Physick",
                        "光の癒し",
                        "Embrace",
                        "Embrassement",
                        "Sanfte Umarmung",
                        "光の囁き",
                        "Whispering Dawn",
                        "Murmure de l'aurore",
                        "Erhebendes Flüstern",
                        "光の癒し",
                        "Embrace",
                        "Embrassement",
                        "Sanfte Umarmung",
                        "生命活性法",
                        "Lustrate",
                        "Loi de revivification",
                        "Revitalisierung",
                        "チョコメディカ",
                        "Choco Medica",
                        "Choco-médica",
                        "Chocobo-Reseda",
                        "チョコケアル",
                        "Choco Cure",
                        "Choco-soin",
                        "Chocobo-Vita"
                    };
                }
                return _healingActions;
            }
            set { _healingActions = value; }
        }

        /// <summary>
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="modifier"></param>
        /// <returns></returns>
        public static double GetBonusAmount(double amount, double modifier)
        {
            return Math.Abs((amount / (modifier + 1)) - amount);
        }

        /// <summary>
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="modifier"></param>
        /// <returns></returns>
        public static double GetOriginalAmount(double amount, double modifier)
        {
            return Math.Abs(amount - GetBonusAmount(amount, modifier));
        }

        /// <summary>
        /// </summary>
        /// <param name="name"></param>
        /// <param name="exp"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetTaggedName(string name, Expressions exp, TimelineType type)
        {
            if (type == TimelineType.Unknown)
            {
                return name;
            }
            var tag = "???";
            name = name.Trim();
            if (String.IsNullOrWhiteSpace(name))
            {
                return "";
            }
            name = Regex.Replace(name, @"\[[\w]+\]", "")
                        .Trim();
            var petFound = false;
            foreach (var pet in _pets.Where(pet => String.Equals(pet, name, Constants.InvariantComparer)))
            {
                petFound = true;
            }
            switch (type)
            {
                case TimelineType.You:
                    tag = exp.YouString;
                    break;
                case TimelineType.Party:
                    tag = "P";
                    break;
                case TimelineType.Alliance:
                    tag = "A";
                    break;
                case TimelineType.Other:
                    tag = "O";
                    break;
            }
            return String.Format("[{0}] {1}", tag, name);
        }

        #region LastAction Helper Dictionaries

        public static class LastAmountByAction
        {
            private static Dictionary<string, List<Tuple<string, double>>> Monster = new Dictionary<string, List<Tuple<string, double>>>();
            private static Dictionary<string, List<Tuple<string, double>>> Player = new Dictionary<string, List<Tuple<string, double>>>();

            public static Dictionary<string, double> GetMonster(string name)
            {
                var results = new Dictionary<string, double>();
                EnsureMonster(name);
                lock (Monster)
                {
                    var actionList = new List<string>();
                    var actionUpdateCount = new Dictionary<string, int>();
                    foreach (var actionTuple in Monster[name])
                    {
                        if (!results.ContainsKey(actionTuple.Item1))
                        {
                            results.Add(actionTuple.Item1, 0);
                        }
                        //results[actionTuple.Item1] += actionTuple.Item2;
                        results[actionTuple.Item1] = actionTuple.Item2;
                        if (!actionList.Contains(actionTuple.Item1))
                        {
                            actionList.Add(actionTuple.Item1);
                        }
                        if (!actionUpdateCount.ContainsKey(actionTuple.Item1))
                        {
                            actionUpdateCount.Add(actionTuple.Item1, 0);
                        }
                        actionUpdateCount[actionTuple.Item1]++;
                    }
                    foreach (var action in actionList)
                    {
                        //results[action] = results[action] / actionUpdateCount[action];
                    }
                }
                return results;
            }

            private static void EnsureMonster(string name)
            {
                lock (Monster)
                {
                    if (!Monster.ContainsKey(name))
                    {
                        Monster.Add(name, new List<Tuple<string, double>>());
                    }
                }
            }

            public static void EnsureMonsterAction(string name, string action, double amount)
            {
                EnsureMonster(name);
                lock (Monster)
                {
                    Monster[name].Add(new Tuple<string, double>(action, amount));
                }
            }

            public static Dictionary<string, double> GetPlayer(string name)
            {
                var results = new Dictionary<string, double>();
                EnsurePlayer(name);
                lock (Player)
                {
                    var actionList = new List<string>();
                    var actionUpdateCount = new Dictionary<string, int>();
                    foreach (var actionTuple in Player[name])
                    {
                        if (!results.ContainsKey(actionTuple.Item1))
                        {
                            results.Add(actionTuple.Item1, 0);
                        }
                        //results[actionTuple.Item1] += actionTuple.Item2;
                        results[actionTuple.Item1] = actionTuple.Item2;
                        if (!actionList.Contains(actionTuple.Item1))
                        {
                            actionList.Add(actionTuple.Item1);
                        }
                        if (!actionUpdateCount.ContainsKey(actionTuple.Item1))
                        {
                            actionUpdateCount.Add(actionTuple.Item1, 0);
                        }
                        actionUpdateCount[actionTuple.Item1]++;
                    }
                    foreach (var action in actionList)
                    {
                        //results[action] = results[action] / actionUpdateCount[action];
                    }
                }
                return results;
            }

            private static void EnsurePlayer(string name)
            {
                lock (Player)
                {
                    if (!Player.ContainsKey(name))
                    {
                        Player.Add(name, new List<Tuple<string, double>>());
                    }
                }
            }

            public static void EnsurePlayerAction(string name, string action, double amount)
            {
                EnsurePlayer(name);
                lock (Player)
                {
                    Player[name].Add(new Tuple<string, double>(action, amount));
                }
            }
        }

        #endregion
    }
}
