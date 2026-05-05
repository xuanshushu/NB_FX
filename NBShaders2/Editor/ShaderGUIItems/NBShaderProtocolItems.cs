using System;
using UnityEditor;
using UnityEngine;
using NBShader;

namespace NBShaderEditor
{
    public class PropertyToggleBlockItem : ShaderGUIItem
    {
        private readonly string _foldOutPropertyName;
        private readonly Func<GUIContent> _contentProvider;
        private readonly int _flagBits;
        private readonly int _flagIndex;
        private readonly string _keyword;
        private readonly string _shaderPassName;
        private readonly Action<bool> _onValueChanged;
        private readonly Func<bool> _isVisible;
        private readonly GUIStyle _labelStyle;
        private readonly ShaderGUIFoldOutHelper _foldOutHelper;

        public PropertyToggleBlockItem(
            NBShaderRootItem rootItem,
            ShaderGUIItem parentItem,
            string foldOutPropertyName,
            string togglePropertyName,
            Func<GUIContent> contentProvider,
            int flagBits = 0,
            int flagIndex = 0,
            string keyword = null,
            string shaderPassName = null,
            Action<bool> onValueChanged = null,
            Func<bool> isVisible = null,
            bool bold = false) : base(rootItem, parentItem)
        {
            _foldOutPropertyName = foldOutPropertyName;
            PropertyName = togglePropertyName;
            _contentProvider = contentProvider ?? (() => GUIContent.none);
            _flagBits = flagBits;
            _flagIndex = flagIndex;
            _keyword = keyword;
            _shaderPassName = shaderPassName;
            _onValueChanged = onValueChanged;
            _isVisible = isVisible;
            _labelStyle = new GUIStyle(EditorStyles.label)
            {
                fontStyle = bold ? FontStyle.Bold : FontStyle.Normal
            };
            _foldOutHelper = new ShaderGUIFoldOutHelper(rootItem, foldOutPropertyName);
            InitTriggerByChild();
        }

        public override void OnGUI()
        {
            if (!FeatureToggleFoldOutItem.IsTierKeywordAllowed(RootItem as NBShaderRootItem, _keyword))
            {
                return;
            }

            if (_isVisible != null && !_isVisible())
            {
                return;
            }

            GuiContent = _contentProvider();
            GetRect();
            MaterialProperty property = PropertyInfo.Property;

            bool enabled = property.floatValue > 0.5f;
            using (ParentControlDisabledScope())
            {
                EditorGUI.showMixedValue = property.hasMixedValue;
                EditorGUI.BeginChangeCheck();
                bool animatedScope = BeginAnimatedPropertyBackground(ControlRect, property);
                using (new EditorGUIIndentLevelScope(0))
                {
                    enabled = EditorGUI.Toggle(ControlRect, enabled);
                }
                EndAnimatedPropertyBackground(animatedScope);
                EditorGUI.showMixedValue = false;
                if (EditorGUI.EndChangeCheck())
                {
                    property.floatValue = enabled ? 1f : 0f;
                    ApplySideEffects(enabled);
                    OnEndChange();
                }
            }

            Rect foldOutRect = LabelRect;
            foldOutRect.width = Mathf.Max(0f, ControlRect.x - LabelRect.x);
            _foldOutHelper.DrawFoldOut(foldOutRect);
            using (ParentControlDisabledScope())
            {
                EditorGUI.LabelField(LabelRect, GuiContent, _labelStyle);
            }

            DrawResetButton();

            if (_foldOutHelper.BeginFadeGroup())
            {
                EditorGUI.indentLevel++;
                using (new InheritedControlDisabledScope(property.hasMixedValue || property.floatValue <= 0.5f))
                {
                    DrawBlock();
                }
                EditorGUI.indentLevel--;
            }

            _foldOutHelper.EndFadedGroup();
        }

        public override void ExecuteReset(bool isCallByParent = false)
        {
            base.ExecuteReset(isCallByParent);
            ApplySideEffects(PropertyInfo.Property.floatValue > 0.5f);
        }

        public override void DrawBlock()
        {
            for (int i = 0; i < ChildrenItemList.Count; i++)
            {
                ChildrenItemList[i].OnGUI();
            }
        }

        private void ApplySideEffects(bool enabled)
        {
            if (RootItem is NBShaderRootItem nbRootItem)
            {
                if (_flagBits != 0)
                {
                    nbRootItem.SyncService.ApplyToggleFlag(_flagBits, enabled, _flagIndex);
                }

                if (!string.IsNullOrEmpty(_keyword))
                {
                    nbRootItem.SyncService.ApplyToggleKeyword(_keyword, enabled);
                }

                if (!string.IsNullOrEmpty(_shaderPassName))
                {
                    nbRootItem.SyncService.ApplyShaderPass(_shaderPassName, enabled);
                }
            }

            _onValueChanged?.Invoke(enabled);
        }
    }

