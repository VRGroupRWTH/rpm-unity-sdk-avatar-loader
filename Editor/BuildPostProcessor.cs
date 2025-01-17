﻿using ReadyPlayerMe.Core;
using ReadyPlayerMe.Core.Analytics;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace ReadyPlayerMe.AvatarLoader.Editor
{
    public static class BuildPostProcessor
    {
        [PostProcessBuild(1)]
        public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
        {
            AppData appData = ApplicationData.GetData();
            AnalyticsEditorLogger.EventLogger.LogBuildApplication(appData.BuildTarget, PlayerSettings.productName, !Debug.isDebugBuild);
        }
    }
}
