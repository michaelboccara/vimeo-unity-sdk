#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using Vimeo;
using System.Linq;

namespace Vimeo.Recorder
{
    [CustomEditor(typeof(VimeoJustUploader))]
    public class VimeoJustUploaderEditor : BaseEditor
    {
        static bool publishFold;
        static bool vimeoFold;

        [MenuItem("GameObject/Video/Vimeo Uploader")]
        private static void CreateUploaderPrefab()
        {
            GameObject go = Instantiate(Resources.Load("Prefabs/[VimeoJustUploader]") as GameObject);
            go.name = "[VimeoJustUploader]";
        }

        void OnDisable()
        {
            EditorPrefs.SetBool("publishFold", publishFold);
            EditorPrefs.SetBool("vimeoFold", vimeoFold);
        }

        void OnEnable()
        {
            publishFold = EditorPrefs.GetBool("publishFold");
            vimeoFold = EditorPrefs.GetBool("vimeoFold");
        }

        public override void OnInspectorGUI()
        {
            var uploader = target as VimeoJustUploader;
            DrawConfig(uploader);
            EditorUtility.SetDirty(target);
        }

        public void DrawConfig(VimeoJustUploader uploader)
        {
            var so = serializedObject;

            // Help Nav            
            GUILayout.BeginHorizontal();
            var style = new GUIStyle();
            style.border = new RectOffset(0, 0, 0, 0);
            GUILayout.Box("", style);

            GUIManageVideosButton();
            GUIHelpButton();
            GUISignOutButton();

            GUILayout.EndHorizontal();
            EditorGUILayout.Space();

            // Vimeo Settings
            if (uploader.Authenticated() && uploader.vimeoSignIn) {

                publishFold = EditorGUILayout.Foldout(publishFold, "Upload to Vimeo");

                if (publishFold) {
                    EditorGUI.indentLevel++;

                    EditorGUILayout.PropertyField(so.FindProperty("videoName"));
                    EditorGUILayout.PropertyField(so.FindProperty("privacyMode"));

                    if (VimeoApi.PrivacyModeDisplay.OnlyPeopleWithAPassword == uploader.privacyMode) {
                        EditorGUILayout.PropertyField(so.FindProperty("videoPassword"), new GUIContent("Password"));
                    }

                    GUISelectFolder();

                    EditorGUILayout.PropertyField(so.FindProperty("commentMode"), new GUIContent("Comments"));
                    EditorGUILayout.PropertyField(so.FindProperty("enableDownloads"));
                    EditorGUILayout.PropertyField(so.FindProperty("enableReviewPage"));

                    EditorGUILayout.PropertyField(so.FindProperty("openInBrowser"));

                    EditorGUI.indentLevel--;
                }

                DrawUploadingControls();
            }

            DrawVimeoAuth(uploader);

            //DrawDefaultInspector();

            so.ApplyModifiedProperties();
        }

        public void DrawUploadingControls()
        {
            var so = serializedObject;
            var uploader = target as VimeoJustUploader;
            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical();

            EditorGUILayout.PropertyField(so.FindProperty("filePath"));

            EditorGUILayout.Space();

            if (EditorApplication.isPlaying)
            {
                EditorGUILayout.Space();

                if (!uploader.isUploading)
                {
                    if (GUILayout.Button("Start Uploading"))
                    {
                        uploader.StartUploading();
                    }

                    if (uploader.uploadStatus == "UploadComplete")
                    {
                        EditorGUILayout.PropertyField(so.FindProperty("videoPermalink"));
                    }
                }
                else
                {
                    EditorGUILayout.Space();

                    var rect = EditorGUILayout.BeginHorizontal();
                    rect.height = 20;
                    GUILayout.Box("", GUILayout.Height(20));
                    EditorGUI.ProgressBar(rect, uploader.uploadProgress, "Uploading to Vimeo...");
                    GUILayout.EndHorizontal();

                    EditorGUILayout.Space();

                    if (GUILayout.Button("Cancel Upload"))
                    {
                        uploader.CancelUpload();
                    }

                }
            }

            GUILayout.EndVertical();

        }

    }
}
#endif