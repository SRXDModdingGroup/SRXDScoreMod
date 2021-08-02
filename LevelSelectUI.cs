using System.Collections.Generic;
using System.Text.RegularExpressions;
using HarmonyLib;

namespace ScoreMod {
    public class LevelSelectUI {
        private static readonly Regex MATCH_BASE_ID = new Regex(@"(.+?)_Stats");
        private static readonly Regex MATCH_CUSTOM_ID = new Regex(@"CUSTOM_(.+?)_(\-?\d+)_Stats");
        private static readonly HashSet<string> FORBIDDEN_NAMES = new HashSet<string> {
            "CreateCustomTrack_Stats",
            "Tutorial XD",
            "RandomizeTrack_Stats"
        };
        
        private static bool menuLoaded;
        private static string selectedTrackStatString;
        private static string selectedTrackId;
        
        [HarmonyPatch(typeof(XDLevelSelectMenuBase), nameof(XDLevelSelectMenuBase.ShowSongDetails)), HarmonyPostfix]
        private static void XDLevelSelectMenuBase_ShowSongDetails_Postfix(XDLevelSelectMenuBase __instance) {
            var trackInfoRef = __instance.WillLandAtHandle.TrackInfoRef;
            
            if (__instance.haveContentTracklistTracksLoaded)
                menuLoaded = true;

            if (!menuLoaded)
                return;

            string statString = trackInfoRef.Stats.statsUniqueString;

            if (statString == selectedTrackStatString) {
                selectedTrackStatString = statString;
                
                return;
            }
            
            selectedTrackStatString = statString;
            selectedTrackId = string.Empty;
            
            if (FORBIDDEN_NAMES.Contains(statString))
                return;
            
            var match = MATCH_CUSTOM_ID.Match(statString);
            
            if (match.Success) {
                var groups = match.Groups;
                uint fileHash;

                unchecked {
                    fileHash = (uint) int.Parse(groups[2].Value);
                }

                selectedTrackId = $"{groups[1].Value.Replace(' ', '_')}_{fileHash:x8}";
            }
            else {
                match = MATCH_BASE_ID.Match(statString);

                if (match.Success)
                    selectedTrackId = match.Groups[1].Value.Replace(' ', '_');
            }
        }
    }
}