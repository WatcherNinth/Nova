using UnityEditor;
using UnityEngine;
using System.IO;

namespace Nova.Editor
{
    public class LevelContextMenu
    {
        [MenuItem("Assets/Create/New Interrogation Level", false, 10)]
        public static void CreateNewLevel()
        {
            string folderPath = GetTargetFolder();
            if (string.IsNullOrEmpty(folderPath)) return;

            var inputWindow = ScriptableObject.CreateInstance<FolderNameInputWindow>();
            inputWindow.Initialize(folderPath, (newFolderName) =>
            {
                if (!string.IsNullOrEmpty(newFolderName))
                {
                    CreateLevelStructure(folderPath, newFolderName);
                }
            });
        }

        private static string GetTargetFolder()
        {
            // 获取选中的文件夹或文件所在目录
            if (Selection.assetGUIDs.Length == 0) return "Assets";
            string selectedPath = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]);
            return Directory.Exists(selectedPath) ? selectedPath : Path.GetDirectoryName(selectedPath);
        }

        private static void CreateLevelStructure(string parentPath, string folderName)
        {
            // 生成唯一路径并创建文件夹
            string newFolderPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(parentPath, folderName));
            AssetDatabase.CreateFolder(parentPath, Path.GetFileName(newFolderPath));

            // 创建默认资源
            string assetPath = Path.Combine(newFolderPath, "level.asset");
            Interrorgation_Level levelAsset = ScriptableObject.CreateInstance<Interrorgation_Level>();
            levelAsset.LevelID = folderName;
            levelAsset.LevelStartScript = $"{folderName}_start_script";
            levelAsset.LevelFinishScript = $"{folderName}_finish_script";


            AssetDatabase.CreateAsset(levelAsset, assetPath);
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = levelAsset;
        }
    }

    class FolderNameInputWindow : EditorWindow
    {
        private string folderName = "NewLevel";
        private System.Action<string> callback;
        private string basePath;

        public void Initialize(string targetPath, System.Action<string> callback)
        {
            basePath = targetPath;
            this.callback = callback;
            ShowModal();
        }

        void OnGUI()
        {
            GUILayout.Label("Create New Interrogation Level");
            folderName = EditorGUILayout.TextField("Level Name", folderName);

            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Create"))
                {
                    callback?.Invoke(folderName);
                    Close();
                }
                if (GUILayout.Button("Cancel"))
                {
                    Close();
                }
            }
        }
    }
}