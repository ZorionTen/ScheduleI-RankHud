using System;
using MelonLoader;
using UnityEngine;
using Il2CppScheduleOne.Levelling;
using Il2CppScheduleOne.GameTime;

[assembly: MelonInfo(typeof(RankHud.RankHudMod), "RankHud", "1.0.2", "OPENCODE")]
[assembly: MelonGame()]

namespace RankHud
{
    public class RankHudMod : MelonMod
    {
        private static readonly string[] RankNames = new string[]
        {
            "Street Rat", "Hoodlum", "Peddler", "Hustler", "Bagman", "Enforcer",
            "Shot Caller", "Block Boss", "Underlord", "Baron", "Kingpin"
        };

        private LevelManager lm;
        private Il2CppScheduleOne.GameTime.TimeManager tm;

        private GUIStyle timeStyle;
        private GUIStyle rankStyle;
        private bool stylesReady;
        private bool warned;

        // level convention detection: if the game ever reports tier 0, tiers are 0-based
        private bool zeroBased;
        private int tiersPerRank = 5;

        // change-triggered diagnostics (bounded)
        private int lastTier = int.MinValue, lastIdx = int.MinValue;
        private float lastXp = float.MinValue, lastNeed = float.MinValue;
        private int logCount;

        private string timeStr = "--:--";
        private string rankLine = "";

        public override void OnUpdate()
        {
            try
            {
                if (lm == null)
                    lm = UnityEngine.Object.FindObjectOfType<LevelManager>();
                if (tm == null)
                    tm = UnityEngine.Object.FindObjectOfType<Il2CppScheduleOne.GameTime.TimeManager>();
                if (lm == null || tm == null)
                    return;

                // --- in-game clock (normalized 0..1 = midnight..midnight) ---
                float n = tm.NormalizedTimeOfDay;
                int mins;
                if (n >= 0f && n <= 1.001f)
                    mins = (int)(n * 1440f);
                else if (n > 1.001f && n <= 1441f)
                    mins = (int)n;                      // already minutes
                else
                    mins = tm.CurrentTime;              // fallback: raw time value
                mins = ((mins % 1440) + 1440) % 1440;
                int h24 = mins / 60;
                int h12 = h24 % 12;
                if (h12 == 0) h12 = 12;
                string ampm = h24 < 12 ? " AM" : " PM";
                string dayStr = "";
                try { dayStr = tm.CurrentDay.ToString() + " "; } catch { }
                timeStr = dayStr + h12 + ":" + string.Format("{0:00}", mins % 60) + ampm;

                // --- "Hustler 1, 50% to Hustler 2" ---
                FullRank cur = lm.GetFullRank();
                int ri = (int)cur.Rank;
                if (ri < 0) ri = 0;
                if (ri > 10) ri = 10;

                try
                {
                    int t = LevelManager.TIERS_PER_RANK;
                    if (t > 0) tiersPerRank = t;
                }
                catch { /* keep default */ }

                int tier = cur.Tier;
                if (tier < 0) tier = 0;
                int level;
                if (tier == 0)
                {
                    zeroBased = true;      // proof: tiers count 0..T-1
                    level = 1;
                }
                else if (zeroBased)
                {
                    level = tier + 1;
                }
                else
                {
                    level = tier;          // assume 1-based until proven otherwise
                }
                if (level < 1) level = 1;
                if (level > tiersPerRank) level = tiersPerRank;

                // progress to next level: XP within tier / XP needed for next tier
                float xp = lm.XP;
                float need = lm.XPToNextTier;
                float p = 0f;
                if (need > 0f)
                {
                    p = xp / need;
                    if (p < 0f) p = 0f;
                    if (p > 1f) p = 1f;
                }

                string curLabel = RankNames[ri] + " " + level;
                string nextLabel;
                bool maxed = (ri >= 10 && level >= tiersPerRank);
                if (maxed)
                {
                    rankLine = curLabel + ", MAX";
                }
                else
                {
                    if (level < tiersPerRank)
                        nextLabel = RankNames[ri] + " " + (level + 1);
                    else if (ri < 10)
                        nextLabel = RankNames[ri + 1] + " 1";
                    else
                        nextLabel = "MAX";
                    int pctInt = (int)(p * 100f + 0.5f);
                    rankLine = curLabel + ", " + pctInt + "% to " + nextLabel;
                }

                // change-triggered diagnostics: settles level-convention & XP questions
                if (logCount < 20 && (tier != lastTier || xp != lastXp || need != lastNeed))
                {
                    lastTier = tier; lastXp = xp; lastNeed = need;
                    logCount++;
                    int idx = -1;
                    try { idx = cur.GetRankIndex(); } catch { }
                    string gs = "?";
                    try { gs = cur.ToString(); } catch { gs = "err"; }
                    MelonLogger.Msg("RankHud: tier=" + tier + " T=" + tiersPerRank
                        + " idx=" + idx + " xp=" + xp + " need=" + need
                        + " totalXP=" + lm.TotalXP + " lvl=" + level
                        + " gameStr=[" + gs + "] line=[" + rankLine + "]");
                }
            }
            catch (Exception e)
            {
                if (!warned)
                {
                    MelonLogger.Error("RankHud update failed: " + e.Message);
                    warned = true;
                }
            }
        }

        public override void OnGUI()
        {
            if (lm == null || tm == null)
                return;
            try
            {
                if (!stylesReady)
                {
                    timeStyle = new GUIStyle(GUI.skin.label);
                    timeStyle.fontSize = 18;
                    timeStyle.fontStyle = FontStyle.Bold;
                    timeStyle.alignment = TextAnchor.MiddleRight;
                    timeStyle.normal.textColor = Color.white;

                    rankStyle = new GUIStyle(GUI.skin.label);
                    rankStyle.fontSize = 18;
                    rankStyle.fontStyle = FontStyle.Bold;
                    rankStyle.alignment = TextAnchor.MiddleRight;
                    rankStyle.normal.textColor = Color.white;

                    stylesReady = true;
                }

                float margin = 12f;
                float blockW = 430f;
                float x0 = Screen.width - margin - blockW;

                // clock, top-right
                GUI.Label(new Rect(x0, 8f, blockW, 26f), timeStr, timeStyle);

                // rank line directly below the time, right-aligned (clears the compass)
                GUI.Label(new Rect(x0, 36f, blockW, 22f), rankLine, rankStyle);
            }
            catch (Exception e)
            {
                if (!warned)
                {
                    MelonLogger.Error("RankHud GUI failed: " + e.Message);
                    warned = true;
                }
            }
        }
    }
}