    public class NBShaderKeywordToggleItem : ToggleItem
    {
        public NBShaderKeywordToggleItem(
            NBShaderRootItem rootItem,
            ShaderGUIItem parentItem,
            string propertyName,
            string keyword,
            Func<GUIContent> contentProvider,
            Action<bool> onValueChanged = null,
            Func<bool> isVisible = null)
            : base(
                rootItem,
                parentItem,
                propertyName,
                contentProvider,
                enabled =>
                {
                    rootItem.SyncService.ApplyToggleKeyword(keyword, enabled);
                    onValueChanged?.Invoke(enabled);
                },
                FeatureToggleFoldOutItem.TierVisible(rootItem, keyword, isVisible))
        {
        }
    }

    public class TextureRelatedFoldOutItem : ShaderGUIItem
    {
        private readonly ShaderGUIFoldOutHelper _foldOutHelper;
        private readonly string _texturePropertyName;
        private readonly Func<GUIContent> _contentProvider;
        private readonly Func<bool> _isVisible;

        public TextureRelatedFoldOutItem(
            ShaderGUIRootItem rootItem,
            ShaderGUIItem parentItem,
            string foldOutPropertyName,
            string texturePropertyName,
            Func<GUIContent> contentProvider,
            Func<bool> isVisible = null) : base(rootItem, parentItem)
        {
            _foldOutHelper = new ShaderGUIFoldOutHelper(rootItem, foldOutPropertyName);
            _texturePropertyName = texturePropertyName;
            _contentProvider = contentProvider ?? (() => GUIContent.none);
            _isVisible = isVisible;
            CheckIsPropertyModified();
        }

        public override void OnGUI()
        {
            if (_isVisible != null && !_isVisible())
            {
                return;
            }

            GetRect();
            _foldOutHelper.DrawFoldOut(LabelRect);
            using (ParentControlDisabledScope())
            {
                EditorGUI.LabelField(LabelRect, _contentProvider(), EditorStyles.boldLabel);
            }

            DrawResetButton();

            if (_foldOutHelper.BeginFadeGroup())
            {
                EditorGUI.indentLevel++;
                using (new InheritedControlDisabledScope(!HasTexture()))
                {
                    DrawBlock();
                }

                EditorGUI.indentLevel--;
            }

            _foldOutHelper.EndFadedGroup();
        }

        public override void DrawBlock()
        {
            for (int i = 0; i < ChildrenItemList.Count; i++)
            {
                ChildrenItemList[i].OnGUI();
            }
        }

        private bool HasTexture()
        {
            return RootItem.PropertyInfoDic.TryGetValue(_texturePropertyName, out ShaderPropertyInfo info) &&
                   (info.Property.hasMixedValue || info.Property.textureValue != null);
        }
    }

    public class ColorChannelSelectItem : ShaderGUIItem
    {
        private static readonly string[] ChannelNames = { "R", "G", "B", "A" };
        private readonly Func<GUIContent> _contentProvider;
        private readonly int _colorChannelFlagPos;
        private readonly int _defaultChannel;
        private readonly Func<bool> _isVisible;

        public ColorChannelSelectItem(
            ShaderGUIRootItem rootItem,
            ShaderGUIItem parentItem,
            int colorChannelFlagPos,
            int defaultChannel,
            Func<GUIContent> contentProvider,
            Func<bool> isVisible = null) : base(rootItem, parentItem)
        {
            _colorChannelFlagPos = colorChannelFlagPos;
            _defaultChannel = Mathf.Clamp(defaultChannel, 0, 3);
            _contentProvider = contentProvider ?? (() => GUIContent.none);
            _isVisible = isVisible;
            CheckIsPropertyModified();
        }

        public override void OnGUI()
        {
            if (_isVisible != null && !_isVisible())
            {
                return;
            }

            GetRect();
            using (ParentControlDisabledScope())
            {
                EditorGUI.LabelField(LabelRect, _contentProvider());
            }

            int channel = GetFirstChannel();
            using (ParentControlDisabledScope())
            {
                EditorGUI.showMixedValue = HasMixedValue();
                EditorGUI.BeginChangeCheck();
                using (new EditorGUIIndentLevelScope(0))
                {
                    channel = EditorGUI.Popup(
                        ControlRect,
                        channel,
                        NBShaderInspectorLocalization.GetInspectorOptions("protocol.colorChannel", ChannelNames));
                }
                EditorGUI.showMixedValue = false;
                if (EditorGUI.EndChangeCheck())
                {
                    SetChannel(channel);
                    CheckIsPropertyModified();
                }
            }

            DrawResetButton();
        }

        public override void CheckIsPropertyModified(bool isCallByChild = false)
        {
            PropertyIsDefaultValue = !HasMixedValue() && GetFirstChannel() == _defaultChannel;
            HasModified = !PropertyIsDefaultValue;
            ParentItem?.CheckIsPropertyModified(true);
        }

        public override void ExecuteReset(bool isCallByParent = false)
        {
            SetChannel(_defaultChannel);
            PropertyIsDefaultValue = true;
            HasModified = false;
            if (!isCallByParent)
            {
                ParentItem?.CheckIsPropertyModified(true);
            }
        }

        private int GetFirstChannel()
        {
            NBShaderFlags flags = GetFlags(0);
            return flags == null ? _defaultChannel : (int)flags.GetColorChanel(_colorChannelFlagPos);
        }

