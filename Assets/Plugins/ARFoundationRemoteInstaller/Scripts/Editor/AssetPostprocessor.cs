using System.Linq;


namespace ARFoundationRemote.Editor {
    public class AssetPostprocessor : UnityEditor.AssetPostprocessor {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
            if (importedAssets.Any(_ => _.Contains($"{nameof(AssetPostprocessor)}.cs"))) {
                ARFoundationRemoteInstaller.OnImport();
            }
        }
    }
}
