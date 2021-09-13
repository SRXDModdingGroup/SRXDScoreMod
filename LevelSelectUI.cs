using System.Collections.Generic;
using System.Text.RegularExpressions;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace ScoreMod {
    // Contains patch functions to make the level select menu show modded scores
    public class LevelSelectUI {
        private static readonly Regex MATCH_BASE_ID = new Regex(@"(.+?)_Stats");
        private static readonly Regex MATCH_CUSTOM_ID = new Regex(@"CUSTOM_(.+?)_(\-?\d+)_Stats");
        private static readonly HashSet<string> FORBIDDEN_NAMES = new HashSet<string> {
            "CreateCustomTrack_Stats",
            "Tutorial XD",
            "RandomizeTrack_Stats"
        };
        
        private static bool menuLoaded;
        private static string realHighScore;
        private static string modHighScore;
        private static string realRank;
        private static string modRank;
        private static string selectedTrackStatString;
        private static string selectedTrackId;
        private static string lastTrackId;
        private static string lastStatString;
        private static TrackData.DifficultyType selectedTrackDifficulty;
        private static TrackData.DifficultyType lastDifficulty;
        private static TMP_Text scoreText;
        private static TMP_Text rankText;

        public static void UpdateUI() {
            if (string.IsNullOrWhiteSpace(selectedTrackId) || scoreText == null || rankText == null)
                return;

            if (ModState.ShowModdedScore) {
                scoreText.SetText(modHighScore);
                rankText.SetText(modRank);
            }
            else {
                scoreText.SetText(realHighScore);
                rankText.SetText(realRank);
            }
        }

        public static void UpdateModScore() {
            if (!string.IsNullOrWhiteSpace(selectedTrackId))
                modHighScore = $"<line-height=50%>{HighScoresContainer.GetHighScore(selectedTrackId, ModState.CurrentContainer.Profile.GetUniqueId(), out int superPerfectCount, out modRank)}\n<size=50%>+{superPerfectCount}";
        }

        public static string GetTrackId(PlayableTrackData trackData) => GetTrackId(trackData.TrackInfoRef.StatsUniqueString, trackData.Difficulty);

        private static string GetTrackId(string statString, TrackData.DifficultyType difficulty) {
            if (statString == lastStatString && difficulty == lastDifficulty)
                return lastTrackId;

            lastStatString = statString;
            lastDifficulty = difficulty;
            
            if (FORBIDDEN_NAMES.Contains(statString))
                return lastTrackId = string.Empty;
            
            var match = MATCH_CUSTOM_ID.Match(statString);
            
            if (match.Success) {
                var groups = match.Groups;
                uint fileHash;

                unchecked {
                    fileHash = (uint) int.Parse(groups[2].Value);
                }

                return lastTrackId = $"{groups[1].Value.Replace(' ', '_')}_{fileHash:x8}_{difficulty}";
            }

            match = MATCH_BASE_ID.Match(statString);

            if (match.Success)
                return lastTrackId = $"{match.Groups[1].Value.Replace(' ', '_')}_{difficulty}";

            return lastTrackId = string.Empty;
        }
        
        [HarmonyPatch(typeof(XDLevelSelectMenuBase), nameof(XDLevelSelectMenuBase.ShowSongDetails)), HarmonyPostfix]
        private static void XDLevelSelectMenuBase_ShowSongDetails_Postfix(XDLevelSelectMenuBase __instance) {
            scoreText = __instance.score[0];
            rankText = __instance.rank[0];
            
            var parent = scoreText.transform.parent;

            if (parent.localScale.x > 0.95f) {
                parent.localScale = 0.9f * Vector3.one;
                
                for (int i = 2; i < 7; i++) {
                    var child = parent.GetChild(i);

                    child.localPosition += 10f * Vector3.down;
                }
            }
            
            if (__instance.haveContentTracklistTracksLoaded)
                menuLoaded = true;

            if (!menuLoaded)
                return;

            string statString = __instance.WillLandAtHandle.TrackInfoRef.StatsUniqueString;

            if (statString == selectedTrackStatString)
                return;

            selectedTrackStatString = statString;
            selectedTrackId = GetTrackId(statString, selectedTrackDifficulty);
            UpdateModScore();
        }

        [HarmonyPatch(typeof(XDDifficultyIcon), nameof(XDDifficultyIcon.ShowCorrectIcon)), HarmonyPostfix]
        private static void XDDifficultyIcon_ShowCorrectIcon_Postfix(TrackData.DifficultyType difficultyType) {
            selectedTrackDifficulty = difficultyType;
            
            if (string.IsNullOrWhiteSpace(selectedTrackStatString))
                return;
            
            selectedTrackId = GetTrackId(selectedTrackStatString, selectedTrackDifficulty);
            UpdateModScore();
        }

        [HarmonyPatch(typeof(TrackInfoAssetReference.StatsForTrackInfo), nameof(TrackInfoAssetReference.StatsForTrackInfo.GetBestScoreForDifficulty)), HarmonyPostfix]
        private static void StatsForTrackInfo_GetBestScoreForDifficulty_Postfix(TrackInfoAssetReference.VersionedIntValueForDifficulty __result) {
            realHighScore = __result.valueForDifficulty.ToString();
        }

        [HarmonyPatch(typeof(TrackDataMetadata), nameof(TrackDataMetadata.GetRankCalculatedFromScore)), HarmonyPostfix]
        private static void TrackDataMetadata_GetRankCalculatedFromScore_Postfix(string __result) {
            realRank = __result;
        }
        
        [HarmonyPatch(typeof(TMP_Text), nameof(TMP_Text.text), MethodType.Setter), HarmonyPrefix]
        private static bool TMP_Text_SetTextInternal_Prefix(TMP_Text __instance, ref string value) {
            if (GameplayState.Playing || !ModState.ShowModdedScore)
                return true;

            if (__instance == scoreText)
                value = modHighScore;
            else if (__instance == rankText)
                value = modRank;

            return true;

        }
    }
}