        private bool HasMixedValue()
        {
            int first = GetFirstChannel();
            for (int i = 1; i < RootItem.ShaderFlags.Count; i++)
            {
                NBShaderFlags flags = GetFlags(i);
                if (flags != null && (int)flags.GetColorChanel(_colorChannelFlagPos) != first)
                {
                    return true;
                }
            }

            return false;
        }

        private void SetChannel(int channel)
        {
            for (int i = 0; i < RootItem.ShaderFlags.Count; i++)
            {
                GetFlags(i)?.SetColorChanel((NBShaderFlags.ColorChannel)channel, _colorChannelFlagPos);
            }
        }

        private NBShaderFlags GetFlags(int index)
        {
            return index >= 0 &&
                   index < RootItem.ShaderFlags.Count &&
                   RootItem.ShaderFlags[index] is NBShaderFlags flags
                ? flags
                : null;
        }
    }

    public class CustomDataSelectItem : ShaderGUIItem
    {
        private static readonly string[] Options =
        {
            "关闭",
            "CustomData1.x",
            "CustomData1.y",
            "CustomData1.z",
            "CustomData1.w",
            "CustomData2.x",
            "CustomData2.y",
            "CustomData2.z",
            "CustomData2.w"
        };

        private readonly Func<GUIContent> _contentProvider;
        private readonly int _dataBitPos;
        private readonly int _dataIndex;
        private readonly Func<bool> _isVisible;

        public CustomDataSelectItem(
            ShaderGUIRootItem rootItem,
            ShaderGUIItem parentItem,
            int dataBitPos,
            int dataIndex,
            Func<GUIContent> contentProvider,
            Func<bool> isVisible = null) : base(rootItem, parentItem)
        {
            _dataBitPos = dataBitPos;
            _dataIndex = dataIndex;
            _contentProvider = contentProvider ?? (() => GUIContent.none);
            _isVisible = isVisible;
            CheckIsPropertyModified();
        }

        public override void OnGUI()
        {
            if (_isVisible != null && !_isVisible())
            {
                return;
            }

            GetRect();
            using (ParentControlDisabledScope())
            {
                EditorGUI.LabelField(LabelRect, _contentProvider());
            }

            NBShaderFlags.CutomDataComponent component = GetFirstComponent();
            using (ParentControlDisabledScope())
            {
                EditorGUI.showMixedValue = HasMixedValue();
                EditorGUI.BeginChangeCheck();
                int index;
                using (new EditorGUIIndentLevelScope(0))
                {
                    index = EditorGUI.Popup(
                        ControlRect,
                        (int)component,
                        NBShaderInspectorLocalization.GetInspectorOptions("protocol.customData", Options));
                }
                EditorGUI.showMixedValue = false;
                if (EditorGUI.EndChangeCheck())
                {
                    SetComponent((NBShaderFlags.CutomDataComponent)index);
                    CheckIsPropertyModified();
                    if (RootItem is NBShaderRootItem nbRootItem)
                    {
                        nbRootItem.SyncService.SyncMaterialState();
                    }
                }
            }

            DrawResetButton();
        }

        public override void CheckIsPropertyModified(bool isCallByChild = false)
        {
            PropertyIsDefaultValue = !HasMixedValue() && GetFirstComponent() == NBShaderFlags.CutomDataComponent.Off;
            HasModified = !PropertyIsDefaultValue;
            ParentItem?.CheckIsPropertyModified(true);
        }

        public override void ExecuteReset(bool isCallByParent = false)
        {
            SetComponent(NBShaderFlags.CutomDataComponent.Off);
            if (RootItem is NBShaderRootItem nbRootItem)
            {
                nbRootItem.SyncService.SyncMaterialState();
            }

            PropertyIsDefaultValue = true;
            HasModified = false;
            if (!isCallByParent)
            {
                ParentItem?.CheckIsPropertyModified(true);
            }
        }

        private NBShaderFlags.CutomDataComponent GetFirstComponent()
        {
            NBShaderFlags flags = GetFlags(0);
            return flags == null ? NBShaderFlags.CutomDataComponent.Off : flags.GetCustomDataFlag(_dataBitPos, _dataIndex);
        }

        private bool HasMixedValue()
        {
            NBShaderFlags.CutomDataComponent first = GetFirstComponent();
            for (int i = 1; i < RootItem.ShaderFlags.Count; i++)
            {
                NBShaderFlags flags = GetFlags(i);
                if (flags != null && flags.GetCustomDataFlag(_dataBitPos, _dataIndex) != first)
                {
                    return true;
                }
            }

            return false;
        }

        private void SetComponent(NBShaderFlags.CutomDataComponent component)
        {
            for (int i = 0; i < RootItem.ShaderFlags.Count; i++)
            {
                GetFlags(i)?.SetCustomDataFlag(component, _dataBitPos, _dataIndex);
            }
        }

        private NBShaderFlags GetFlags(int index)
        {
            return index >= 0 &&
                   index < RootItem.ShaderFlags.Count &&
                   RootItem.ShaderFlags[index] is NBShaderFlags flags
                ? flags
                : null;
        }
    }

    public class WrapModeItem : ShaderGUIItem
    {
        private static readonly string[] Options =
        {
            "Repeat",
            "Clamp",
            "RepeatX_ClampY",
            "ClampX_RepeatY"
        };

