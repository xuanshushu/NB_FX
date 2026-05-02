using System;
using UnityEditor;
using UnityEngine;

namespace NBShaderEditor
{
    public class BlockItem : ShaderGUIItem
    {
        protected readonly ShaderGUIRootItem SharedRootItem;
        private readonly ShaderGUIFoldOutHelper _foldOutHelper;
        private readonly Func<GUIContent> _contentProvider;

        protected virtual GUIStyle TitleStyle => EditorStyles.label;
        protected virtual bool DrawSeparatorLine => false;

        public string FoldOutPropertyName { get; }

        public BlockItem(
            ShaderGUIRootItem rootItem,
            ShaderGUIItem parentItem,
            string foldOutPropertyName,
            Func<GUIContent> contentProvider) : base(rootItem, parentItem)
        {
            SharedRootItem = rootItem;
            FoldOutPropertyName = foldOutPropertyName;
            _contentProvider = contentProvider ?? (() => GUIContent.none);
            _foldOutHelper = new ShaderGUIFoldOutHelper(rootItem, foldOutPropertyName);
        }

        public override void OnGUI()
        {
            GuiContent = _contentProvider();
            DrawLeadingSpace();
            GetRect();
            _foldOutHelper.DrawFoldOut(LabelRect);
            using (ParentControlDisabledScope())
            {
                EditorGUI.LabelField(LabelRect, GuiContent, TitleStyle);
            }

            DrawResetButton();
            EditorGUI.indentLevel++;
            if (_foldOutHelper.BeginFadeGroup())
            {
                DrawBlock();
            }
            _foldOutHelper.EndFadedGroup();
            EditorGUI.indentLevel--;
            if (DrawSeparatorLine)
            {
                Rect rect = ApplyGlobalRectCompensation(EditorGUILayout.GetControlRect(false, 1));
                EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            }
        }

        protected virtual void DrawLeadingSpace()
        {
        }

        public override void DrawBlock()
        {
            for (int i = 0; i < ChildrenItemList.Count; i++)
            {
                ChildrenItemList[i].OnGUI();
            }
        }
    }

    public class BigBlockItem : BlockItem
    {
        protected override GUIStyle TitleStyle => EditorStyles.boldLabel;
        protected override bool DrawSeparatorLine => true;

        public BigBlockItem(
            ShaderGUIRootItem rootItem,
            ShaderGUIItem parentItem,
            string foldOutPropertyName,
            Func<GUIContent> contentProvider) : base(rootItem, parentItem, foldOutPropertyName, contentProvider)
        {
        }

        protected override void DrawLeadingSpace()
        {
            EditorGUILayout.Space();
        }
    }

}
