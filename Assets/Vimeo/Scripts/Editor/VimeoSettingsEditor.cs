using UnityEngine;
using UnityEditor;
using System;

namespace Vimeo
{
    [CustomEditor(typeof(VimeoSettings))]
    public class VimeoSettingsEditor : BaseEditor
    {
        public override void OnInspectorGUI()
        {
            var player = target as VimeoSettings;
            DrawVimeoConfig(player); 
            EditorUtility.SetDirty(target);
        }

        public void DrawVimeoConfig(VimeoSettings settings)
        {
            var so = serializedObject;

             // Help Nav            
            GUILayout.BeginHorizontal();
            var style = new GUIStyle();
            style.border = new RectOffset(0,0,0,0);
            GUILayout.Box("", style);

            GUIManageVideosButton();
            GUIHelpButton();
            GUISignOutButton();
            
            GUILayout.EndHorizontal();
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            if (settings.Authenticated() && settings.vimeoSignIn) {
                bool updated;
                updated = GUISelectFolderType();
                GUISelectFolder(updated);
                GUISelectVideo(); // don't fill videos unless explicitly requested via button
            }

            DrawVimeoAuth(settings);
            so.ApplyModifiedProperties();
        }
    }
}