        private readonly Func<GUIContent> _contentProvider;
        private readonly int _wrapFlagBits;
        private readonly int _flagIndex;
        private readonly Func<bool> _isVisible;

        public WrapModeItem(
            ShaderGUIRootItem rootItem,
            ShaderGUIItem parentItem,
            int wrapFlagBits,
            Func<GUIContent> contentProvider,
            int flagIndex = 2,
            Func<bool> isVisible = null) : base(rootItem, parentItem)
        {
            _wrapFlagBits = wrapFlagBits;
            _flagIndex = flagIndex;
            _contentProvider = contentProvider ?? (() => GUIContent.none);
            _isVisible = isVisible;
            CheckIsPropertyModified();
        }

        public override void OnGUI()
        {
            if (_isVisible != null && !_isVisible())
            {
                return;
            }

            GetRect();
            using (ParentControlDisabledScope())
            {
                EditorGUI.LabelField(LabelRect, _contentProvider());
            }

            int mode = GetFirstMode();
            using (ParentControlDisabledScope())
            {
                EditorGUI.showMixedValue = HasMixedValue();
                EditorGUI.BeginChangeCheck();
                using (new EditorGUIIndentLevelScope(0))
                {
                    mode = EditorGUI.Popup(
                        ControlRect,
                        mode,
                        NBShaderInspectorLocalization.GetInspectorOptions("protocol.wrapMode", Options));
                }
                EditorGUI.showMixedValue = false;
                if (EditorGUI.EndChangeCheck())
                {
                    SetMode(mode);
                    CheckIsPropertyModified();
                }
            }

            DrawResetButton();
        }

        public override void CheckIsPropertyModified(bool isCallByChild = false)
        {
            PropertyIsDefaultValue = !HasMixedValue() && GetFirstMode() == 0;
            HasModified = !PropertyIsDefaultValue;
            ParentItem?.CheckIsPropertyModified(true);
        }

        public override void ExecuteReset(bool isCallByParent = false)
        {
            SetMode(0);
            PropertyIsDefaultValue = true;
            HasModified = false;
            if (!isCallByParent)
            {
                ParentItem?.CheckIsPropertyModified(true);
            }
        }

        private int GetFirstMode()
        {
            ShaderFlagsBase flags = RootItem.ShaderFlags.Count > 0 ? RootItem.ShaderFlags[0] : null;
            if (flags == null)
            {
                return 0;
            }

            int mode = flags.CheckFlagBits(_wrapFlagBits, index: _flagIndex) ? 1 : 0;
            if (flags.CheckFlagBits(_wrapFlagBits << 16, index: _flagIndex))
            {
                mode += 2;
            }

            return mode;
        }

        private bool HasMixedValue()
        {
            int first = GetFirstMode();
            for (int i = 1; i < RootItem.ShaderFlags.Count; i++)
            {
                ShaderFlagsBase flags = RootItem.ShaderFlags[i];
                int mode = flags.CheckFlagBits(_wrapFlagBits, index: _flagIndex) ? 1 : 0;
                if (flags.CheckFlagBits(_wrapFlagBits << 16, index: _flagIndex))
                {
                    mode += 2;
                }

                if (mode != first)
                {
                    return true;
                }
            }

            return false;
        }

        private void SetMode(int mode)
        {
            for (int i = 0; i < RootItem.ShaderFlags.Count; i++)
            {
                ShaderFlagsBase flags = RootItem.ShaderFlags[i];
                SetFlag(flags, _wrapFlagBits, (mode & 1) != 0, _flagIndex);
                SetFlag(flags, _wrapFlagBits << 16, (mode & 2) != 0, _flagIndex);
            }
        }

        private static void SetFlag(ShaderFlagsBase flags, int bits, bool enabled, int index)
        {
            if (enabled)
            {
                flags.SetFlagBits(bits, index: index);
            }
            else
            {
                flags.ClearFlagBits(bits, index: index);
            }
        }
    }

    public class PNoiseBlendModeItem : ShaderGUIItem
    {
        private static readonly string[] Options = { "不使用", "Multiply", "Min", "HardLight" };
        private readonly Func<GUIContent> _contentProvider;
        private readonly int _pNoiseBlendModeFlagPos;
        private readonly Func<bool> _isVisible;
        private readonly ShaderGUISliderItem _opacitySlider;

        public PNoiseBlendModeItem(
            NBShaderRootItem rootItem,
            ShaderGUIItem parentItem,
            int pNoiseBlendModeFlagPos,
            string opacityPropertyName,
            Func<GUIContent> contentProvider,
            Func<bool> isVisible = null) : base(rootItem, parentItem)
        {
            _pNoiseBlendModeFlagPos = pNoiseBlendModeFlagPos;
            _contentProvider = contentProvider ?? (() => GUIContent.none);
            _isVisible = isVisible;
            _opacitySlider = new ShaderGUISliderItem(rootItem, this)
            {
                PropertyName = opacityPropertyName,
                GuiContent = NBShaderInspectorLocalization.MakeInspectorContent("protocol.pnoise.opacity", "Program Noise Blend Opacity"),
                Min = 0f,
                Max = 1f
            };
            _opacitySlider.InitTriggerByChild();
            CheckIsPropertyModified();
        }

