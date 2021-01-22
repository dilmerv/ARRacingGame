#if AR_FOUNDATION_REMOTE_INSTALLED
    using ARFoundationRemote.Runtime;
#endif
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.PackageManager;
using UnityEngine;


namespace ARFoundationRemote.Editor {
    [UsedImplicitly]
    [InitializeOnLoad]
    public class CompanionAppInstaller: IPreprocessBuildWithReport, IPostprocessBuildWithReport {
        const string appName = "ARCompanion";
        const string arCompanionDefine = "AR_COMPANION";

        public int callbackOrder => 0;


        static CompanionAppInstaller() {
            // OnPreprocessBuild is not called after unsuccessful build
            restoreChanges();
        }

        void IPreprocessBuildWithReport.OnPreprocessBuild(BuildReport report) {
            if (isBuildingCompanionApp(report)) {
                applicationIdentifier = removeAppName(applicationIdentifier) + appName;
                PlayerSettings.productName = appName + removeAppName(PlayerSettings.productName);
                toggleDefine(arCompanionDefine, true);
            } else {
                if (EditorBuildSettings.scenes.Any(_ => _.path.Contains("ARCompanion.unity"))) {
                    throw new Exception("AR Foundation Editor Remote: please build the companion app via Installer object by pressing 'Install AR Companion App' or 'Build AR Companion and show in folder' button.");
                }
            }
        }

        static string applicationIdentifier {
            get => PlayerSettings.GetApplicationIdentifier(activeBuildTargetGroup);
            set => PlayerSettings.SetApplicationIdentifier(activeBuildTargetGroup, value);
        }
        
        public void OnPostprocessBuild(BuildReport report) {
            if (isBuildingCompanionApp(report)) {
                restoreChanges();

                if (report.summary.totalErrors == 0) {
                    Debug.Log(appName + " build succeeded.");
                }
            }
        }

        static void restoreChanges() {
            PlayerSettings.productName = removeAppName(PlayerSettings.productName);
            applicationIdentifier = removeAppName(applicationIdentifier);
            disableCompanionAppDefine();
        }

        static void disableCompanionAppDefine() {
            toggleDefine(arCompanionDefine, false);
        }

        static void toggleDefine(string define, bool enable) {
            var buildTargetGroup = activeBuildTargetGroup;
            if (enable) {
                if (isDefineSet(define)) {
                    return;
                }

                var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, $"{defines};{define}");
            } else {
                if (!isDefineSet(define)) {
                    return;
                }

                var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup).Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries);
                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, string.Join(";", defines.Where(d => d.Trim() != define).ToArray()));
            }
        }

        static BuildTargetGroup activeBuildTargetGroup => EditorUserBuildSettings.activeBuildTarget.ToBuildTargetGroup();

        static bool isDefineSet(string define) {
            return PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.activeBuildTarget.ToBuildTargetGroup()).Contains(define);
        }

        static bool isBuildingCompanionApp(BuildReport report) {
            var result = report.summary.outputPath.Contains(companionAppFolder);
            //Debug.Log("isBuildingCompanionApp " + result);
            return result;
        }

        static string removeAppName(string s) {
            return s.Replace(appName, "");
        }

        public static void Build(string optionalCompanionAppExtension) {
            build(buildOptions | BuildOptions.ShowBuiltPlayer, optionalCompanionAppExtension);
        }

        public static void BuildAndRun(string optionalCompanionAppExtension) {
            build(buildOptions | BuildOptions.AutoRunPlayer, optionalCompanionAppExtension);
        }

        static void build(BuildOptions options, string optionalCompanionAppExtension) {
            var listRequest = Client.List(true, true);
            ARFoundationRemoteInstaller.runRequest(listRequest, () => {
                if (listRequest.Status != StatusCode.Success) {
                    Debug.LogError("ARFoundationRemoteInstaller can't check installed packages.");
                    return;
                }

                if (!isPresent(ARFoundationRemoteInstaller.pluginId)) {
                    Debug.LogError("Please install " + ARFoundationRemoteInstaller.displayName);
                    return;
                }
                
                #if AR_FOUNDATION_REMOTE_INSTALLED
                    var instance = Settings.Instance;
                    instance.packages = PackageVersionData.Create(listRequest.Result);
                    EditorUtility.SetDirty(instance);

                    if (Defines.isAndroid) {
                        if (!isPresent("com.unity.xr.arcore")) {
                            logPluginNotInstalledError("ARCore XR Plugin");
                            return;
                        }
                    } else if (Defines.isIOS) {
                        if (!isPresent("com.unity.xr.arkit") && !isPresent("com.unity.xr.arkit-face-tracking")) {
                            logPluginNotInstalledError("ARKit XR Plugin");
                            return;
                        }
                    }

                    void logPluginNotInstalledError(string pluginName) {
                        Debug.LogError($"Please install '{pluginName}' via Package Manager and ENABLE IT in 'XR Plug-in Management' window.");
                    }
                #endif
                
                var scenes = getSenderScenePaths().Select(_ => _.ToString()).ToArray();
                BuildPipeline.BuildPlayer(scenes, getInstallDirectory() + EditorUserBuildSettings.activeBuildTarget + getExtension(optionalCompanionAppExtension), EditorUserBuildSettings.activeBuildTarget, options);
                
                bool isPresent(string packageId) {
                    return listRequest.Result.SingleOrDefault(_ => _.name == packageId) != null;
                }
            });
        }

        static string getExtension(string optionalCompanionAppExtension) {
            switch (EditorUserBuildSettings.activeBuildTarget) {
                case BuildTarget.iOS:
                    return "";
                case BuildTarget.Android:
                    return ".apk";
                default:
                    if (string.IsNullOrEmpty(optionalCompanionAppExtension)) {
                        Debug.LogWarning("Please specify optionalCompanionAppExtension if your target platform needs one. For example, Android target requires .apk extension for the builds.");
                    } else {
                        Debug.Log("Using optionalCompanionAppExtension: " + optionalCompanionAppExtension);
                    }
                    return optionalCompanionAppExtension;
            }
        }

        /// Adding BuildOptions.AcceptExternalModificationsToPlayer will produce gradle build for android instead of apk
        /// Enable BuildOptions.Development for assertions to work
        static BuildOptions buildOptions => BuildOptions.Development;

        static string getInstallDirectory() {
            return Directory.GetParent(Application.dataPath).FullName + "/" + companionAppFolder + "/";
        }

        static string companionAppFolder => "ARFoundationRemoteCompanionApp";

        static IEnumerable<FileInfo> getSenderScenePaths() {
            return new DirectoryInfo(Application.dataPath + "/Plugins/ARFoundationRemoteInstaller/Scenes/ARCompanion")
                .GetFiles("*.unity");
        }

        public static void DeleteCompanionAppBuildFolder() {
            var path = getInstallDirectory();
            if (Directory.Exists(path)) {
                Directory.Delete(path, true);
            }
        }
    }
}
