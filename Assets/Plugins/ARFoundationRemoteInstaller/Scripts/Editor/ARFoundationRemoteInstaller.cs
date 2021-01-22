using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.Assertions;
using Debug = UnityEngine.Debug;


namespace ARFoundationRemote.Editor {
    public class ARFoundationRemoteInstaller : ScriptableObject {
        [Tooltip("Use this field if your platform require additional extension when making a build.")]
        [SerializeField] public string optionalCompanionAppExtension = "";
        
        public const string pluginId = "com.kyrylokuzyk.arfoundationremote";
        public const string displayName = "AR Foundation Editor Remote";
        const string packagesFolderName = "Packages";
        static readonly Dictionary<string, string> minDependencies = new Dictionary<string, string> {
            {"com.unity.xr.arfoundation", "3.0.1"},
            {"com.unity.xr.arsubsystems", "3.0.0"},
            {"com.unity.xr.arcore", "3.0.1"},
            {"com.unity.xr.arkit", "3.0.1"},
            {"com.unity.xr.arkit-face-tracking", "3.0.1"},
        };

        static DirectoryInfo dataPathParent => Directory.GetParent(Application.dataPath);
        static char slash => Path.DirectorySeparatorChar;
        static string sourceFolderName => $"{dataPathParent}{slash}Assets{slash}Plugins{slash}ARFoundationRemoteInstaller{slash}{pluginId}";
        static string destinationFolderName => $"{dataPathParent}{slash}{packagesFolderName}{slash}{pluginId}";


        public static void UnInstallPlugin() {
            #if AR_FOUNDATION_REMOTE_INSTALLED
                FixesForEditorSupport.Undo();
            #endif
            
            var listRequest = Client.List(true, false);
            runRequest(listRequest, () => {
                Assert.AreEqual(StatusCode.Success, listRequest.Status);
                var plugin = listRequest.Result.SingleOrDefault(_ => _.name == pluginId);
                Assert.IsNotNull(plugin);
                if (plugin.source == PackageSource.Embedded) {
                    moveFolder(destinationFolderName, sourceFolderName);
                    logUninstallSuccess();
                } else {
                    Debug.LogError("Removing plugin via Package Manager. This error should not be visible in production!");
                    var removeRequest = Client.Remove(pluginId);
                    runRequest(removeRequest, () => {
                        if (removeRequest.Status == StatusCode.Success) {
                            logUninstallSuccess();
                        } else {
                            Debug.LogError($"removeRequest failed {removeRequest.Error}");
                        }
                    });
                }    
            });
            
            void logUninstallSuccess() {
                Debug.Log($"{displayName} was uninstalled from Packages folder. To uninstall the plugin completely, please delete the ARFoundationRemoteInstaller folder.");
            }
        }

        static void checkDependencies(Action<bool> callback) {
            var listRequest = Client.List(true, true);
            runRequest(listRequest, () => {
                callback(checkVersions(listRequest.Result));
            });
        }

        static bool checkVersions(PackageCollection packages) {
            var result = true;
            foreach (var package in packages) {
                var packageName = package.name;
                var currentVersion = parseUnityPackageManagerVersion(package.version);
                if (minDependencies.TryGetValue(packageName, out string dependency)) {
                    //Debug.Log(packageName);
                    var minRequiredVersion = new Version(dependency);
                    if (currentVersion < minRequiredVersion) {
                        result = false;
                        Debug.LogError("Please update this package to the required version via Window -> Package Manager: " + packageName + ":" + minRequiredVersion);
                    }
                }
            }

            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS) {
                if (packages.All(_ => _.name != "com.unity.xr.arkit-face-tracking")) {
                    Debug.Log("To enable iOS face tracking, install ARKit Face Tracking 3.0.1 via Package Manager.");
                }
            }
            
            return result;
        }

        static Version parseUnityPackageManagerVersion(string version) {
            var versionNumbersStrings = version.Split('.', '-');
            const int numOfVersionComponents = 3;
            Assert.IsTrue(versionNumbersStrings.Length >= numOfVersionComponents);
            var numbers = new List<int>();
            for (int i = 0; i < numOfVersionComponents; i++) {
                var str = versionNumbersStrings[i];
                if (int.TryParse(str, out int num)) {
                    numbers.Add(num);
                } else {
                    throw new Exception("cant parse " + str + " in " + version);
                }
            }

            return new Version(numbers[0], numbers[1], numbers[2]);
        }

        static Action requestCompletedCallback;
        static Request currentRequest;

        public static void runRequest(Request request, Action callback) {
            if (currentRequest != null) {
                Debug.Log(currentRequest.GetType().Name + " is already running, skipping new " + request.GetType().Name);
                return;
            }
        
            Assert.IsNull(requestCompletedCallback);
            Assert.IsNull(currentRequest);
            currentRequest = request;
            requestCompletedCallback = callback;
            EditorApplication.update += editorUpdate;
        }

        static void editorUpdate() {
            Assert.IsNotNull(currentRequest);
            if (currentRequest.IsCompleted) {
                EditorApplication.update -= editorUpdate;
                currentRequest = null;
                var cachedCallback = requestCompletedCallback;
                requestCompletedCallback = null;
                cachedCallback();
            }
        }

        [Conditional("_")]
        static void log(string msg) {
            Debug.Log(msg);
        }
        
        public static void InstallPlugin(bool verbose) {
            checkDependencies(success => {
                if (success) {
                    if (!Directory.Exists(sourceFolderName)) {
                        if (verbose) {
                            Debug.LogError($"{displayName}: please re-import the plugin or buy the additional license if you're trying to install the plugin on different development machine.");
                        }

                        return;
                    }

                    if (Directory.Exists(destinationFolderName)) {
                        Directory.Delete(destinationFolderName, true);
                    }

                    moveFolder(sourceFolderName, destinationFolderName);
                    addGitIgnore();
                    Debug.Log(displayName + " installed successfully. Please read DOCUMENTATION located at Assets/Plugins/ARFoundationRemoteInstaller/DOCUMENTATION.txt");
                } else {
                    Debug.LogError(displayName + " installation failed. Please fix errors and press 'Installer-Install Plugin'");
                }
            });
        }

        static void moveFolder(string source, string dest) {
            Assert.IsTrue(Directory.Exists(source));
            Assert.IsFalse(Directory.Exists(dest));
            
            Directory.Move(source, dest);
            File.Delete($"{source}.meta");
            log("AssetDatabase.Refresh()");
            AssetDatabase.Refresh();
            log(source);
            log(dest);
        }

        static void addGitIgnore() {
            var path = $"{dataPathParent}{slash}.gitignore";
            if (File.Exists(path)) {
                var text = File.ReadAllText(path);
                string ignore = $"{packagesFolderName}/{pluginId}";
                if (!text.Contains(ignore)) {
                    text += $"\n{ignore}";
                    File.WriteAllText(path, text);
                }
            }
        }


        public static void OnImport() {
            log("OnImport");
            InstallPlugin(false);
        }
    }
}