        public override void OnGUI()
        {
            if (_isVisible != null && !_isVisible())
            {
                return;
            }

            GetRect();
            using (ParentControlDisabledScope())
            {
                EditorGUI.LabelField(LabelRect, _contentProvider());
            }

            NBShaderFlags.PNoiseBlendMode mode = GetFirstMode();
            using (ParentControlDisabledScope())
            {
                EditorGUI.showMixedValue = HasMixedValue();
                EditorGUI.BeginChangeCheck();
                int index;
                using (new EditorGUIIndentLevelScope(0))
                {
                    index = EditorGUI.Popup(
                        ControlRect,
                        (int)mode,
                        NBShaderInspectorLocalization.GetInspectorOptions("protocol.pnoiseBlend", Options));
                }
                EditorGUI.showMixedValue = false;
                if (EditorGUI.EndChangeCheck())
                {
                    SetMode((NBShaderFlags.PNoiseBlendMode)index);
                    CheckIsPropertyModified();
                }
            }

            DrawResetButton();
            _opacitySlider.GuiContent = NBShaderInspectorLocalization.MakeInspectorContent("protocol.pnoise.opacity", "Program Noise Blend Opacity");
            _opacitySlider.OnGUI();
        }

        public override void CheckIsPropertyModified(bool isCallByChild = false)
        {
            PropertyIsDefaultValue = !HasMixedValue() && GetFirstMode() == NBShaderFlags.PNoiseBlendMode.NotUse;
            HasModified = !PropertyIsDefaultValue;
            foreach (ShaderGUIItem childItem in ChildrenItemList)
            {
                HasModified |= childItem.HasModified;
            }
            ParentItem?.CheckIsPropertyModified(true);
        }

        public override void ExecuteReset(bool isCallByParent = false)
        {
            SetMode(NBShaderFlags.PNoiseBlendMode.NotUse);
            PropertyIsDefaultValue = true;
            foreach (ShaderGUIItem childItem in ChildrenItemList)
            {
                childItem.ExecuteReset(true);
            }
            HasModified = false;
            if (!isCallByParent)
            {
                ParentItem?.CheckIsPropertyModified(true);
            }
        }

        private NBShaderFlags.PNoiseBlendMode GetFirstMode()
        {
            NBShaderFlags flags = GetFlags(0);
            return flags == null ? NBShaderFlags.PNoiseBlendMode.NotUse : flags.GetPNoiseBlendMode(_pNoiseBlendModeFlagPos);
        }

        private bool HasMixedValue()
        {
            NBShaderFlags.PNoiseBlendMode first = GetFirstMode();
            for (int i = 1; i < RootItem.ShaderFlags.Count; i++)
            {
                NBShaderFlags flags = GetFlags(i);
                if (flags != null && flags.GetPNoiseBlendMode(_pNoiseBlendModeFlagPos) != first)
                {
                    return true;
                }
            }

            return false;
        }

        private void SetMode(NBShaderFlags.PNoiseBlendMode mode)
        {
            for (int i = 0; i < RootItem.ShaderFlags.Count; i++)
            {
                GetFlags(i)?.SetPNoiseBlendMode(mode, _pNoiseBlendModeFlagPos);
            }
        }

        private NBShaderFlags GetFlags(int index)
        {
            return index >= 0 &&
                   index < RootItem.ShaderFlags.Count &&
                   RootItem.ShaderFlags[index] is NBShaderFlags flags
                ? flags
                : null;
        }
    }

    public class UVModeSelectItem : ShaderGUIItem
    {
        private static readonly string[] UVModeNames =
        {
            "默认UV通道",
            "特殊UV通道",
            "极坐标|旋转",
            "圆柱无缝",
            "主贴图",
            "屏幕UV",
            "世界坐标",
            "局部本地坐标",
            "公共UV"
        };

        private static readonly string[] PosUVModeNames = { "xy平面", "xz平面", "yz平面" };
        private static readonly string[] SpecialUVChannelNames = { "UV2_Texcoord1", "UV3_Texcoord2" };

        private readonly Func<GUIContent> _contentProvider;
        private readonly string _foldOutPropertyName;
        private readonly int _uvModeBitPos;
        private readonly int _uvModeFlagIndex;
        private readonly string _texturePropertyName;
        private readonly bool _forceEnable;
        private readonly Func<bool> _isVisible;
        private readonly ShaderGUIFoldOutHelper _foldOutHelper;

        private readonly SpecialUVChannelModeItem _specialUVChannelItem;
        private readonly PropertyToggleBlockItem _twirlBlock;
        private readonly Vector2LineItem _twirlCenterItem;
        private readonly ShaderGUIFloatItem _twirlStrengthItem;
        private readonly PropertyToggleBlockItem _polarBlock;
        private readonly Vector2LineItem _polarCenterItem;
        private readonly VectorComponentItem _polarStrengthItem;
        private readonly Vector3Item _cylinderRotateItem;
        private readonly Vector3Item _cylinderOffsetItem;
        private readonly ShaderGUIPopUpItem _worldSpaceItem;
        private readonly ShaderGUIPopUpItem _objectSpaceItem;

