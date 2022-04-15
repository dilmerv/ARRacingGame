// Copyright 2021 Niantic, Inc. All Rights Reserved.

using UnityEngine;

using UnityEditor;
using UnityEditor.Callbacks;

using System.IO;
using System.Collections;
#if UNITY_IOS && UNITY_EDITOR_OSX
using UnityEditor.iOS.Xcode;

public class PostBuildProcess: MonoBehaviour
{
  [PostProcessBuild]
  public static void OnPostProcessBuild(BuildTarget buildTarget, string path)
  {
    if (buildTarget == BuildTarget.iOS)
      BuildForiOS(path);
  }

  private static void BuildForiOS(string path)
  {
    string projectPath = path + "/Unity-iPhone.xcodeproj/project.pbxproj";

    PBXProject project = new PBXProject();
    var file = File.ReadAllText(projectPath);
    project.ReadFromString(file);

#if UNITY_2019_3_OR_NEWER
    string appTarget = project.GetUnityMainTargetGuid();
#else
    string appTarget = project.TargetGuidByName("Unity-iPhone");
#endif

    // TODO, have this be generated, we already do this with a lot of other
    // properties anyway
    project.SetBuildProperty(appTarget, "ENABLE_BITCODE", "NO");
    project.SetBuildProperty(project.ProjectGuid(), "ENABLE_BITCODE", "NO");

    project.AddFrameworkToProject(appTarget, "ARKit.framework", false);
    project.AddFrameworkToProject(appTarget, "Metal.framework", false);
    project.AddFrameworkToProject(appTarget, "Vision.framework", false);
    project.AddFrameworkToProject(appTarget, "CoreML.framework", false);
    project.AddFrameworkToProject(appTarget, "CoreImage.framework", false);

    // https://issuetracker.unity3d.com/issues/ios-unityframework-with-3rd-party-plugins-triggers-watchdog-termination-after-launch
#if UNITY_2019_4_OR_NEWER
      project.AddFrameworkToProject(project.GetUnityMainTargetGuid(), "UnityFramework.framework", false);
#endif

    // Not sure why unity likes to make a dependency with a framework called
    // 'null.framework' but this works around that nonsense

#if UNITY_2019_3_OR_NEWER
    var unityTarget = project.GetUnityFrameworkTargetGuid();
#else
    var unityTarget = appTarget;
#endif

    if (project.ContainsFramework(unityTarget, "null.framework"))
        project.RemoveFrameworkFromProject(unityTarget, "null.framework");

    File.WriteAllText(projectPath, project.WriteToString());

    // Get the plist
    string plistPath = path + "/Info.plist";
    PlistDocument plist = new PlistDocument();
    plist.ReadFromString(File.ReadAllText(plistPath));

    PlistElementDict rootDict = plist.root;

    // Set key and value for NSMotionUsageDescription.
    rootDict.SetString("NSMotionUsageDescription", "Required for Augmented Reality");

    // Write edited plist to file
    File.WriteAllText(plistPath, plist.WriteToString());
  }
}

#endif
