using UnityEngine;
using UnityEditor;

namespace DefineSymbolEditor
{
    /// <summary>
    /// プラットフォーム毎の設定
    /// </summary>
    class DefineSymbolPlatformData
    {
        public readonly BuildTargetGroup target;
        public string name { get { return target.ToString(); } }
        public readonly Texture icon;
        
        public DefineSymbolStatus status;


        //------------------------------------------------------
        // lifecycle
        //------------------------------------------------------

        public DefineSymbolPlatformData(BuildTargetGroup target)
        {
            this.target = target;
            icon = LoadIcon(target);
        }

        static Texture LoadIcon(BuildTargetGroup target)
        {
            var textureName = target.ToString();

            // iOSは古いiPhoneの名前で存在してるらしい
            if (target == BuildTargetGroup.iOS)
                textureName = "iPhone";

            var icon = EditorGUIUtility.Load(string.Format("BuildSettings.{0}", textureName)) as Texture;
            if (icon == null)
                icon = EditorGUIUtility.Load(string.Format("d_BuildSettings.{0}", textureName)) as Texture;
            return icon;
        }
    }
}