        public UVModeSelectItem(
            NBShaderRootItem rootItem,
            ShaderGUIItem parentItem,
            string foldOutPropertyName,
            int uvModeBitPos,
            int uvModeFlagIndex,
            Func<GUIContent> contentProvider,
            string texturePropertyName = null,
            bool forceEnable = false,
            Func<bool> isVisible = null) : base(rootItem, parentItem)
        {
            _foldOutPropertyName = foldOutPropertyName;
            _uvModeBitPos = uvModeBitPos;
            _uvModeFlagIndex = uvModeFlagIndex;
            _contentProvider = contentProvider ?? (() => GUIContent.none);
            _texturePropertyName = texturePropertyName;
            _forceEnable = forceEnable;
            _isVisible = isVisible;
            _foldOutHelper = new ShaderGUIFoldOutHelper(rootItem, foldOutPropertyName);

            _specialUVChannelItem = new SpecialUVChannelModeItem(rootItem, this);
            _twirlBlock = new PropertyToggleBlockItem(
                rootItem,
                this,
                "_GlobalTwirlFoldOut",
                "_UTwirlEnabled",
                () => NBShaderInspectorLocalization.MakeInspectorContent("protocol.uv.twirl", "Twirl"),
                NBShaderFlags.FLAG_BIT_PARTICLE_UTWIRL_ON,
                0);
            _twirlCenterItem = new Vector2LineItem(rootItem, _twirlBlock, "_TWParameter", true, () => NBShaderInspectorLocalization.MakeInspectorContent("protocol.uv.twirlCenter", "Twirl Center"));
            _twirlStrengthItem = new ShaderGUIFloatItem(rootItem, _twirlBlock)
            {
                PropertyName = "_TWStrength",
                GuiContent = NBShaderInspectorLocalization.MakeInspectorContent("protocol.uv.twirlStrength", "Twirl Strength")
            };
            _twirlStrengthItem.InitTriggerByChild();

            _polarBlock = new PropertyToggleBlockItem(
                rootItem,
                this,
                "_GlobalPolarFoldOut",
                "_PolarCoordinatesEnabled",
                () => NBShaderInspectorLocalization.MakeInspectorContent("protocol.uv.polar", "Polar Coordinates"),
                NBShaderFlags.FLAG_BIT_PARTICLE_POLARCOORDINATES_ON,
                0);
            _polarCenterItem = new Vector2LineItem(rootItem, _polarBlock, "_PCCenter", true, () => NBShaderInspectorLocalization.MakeInspectorContent("protocol.uv.polarCenter", "Polar Center"));
            _polarStrengthItem = new VectorComponentItem(rootItem, _polarBlock, "_PCCenter", 2, () => NBShaderInspectorLocalization.MakeInspectorContent("protocol.uv.polarStrength", "Polar Strength"), true, 0f, 1f);

            _cylinderRotateItem = new Vector3Item(rootItem, this, "_CylinderUVRotate", () => NBShaderInspectorLocalization.MakeInspectorContent("protocol.uv.cylinderRotate", "Cylinder Rotation"), _ => UpdateCylinderMatrix(rootItem));
            _cylinderOffsetItem = new Vector3Item(rootItem, this, "_CylinderUVPosOffset", () => NBShaderInspectorLocalization.MakeInspectorContent("protocol.uv.cylinderOffset", "Cylinder Offset"), _ => UpdateCylinderMatrix(rootItem));

            _worldSpaceItem = new ShaderGUIPopUpItem(rootItem, this)
            {
                PropertyName = "_WorldSpaceUVModeSelector",
                GuiContent = NBShaderInspectorLocalization.MakeInspectorContent("protocol.uv.coordinatePlane", "Coordinate Plane"),
                PopUpNames = NBShaderInspectorLocalization.GetInspectorOptions("protocol.uv.positionPlane", PosUVModeNames)
            };
            _worldSpaceItem.InitTriggerByChild();

            _objectSpaceItem = new ShaderGUIPopUpItem(rootItem, this)
            {
                PropertyName = "_ObjectSpaceUVModeSelector",
                GuiContent = NBShaderInspectorLocalization.MakeInspectorContent("protocol.uv.coordinatePlane", "Coordinate Plane"),
                PopUpNames = NBShaderInspectorLocalization.GetInspectorOptions("protocol.uv.positionPlane", PosUVModeNames)
            };
            _objectSpaceItem.InitTriggerByChild();

            CheckIsPropertyModified();
        }

