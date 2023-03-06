using System;
using System.Collections.Generic;
using System.Diagnostics;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Internal;

[Conditional("UNITY_EDITOR")]
[AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = true)]
public class CompactTabGroupAttribute : PropertyGroupAttribute, ISubGroupProviderAttribute
{
	/// <summary>
	/// The default tab group name which is used when the single-parameter constructor is called.
	/// </summary>
	public const string DEFAULT_NAME = "_DefaultCompactTabGroup";
	/// <summary>Name of the tab.</summary>
	public string TabName;
	/// <summary>Tab color expression</summary>
	private string Color;
	/// <summary>
	/// Should this tab be the same height as the rest of the tab group.
	/// </summary>
	public bool UseFixedHeight;
	/// <summary>
	/// If true, the content of each page will not be contained in any box.
	/// </summary>
	public bool Paddingless;
	/// <summary>
	/// If true, the tab group will be hidden if it only contains one tab.
	/// </summary>
	public bool HideTabGroupIfTabGroupOnlyHasOneTab;

	/// <summary>
	/// Organizes the property into the specified tab in the default group.
	/// Default group name is '_DefaultNewTabGroup'
	/// </summary>
	/// <param name="tab">The tab.</param>
	/// <param name="useFixedHeight">if set to <c>true</c> [use fixed height].</param>
	/// <param name="order">The order.</param>
	/// <param name="color">Color of tab. Use expression that will return Unity.Color like '@Color.red`</param>
	public CompactTabGroupAttribute(string tab, bool useFixedHeight = false, float order = 0.0f, string color = "")
		: this(DEFAULT_NAME, tab, useFixedHeight, order, color) { }
	
	/// <summary>
	/// Organizes the property into the specified tab in the specified group.
	/// </summary>
	/// <param name="group">The group to attach the tab to.</param>
	/// <param name="tab">The name of the tab.</param>
	/// <param name="useFixedHeight">Set to true to have a constant height across the entire tab group.</param>
	/// <param name="order">The order of the group.</param>
	/// <param name="color">Color of tab. Use expression that will return Unity.Color like '@Color.red`</param>
	public CompactTabGroupAttribute(string group, string tab, bool useFixedHeight = false, float order = 0.0f, string color = "")
		: base(group, order)
	{
		TabName = tab;
		UseFixedHeight = useFixedHeight;
		Color = color;
		
		Tabs = new List<Tab>();
		
		if (tab == null)
			return;
		
		Tabs.Add(new Tab(tab, color));
	}
	
	public struct Tab : IEquatable<Tab>
	{
		public string Name;
		public string Color;

		public Tab(string name, string color)
		{
			Name = name;
			Color = color;
		}

		public bool Equals(Tab other) => Name == other.Name;

		public override bool Equals(object obj) => obj is Tab other && Equals(other);

		public override int GetHashCode() => (Name != null ? Name.GetHashCode() : 0);
	}

	/// <summary>Name of all tabs in this group.</summary>
	public List<Tab> Tabs { get; private set; }
	
	/// <summary>Combines the tab group with another group.</summary>
	/// <param name="other">The other group.</param>
	protected override void CombineValuesWith(PropertyGroupAttribute other)
	{
		base.CombineValuesWith(other);
		
		if (other is not CompactTabGroupAttribute tabGroupAttribute || tabGroupAttribute.TabName == null)
			return;
		
		UseFixedHeight = UseFixedHeight || tabGroupAttribute.UseFixedHeight;
		Paddingless = Paddingless || tabGroupAttribute.Paddingless;
		HideTabGroupIfTabGroupOnlyHasOneTab = HideTabGroupIfTabGroupOnlyHasOneTab || tabGroupAttribute.HideTabGroupIfTabGroupOnlyHasOneTab;

		var newTab = new Tab(tabGroupAttribute.TabName, tabGroupAttribute.Color);

		var index = Tabs.IndexOf(newTab);
		
		if (index >= 0)
		{
			if (!string.IsNullOrWhiteSpace(newTab.Color))
				Tabs[index] = newTab;

			return;
		}
		
		Tabs.Add(newTab);
	}
	
	/// <summary>Not yet documented.</summary>
	/// <returns>Not yet documented.</returns>
	IList<PropertyGroupAttribute> ISubGroupProviderAttribute.GetSubGroupAttributes()
	{
		int num = 0;
		List<PropertyGroupAttribute> subGroupAttributes = new List<PropertyGroupAttribute>(Tabs.Count);
		
		foreach (var tab in Tabs)
			subGroupAttributes.Add(new ToggleTabSubGroupAttribute(GroupID + "/" + tab.Name, num++));
		
		return subGroupAttributes;
	}

	/// <summary>Not yet documented.</summary>
	/// <returns>Not yet documented.</returns>
	string ISubGroupProviderAttribute.RepathMemberAttribute(PropertyGroupAttribute attr) => GroupID + "/" + ((CompactTabGroupAttribute) attr).TabName;

	[Conditional("UNITY_EDITOR")]
	public class ToggleTabSubGroupAttribute : PropertyGroupAttribute
	{
		public ToggleTabSubGroupAttribute(string groupId, float order)
			: base(groupId, order)
		{
		}
	}
}