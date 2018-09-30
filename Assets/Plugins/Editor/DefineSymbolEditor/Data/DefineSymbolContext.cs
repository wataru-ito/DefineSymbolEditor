using System;
using System.Collections.Generic;
using UnityEngine;

namespace DefineSymbolEditor
{
    [Serializable]
    public class SymbolContext
    {
        public string name;
        public string description;
        public bool individual; // プラットフォーム別の設定か

        //------------------------------------------------------
        // lifecycle
        //------------------------------------------------------

        public SymbolContext()
        { }

        public SymbolContext(SymbolContext src)
        {
            name = src.name;
            description = src.description;
            individual = src.individual;
        }

        //------------------------------------------------------
		// accessor
		//------------------------------------------------------
        
        public GUIContent content
        {
            get { return new GUIContent(name, description); }
        }
    }

    [Serializable]
    public class ToggleContext : SymbolContext
    {
        //------------------------------------------------------
		// lifecycle
		//------------------------------------------------------
        
        public ToggleContext()
        { }

        public ToggleContext(ToggleContext src)
            : base(src)
        { }
    }

    [Serializable]
    public class DropdownContext : SymbolContext
    {
        public List<string> items;

        //------------------------------------------------------
		// lifecycle
		//------------------------------------------------------
        
        public DropdownContext()
        {
            items = new List<string>();
        }

        public DropdownContext(DropdownContext src)
            : base(src)
        {
            items = new List<string>(src.items);
        }


        //------------------------------------------------------
		// accessor
		//------------------------------------------------------
        
        public GUIContent[] displayedOptions
        {
            get
            {
                var array = new GUIContent[items.Count];
                for (int i = 0; i < items.Count; ++i)
                {
                    array[i] = new GUIContent(items[i]);
                }
                return array;
            }
        }
    }


    /// <summary>
    /// シンボルの設定項目情報
    /// </summary>
    [Serializable]
	class DefineSymbolContext
	{
		public List<ToggleContext> toggles;
		public List<DropdownContext> dropdowns;


		//------------------------------------------------------
		// lifecycle
		//------------------------------------------------------

		public DefineSymbolContext()
		{
			toggles = new List<ToggleContext>();
			dropdowns = new List<DropdownContext>();
		}

		public DefineSymbolContext(DefineSymbolContext src)
		{
            toggles = src.toggles.ConvertAll(i => new ToggleContext(i));
            dropdowns = src.dropdowns.ConvertAll(i => new DropdownContext(i));
        }

		
        //------------------------------------------------------
		// accessor
		//------------------------------------------------------
        
        /// <summary>
        /// 共通設定とプラットフォーム固有設定に分離
        /// </summary>
        public void Split(out DefineSymbolContext common, out DefineSymbolContext indivisual)
		{
			common = new DefineSymbolContext();
			indivisual = new DefineSymbolContext();

			foreach (var toggle in toggles)
			{
				(toggle.individual ? indivisual : common).toggles.Add(toggle);
			}

			foreach (var dropdown in dropdowns)
			{
				(dropdown.individual ? indivisual : common).dropdowns.Add(dropdown);
			}
		}
	}
}