        public override void OnGUI()
        {
            if (_isVisible != null && !_isVisible())
            {
                return;
            }

            bool controlDisabled = !_forceEnable && !HasTexture();
            GetRect();
            using (ParentControlDisabledScope(controlDisabled))
            {
                EditorGUI.LabelField(LabelRect, _contentProvider());
            }

            NBShaderFlags.UVMode mode = GetFirstMode();
            using (ParentControlDisabledScope(controlDisabled))
            {
                EditorGUI.showMixedValue = HasMixedValue();
                EditorGUI.BeginChangeCheck();
                int index;
                using (new EditorGUIIndentLevelScope(0))
                {
                    index = EditorGUI.Popup(
                        ControlRect,
                        (int)mode,
                        NBShaderInspectorLocalization.GetInspectorOptions("protocol.uv.mode", UVModeNames));
                }
                EditorGUI.showMixedValue = false;
                if (EditorGUI.EndChangeCheck())
                {
                    mode = (NBShaderFlags.UVMode)index;
                    SetMode(mode);
                    _foldOutHelper.SetOpen(NeedsFoldOut(mode));
                    CheckIsPropertyModified();
                }

                DrawResetButton();
            }

            bool needFoldOut = NeedsFoldOut(mode);
            if (needFoldOut)
            {
                _foldOutHelper.DrawFoldOut(LabelRect);
            }

            if (needFoldOut && !HasMixedValue())
            {
                if (_foldOutHelper.BeginFadeGroup())
                {
                    EditorGUI.indentLevel++;
                    using (ParentControlDisabledScope(controlDisabled))
                    {
                        EditorGUILayout.LabelField(
                            NBShaderInspectorLocalization.GetInspectorText(
                                "protocol.uv.sharedMaterial.message",
                                "The following settings are shared in the material:"),
                            EditorStyles.boldLabel);
                    }
                    using (new InheritedControlDisabledScope(controlDisabled))
                    {
                        switch (mode)
                        {
                            case NBShaderFlags.UVMode.SpecialUVChannel:
                                _specialUVChannelItem.OnGUI();
                                break;
                            case NBShaderFlags.UVMode.PolarOrTwirl:
                                _twirlBlock.OnGUI();
                                _polarBlock.OnGUI();
                                break;
                            case NBShaderFlags.UVMode.Cylinder:
                                using (ParentControlDisabledScope())
                                {
                                    EditorGUILayout.LabelField(
                                        NBShaderInspectorLocalization.GetInspectorText(
                                            "protocol.uv.cylinderWarning.message",
                                            "Cylinder mode is expensive. Use it carefully."));
                                }

                                _cylinderRotateItem.OnGUI();
                                _cylinderOffsetItem.OnGUI();
                                UpdateCylinderMatrix(RootItem);
                                break;
                            case NBShaderFlags.UVMode.WorldPos:
                                _worldSpaceItem.GuiContent = NBShaderInspectorLocalization.MakeInspectorContent("protocol.uv.coordinatePlane", "Coordinate Plane");
                                _worldSpaceItem.PopUpNames = NBShaderInspectorLocalization.GetInspectorOptions("protocol.uv.positionPlane", PosUVModeNames);
                                _worldSpaceItem.OnGUI();
                                break;
                            case NBShaderFlags.UVMode.ObjectPos:
                                _objectSpaceItem.GuiContent = NBShaderInspectorLocalization.MakeInspectorContent("protocol.uv.coordinatePlane", "Coordinate Plane");
                                _objectSpaceItem.PopUpNames = NBShaderInspectorLocalization.GetInspectorOptions("protocol.uv.positionPlane", PosUVModeNames);
                                _objectSpaceItem.OnGUI();
                                break;
                        }
                    }

                    EditorGUI.indentLevel--;
                }

                _foldOutHelper.EndFadedGroup();
            }
        }

        public override void CheckIsPropertyModified(bool isCallByChild = false)
        {
            PropertyIsDefaultValue = !HasMixedValue() && GetFirstMode() == NBShaderFlags.UVMode.DefaultUVChannel;
            HasModified = !PropertyIsDefaultValue;
            foreach (ShaderGUIItem childItem in ChildrenItemList)
            {
                HasModified |= childItem.HasModified;
            }
            ParentItem?.CheckIsPropertyModified(true);
        }

        public override void ExecuteReset(bool isCallByParent = false)
        {
            SetMode(NBShaderFlags.UVMode.DefaultUVChannel);
            PropertyIsDefaultValue = true;
            HasModified = false;
            if (!isCallByParent)
            {
                ParentItem?.CheckIsPropertyModified(true);
            }
        }

        private static bool NeedsFoldOut(NBShaderFlags.UVMode mode)
        {
            return mode != NBShaderFlags.UVMode.DefaultUVChannel &&
                   mode != NBShaderFlags.UVMode.CommonUV &&
                   mode != NBShaderFlags.UVMode.ScreenUV &&
                   mode != NBShaderFlags.UVMode.MainTex;
        }

        private bool HasTexture()
        {
            if (string.IsNullOrEmpty(_texturePropertyName) ||
                !RootItem.PropertyInfoDic.TryGetValue(_texturePropertyName, out ShaderPropertyInfo info))
            {
                return true;
            }

            return info.Property.hasMixedValue || info.Property.textureValue != null;
        }

        private NBShaderFlags.UVMode GetFirstMode()
        {
            NBShaderFlags flags = GetFlags(0);
            return flags == null ? NBShaderFlags.UVMode.DefaultUVChannel : flags.GetUVMode(_uvModeBitPos, _uvModeFlagIndex);
        }

        private bool HasMixedValue()
        {
            NBShaderFlags.UVMode first = GetFirstMode();
            for (int i = 1; i < RootItem.ShaderFlags.Count; i++)
            {
                NBShaderFlags flags = GetFlags(i);
                if (flags != null && flags.GetUVMode(_uvModeBitPos, _uvModeFlagIndex) != first)
                {
                    return true;
                }
            }

            return false;
        }

