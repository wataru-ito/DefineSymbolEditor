using UnityEditor;

namespace DefineSymbolEditor
{
    /// <summary>
    /// 現在の設定とプリセットを共通で扱えるようにする
    /// </summary>
    interface IDefineSymbolData
    {
        string GetCommonSymbols();

        string GetScriptingDefineSymbolsForGroup(BuildTargetGroup targetGroup);
    }
}