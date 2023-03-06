using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.ValueResolvers;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEngine;

public class CompactTabGroupAttributeDrawer : OdinGroupDrawer<CompactTabGroupAttribute>
{
	private class Tab
	{
		public string TabName;
		public List<InspectorProperty> InspectorProperties = new List<InspectorProperty>();
		public ValueResolver<string> Title;
		public ValueResolver<Color> Color;
	}

	private const string CURRENT_TAB_INDEX_KEY = "CurrentTabIndex";
	const float TAB_HEIGHT = 22f;

	private List<Tab> m_tabs = new();

	private GUITabGroup m_tabGroup;

	private float m_previousRectWidth;
	private int m_prevIndex;

	protected override void Initialize()
	{
		m_tabGroup = SirenixEditorGUI.CreateAnimatedTabGroup(Property);

		var groupIndex = 0;

		foreach (var child in Property.Children)
		{
			if (child.Info.PropertyType != PropertyType.Group)
				continue;

			var attribute = child.GetAttribute<PropertyGroupAttribute>();
			var type = attribute.GetType();

			if (!type.IsNested || type.DeclaringType != typeof(CompactTabGroupAttribute))
				continue;

			var tab = new Tab();
			tab.TabName = child.NiceName;
			tab.Title = ValueResolver.GetForString(Property, child.Name.TrimStart('#'));
			tab.Color = ValueResolver.Get(Property, Attribute.Tabs[groupIndex].Color, Color.white);

			foreach (var ch in child.Children)
				tab.InspectorProperties.Add(ch);

			m_tabs.Add(tab);

			groupIndex++;
		}

		Property.State.Create(CURRENT_TAB_INDEX_KEY, true, 0);

		foreach (var tab in m_tabs)
			m_tabGroup.RegisterTab(tab.TabName);

		var currentIndex = Property.State.Get<int>(CURRENT_TAB_INDEX_KEY);

		var tab1 = m_tabs[currentIndex];
		m_tabGroup.SetCurrentPage(m_tabGroup.RegisterTab(tab1.TabName));
	}

	protected override void DrawPropertyLayout(GUIContent label)
	{
		CompactTabGroupAttribute attribute = Attribute;

		if (DrawOnlyTabContents(attribute))
			return;

		m_tabGroup.AnimationSpeed = 1f / SirenixEditorGUI.TabPageSlideAnimationDuration;
		m_tabGroup.FixedHeight = attribute.UseFixedHeight;

		var currentIndex = Property.State.Get<int>(CURRENT_TAB_INDEX_KEY);

		var backgroundColor = currentIndex >= 0 ? m_tabs[currentIndex].Color.GetValue() : Color.white;

		// Animate background color while tab changing animation
		if (m_tabGroup.T > 0f)
		{
			var prevColor = m_tabs[m_prevIndex].Color.GetValue();
			backgroundColor = Color.Lerp(prevColor, backgroundColor, m_tabGroup.T);
		}

		GUIHelper.PushColor(backgroundColor);
		var rect = SirenixEditorGUI.BeginIndentedVertical(
			attribute.Paddingless ? SirenixGUIStyles.None : SirenixGUIStyles.ToggleGroupBackground,
			GUILayoutOptions.ExpandWidth(false));

		GUIHelper.PopColor();

		currentIndex = DrawTabs(m_previousRectWidth, currentIndex);

		DrawTabGroup(currentIndex);

		SirenixEditorGUI.EndIndentedVertical();

		if (Event.current.type == EventType.Repaint)
			m_previousRectWidth = rect.width;
	}

	private void DrawTabGroup(int currentIndex)
	{
		m_tabGroup.BeginGroup(false, style: GUIStyle.none);

		for (var index = 0; index < m_tabs.Count; index++)
		{
			var tab = m_tabs[index];
			var guiTabPage = m_tabGroup.RegisterTab(tab.TabName);
			guiTabPage.Title = tab.TabName;

			if (m_tabGroup.NextPage == null && m_tabGroup.CurrentPage == guiTabPage && currentIndex != index)
			{
				m_prevIndex = currentIndex;
				currentIndex = index;
				Property.State.Set(CURRENT_TAB_INDEX_KEY, index);
			}

			if (guiTabPage.BeginPage())
			{
				foreach (var tabInspectorProperty in tab.InspectorProperties)
				{
					tabInspectorProperty.Update();
					tabInspectorProperty.Draw();
				}
			}

			guiTabPage.EndPage();
		}

		m_tabGroup.EndGroup();
	}

	private int DrawTabs(float contextWidth, int currentIndex)
	{
		SirenixEditorGUI.BeginHorizontalToolbar(TAB_HEIGHT);
		var namesWidthSum = 0f;

		for (var index = 0; index < m_tabs.Count; index++)
		{
			var tab = m_tabs[index];

			var newTabSize = SirenixGUIStyles.ToolbarTab.CalcSize(new GUIContent(tab.TabName)).x;
			namesWidthSum += newTabSize;

			if (namesWidthSum > contextWidth && contextWidth > 0f)
			{
				SirenixEditorGUI.EndHorizontalToolbar();
				SirenixEditorGUI.BeginHorizontalToolbar(TAB_HEIGHT);
				namesWidthSum = newTabSize;
			}

			if (ToolbarTab(index == currentIndex, tab.Title.GetValue(), TAB_HEIGHT, tab.Color.GetValue()) &&
			    index != currentIndex)
			{
				m_prevIndex = currentIndex;
				currentIndex = index;

				m_tabGroup.GoToPage(m_tabGroup.RegisterTab(tab.TabName));
				Property.State.Set(CURRENT_TAB_INDEX_KEY, currentIndex);
			}
		}

		SirenixEditorGUI.EndHorizontalToolbar();

		return currentIndex;
	}

	private bool DrawOnlyTabContents(CompactTabGroupAttribute attribute)
	{
		if (!attribute.HideTabGroupIfTabGroupOnlyHasOneTab || m_tabs.Count > 1)
			return false;

		foreach (var tab in m_tabs)
		{
			foreach (var inspectorProperty in tab.InspectorProperties)
			{
				inspectorProperty.Update();
				inspectorProperty.Draw(inspectorProperty.Label);
			}
		}

		return true;
	}

	public static bool ToolbarTab(bool isActive, string label, float height, Color color)
	{
		var options = GUILayoutOptions.Height(height);
		var content = GUIHelper.TempContent(label);
		var style = SirenixGUIStyles.ToolbarTab;
		var rect = GUILayoutUtility.GetRect(content, style, options);

		GUIHelper.PushColor(color);
		var pressed = GUI.Toggle(rect, isActive, string.Empty, style);
		GUIHelper.PopColor();

		GUI.Label(rect, content, SirenixGUIStyles.LabelCentered);

		if (!pressed)
			return false;

		if (!isActive)
		{
			GUIHelper.RemoveFocusControl();
			GUIHelper.RequestRepaint();
		}

		return true;
	}
}