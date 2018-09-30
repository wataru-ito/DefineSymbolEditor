using System;
using UnityEngine;
using UnityEditor;

namespace DefineSymbolEditor
{
    class DefineSymbolPresetCreateWindow : EditorWindow
    {
        string m_name = "PRESET";
        Action<string> m_callback;


        //------------------------------------------------------
        // static function
        //------------------------------------------------------

        public static DefineSymbolPresetCreateWindow Open(string presetName, Action<string> callback)
        {
            var win = CreateInstance<DefineSymbolPresetCreateWindow>();
            if (!string.IsNullOrEmpty(presetName))
                win.m_name = presetName;
            win.m_callback = callback;
            win.ShowAuxWindow();
            return win;
        }


        //------------------------------------------------------
        // unity system function
        //------------------------------------------------------

        void OnEnable()
        {
            titleContent = new GUIContent("プリセット保存");
            minSize =
            maxSize = new Vector2(250, 40);
        }

        void OnGUI()
        {
            EditorGUIUtility.labelWidth = 70f;
            m_name = EditorGUILayout.TextField("プリセット名", m_name);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = !string.IsNullOrEmpty(m_name);
                if (GUILayout.Button("保存", "ButtonLeft"))
                {
                    m_callback(m_name);
                    Close();
                }
                GUI.enabled = true;

                if(GUILayout.Button("戻る", "ButtonRight"))
                {
                    Close();
                }
            }
        }
    }
}