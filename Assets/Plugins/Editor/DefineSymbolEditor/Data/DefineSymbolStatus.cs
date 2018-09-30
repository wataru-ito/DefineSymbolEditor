using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;

namespace DefineSymbolEditor
{
    public class ToggleStatus
    {
        public readonly ToggleContext context;
        public bool enabled;

        //------------------------------------------------------
		// lifecycle
		//------------------------------------------------------
        
        public ToggleStatus(ToggleContext context, string[] symbols)
        {
            this.context = context;
            enabled = Array.IndexOf(symbols, context.name) >= 0;
        }

        public override string ToString()
        {
            return enabled ? context.name : string.Empty;
        }
    }

    public class DropdownStatus
    {
        public readonly DropdownContext context;
        public int index;

        //------------------------------------------------------
		// lifecycle
		//------------------------------------------------------
        
        public DropdownStatus(DropdownContext context, string[] symbols)
        {
            this.context = context;
            index = -1;

            var symbolIndex = Array.FindIndex(symbols, i => i.StartsWith(context.name));
            if (symbolIndex >= 0)
            {
                var itemName = symbols[symbolIndex].Substring(context.name.Length + 1);
                index = context.items.IndexOf(itemName);
            }
        }

        public override string ToString()
        {
            return index >= 0 ? string.Format("{0}_{1}", context.name, context.items[index]) : string.Empty;
        }
    }

    /// <summary>
    /// 編集用のデータ
    /// </summary>
    class DefineSymbolStatus
	{
		public readonly BuildTargetGroup target;
		public DefineSymbolStatus common;
		public List<ToggleStatus> toggles;
		public List<DropdownStatus> dropdowns;


		//------------------------------------------------------
		// factory
		//------------------------------------------------------

		public DefineSymbolStatus(BuildTargetGroup target, DefineSymbolStatus common, DefineSymbolContext context, string symbol)
		{
			this.target = target;
			this.common = common;

			var symbols = symbol.Split(';');
			toggles = context.toggles.ConvertAll(i => new ToggleStatus(i, symbols));
			dropdowns = context.dropdowns.ConvertAll(i => new DropdownStatus(i, symbols));
		}


		//------------------------------------------------------
		// accessor
		//------------------------------------------------------

		public string ToSymbols()
		{
			var sb = new StringBuilder();

			if (common != null)
			{
				CollectSymbol(sb, common.toggles);
				CollectSymbol(sb, common.dropdowns);
			}

			CollectSymbol(sb, toggles);
			CollectSymbol(sb, dropdowns);

			return sb.ToString();
		}

		static void CollectSymbol<T>(StringBuilder sb, List<T> stateList) 
			where T : class
		{
			foreach (var state in stateList)
			{
				var symbol = state.ToString();
				if (!string.IsNullOrEmpty(symbol))
				{
					if (sb.Length > 0) sb.Append(";");
					sb.Append(symbol);
				}
			}
		}
	}
}