        private void SetMode(NBShaderFlags.UVMode mode)
        {
            for (int i = 0; i < RootItem.ShaderFlags.Count; i++)
            {
                GetFlags(i)?.SetUVMode(mode, _uvModeBitPos, _uvModeFlagIndex);
            }
        }

        private NBShaderFlags GetFlags(int index)
        {
            return index >= 0 &&
                   index < RootItem.ShaderFlags.Count &&
                   RootItem.ShaderFlags[index] is NBShaderFlags flags
                ? flags
                : null;
        }

        private static void UpdateCylinderMatrix(ShaderGUIRootItem rootItem)
        {
            if (!rootItem.PropertyInfoDic.TryGetValue("_CylinderUVRotate", out ShaderPropertyInfo rotateInfo) ||
                !rootItem.PropertyInfoDic.TryGetValue("_CylinderUVPosOffset", out ShaderPropertyInfo offsetInfo))
            {
                return;
            }

            Matrix4x4 cylinderMatrix = Matrix4x4.Translate(offsetInfo.Property.vectorValue) *
                                       Matrix4x4.Rotate(Quaternion.Euler(rotateInfo.Property.vectorValue));
            SetVector(rootItem, "_CylinderMatrix0", cylinderMatrix.GetRow(0));
            SetVector(rootItem, "_CylinderMatrix1", cylinderMatrix.GetRow(1));
            SetVector(rootItem, "_CylinderMatrix2", cylinderMatrix.GetRow(2));
            SetVector(rootItem, "_CylinderMatrix3", cylinderMatrix.GetRow(3));
        }

        private static void SetVector(ShaderGUIRootItem rootItem, string propertyName, Vector4 value)
        {
            if (rootItem.PropertyInfoDic.TryGetValue(propertyName, out ShaderPropertyInfo info))
            {
                info.Property.vectorValue = value;
            }
        }

        private class SpecialUVChannelModeItem : ShaderGUIPopUpItem
        {
            public SpecialUVChannelModeItem(ShaderGUIRootItem rootItem, ShaderGUIItem parentItem) : base(rootItem, parentItem)
            {
                PropertyName = "_SpecialUVChannelMode";
                GuiContent = NBShaderInspectorLocalization.MakeInspectorContent("protocol.uv.specialChannel", "Special UV Channel");
                PopUpNames = NBShaderInspectorLocalization.GetInspectorOptions("protocol.uv.specialChannel", SpecialUVChannelNames);
                InitTriggerByChild();
            }

            public override void OnGUI()
            {
                GuiContent = NBShaderInspectorLocalization.MakeInspectorContent("protocol.uv.specialChannel", "Special UV Channel");
                PopUpNames = NBShaderInspectorLocalization.GetInspectorOptions("protocol.uv.specialChannel", SpecialUVChannelNames);
                base.OnGUI();
            }

            public override void OnEndChange()
            {
                base.OnEndChange();
                bool useTexcoord1 = Mathf.RoundToInt(PropertyInfo.Property.floatValue) == 0;
                for (int i = 0; i < RootItem.ShaderFlags.Count; i++)
                {
                    ShaderFlagsBase flags = RootItem.ShaderFlags[i];
                    if (useTexcoord1)
                    {
                        flags.SetFlagBits(NBShaderFlags.FLAG_BIT_PARTICLE_1_USE_TEXCOORD1, index: 1);
                        flags.ClearFlagBits(NBShaderFlags.FLAG_BIT_PARTICLE_1_USE_TEXCOORD2, index: 1);
                    }
                    else
                    {
                        flags.ClearFlagBits(NBShaderFlags.FLAG_BIT_PARTICLE_1_USE_TEXCOORD1, index: 1);
                        flags.SetFlagBits(NBShaderFlags.FLAG_BIT_PARTICLE_1_USE_TEXCOORD2, index: 1);
                    }
                }
            }
        }
    }

    public class KeywordListItem : ShaderGUIItem
    {
        private readonly Func<GUIContent> _contentProvider;

        public KeywordListItem(ShaderGUIRootItem rootItem, ShaderGUIItem parentItem, Func<GUIContent> contentProvider) : base(rootItem, parentItem)
        {
            _contentProvider = contentProvider ?? (() => GUIContent.none);
        }

        public override void OnGUI()
        {
            EditorGUILayout.Space();
            using (ParentControlDisabledScope())
            {
                EditorGUILayout.LabelField(_contentProvider(), EditorStyles.boldLabel);
            }

            if (RootItem.Mats == null || RootItem.Mats.Count == 0 || RootItem.Mats[0] == null)
            {
                return;
            }

            string[] keywords = RootItem.Mats[0].shaderKeywords;
            if (keywords == null || keywords.Length == 0)
            {
                using (ParentControlDisabledScope())
                {
                    EditorGUILayout.LabelField(NBShaderInspectorLocalization.GetInspectorText("common.none", "None"));
                }

                return;
            }

            for (int i = 0; i < keywords.Length; i++)
            {
                using (ParentControlDisabledScope())
                {
                    EditorGUILayout.LabelField(keywords[i]);
                }
            }
        }
    }
}
