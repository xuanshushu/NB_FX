# NBShaderGUI Rule

## Purpose

This rule defines the structure constraints for `NBShaders2/Editor/NBShaderGUI` and shared `ShaderGUIItems`.

## Core Principles

- Keep the GUI structure minimal and explicit.
- Do not add wrapper abstractions without a clear responsibility.
- `FoldOut` behavior and `parent-child composition` are two different concerns and must not be mixed.
- Common drawing items live in `XuanXuanRenderUtility/Editor/ShaderGUIItems`.
- Shared item names must not use the `NBShader` prefix.

## Shared Item Rules

All common material-property drawing items must live in `XuanXuanRenderUtility/Editor/ShaderGUIItems`, not in `NBShaders2/Editor/ShaderGUIItems`.

Use shared items for common material property drawing:

- `ToggleItem`
- `ColorItem`
- `TextureItem`
- `Vector2LineItem`
- `VectorComponentItem`
- `HelpBoxItem`

Vector-related shared item classes should stay in one `VectorItem.cs` file.

Do not create NBShader-prefixed wrappers for common drawing behavior. These names are not allowed for shared/general controls:

- `NBShaderTogglePropertyItem`
- `NBShaderColorPropertyItem`
- `NBShaderTexturePropertyItem`
- `NBShaderVector2LinePropertyItem`
- `NBShaderVectorComponentPropertyItem`
- `NBShaderHelpBoxItem`

Do not create wrappers that only set `PropertyName`, `GuiContent`, ranges, or visibility around an existing shared item. Prefer using the shared item directly, or create a concrete business item when there is real shader-specific behavior.

Pure configuration items should be instantiated directly inside the owning block constructor.

Use direct shared item construction for cases that only assign:

- `PropertyName`
- `GuiContent` or `GUIContent` provider
- `RangePropertyName`
- `Min` / `Max`
- simple visibility predicates

Example pattern:

```csharp
_alphaAllItem = new ShaderGUISliderItem(rootItem, this)
{
    PropertyName = "_AlphaAll",
    GuiContent = NBShaderInspectorLocalization.MakeInspectorContent("base.alphaAll", "Overall Alpha"),
    RangePropertyName = "AlphaAllRangeVec"
};
_alphaAllItem.InitTriggerByChild();
```

Do not create a named class for this pattern, such as `AlphaAllItem`, unless it adds real behavior.

A concrete item class is justified only when it owns shader-specific behavior, such as:

- overriding `OnGUI`, `DrawBlock`, `OnEndChange`, `CheckIsPropertyModified`, or `ExecuteReset`
- synchronizing keywords, render queue, blend state, pass state, runtime flags, or context
- implementing a reusable layout/control behavior that belongs in shared `ShaderGUIItems`

Current examples:

- `ZTestItem` is allowed because it forces UIEffect depth behavior.
- `AddToPreMultiplySlider` is allowed because its reset default depends on `BlendMode`.
- Popup items are allowed when they update `Context`, keywords, render state, or child visibility.
- `BaseColorIntensityItem`, `AlphaAllItem`, and `CutOffSlider` are not allowed as named wrappers because they only configure shared float/slider items.

Shared items must not reference:

- `NBShaderRootItem`
- `NBShaderInspectorLocalization`
- NBShader-specific keyword, pass, or flag logic

NBShader-specific labels should be passed into shared items through `GUIContent` providers from the call site.

## Localization Rules

NBShader2 Inspector user-facing text must go through `NBShaderInspectorLocalization`.

- Use CSV keys in `NBShaders2/Editor/Localization/NBShaderInspectorLocalization.csv`.
- CSV format is `key,zh-CN,en-US,zh-CN-tip,en-US-tip`.
- Tooltip text belongs to the same row as its label key. If a row has no tooltip, leave the tip columns empty.
- Legacy `.tip` rows are only compatibility fallback for existing non-label message reads. New label tooltips must not create a separate `.tip` key.
- Language selection is editor-wide and stored in `EditorPrefs` key `NBShader2.Localization.Language`.
- Language menus live under `Tools/NBShader2/Language/中文` and `Tools/NBShader2/Language/English`.
- CSV edits can be reloaded through `Tools/NBShader2/Language/Reload Localization`.

Fallback order:

- current language
- `zh-CN`
- code fallback

Key conventions:

- normal labels use `inspector.<block>.<feature>.<name>.label`
- label tooltips use the same key as the label and read the `<language>-tip` column
- standalone tooltip/help text that is not attached to a `GUIContent` should use `.message` or another explicit text key
- pure messages use `inspector.<block>.<feature>.<name>.message`
- buttons use `inspector.<block>.<feature>.<name>.button`
- popup options use `inspector.<block>.<feature>.<name>.option.<index>`

Code conventions:

- Normal controls should call `NBShaderInspectorLocalization.MakeInspectorContent(key, fallback, tip)` or pass a provider that calls it. The helper reads the label from `inspector.<key>.label` and the tooltip from the same CSV row first.
- HelpBox/message strings should call `NBShaderInspectorLocalization.GetInspectorText(key, fallback)`.
- Popup options should call `NBShaderInspectorLocalization.GetInspectorOptions(key, fallbackArray)` or update `PopUpNames` from an options provider.
- Shared `XuanXuanRenderUtility/Editor/ShaderGUIItems` must not reference `NBShaderInspectorLocalization`.
- Shared items that draw visible labels should expose `GUIContent` providers; NBShader2 call sites provide localized content.
- Technical identifiers stay untranslated unless they are deliberately shown as UI copy: shader property names, keyword names, pass names, enum protocol values, render queue numbers, and stencil config keys remain stable.

## NBShaders2 Item Rules

`NBShaders2/Editor/ShaderGUIItems` should only contain NBShader business structure:

- top-level blocks
- secondary business blocks
- shader-specific popup items with side effects
- composite layout items such as the main texture layout
- feature-specific items that synchronize NBShader keywords, pass state, or runtime flags

NBShader-specific item names are acceptable only when the item has NBShader business meaning. They must not be used for generic drawing controls.

## Inheritance Rules

### 1. `ShaderGUIItem`

`ShaderGUIItem` is the base class for normal GUI items.

Use it for:

- leaf controls
- composite layout items
- parent-child grouping items without foldout
- texture layout parent items

Important:

- parent-child composition already belongs to `ShaderGUIItem`
- do not introduce extra “control base class” layers just to express parent-child grouping

### 2. `BlockItem`

`BlockItem` exists only to provide `FoldOut` behavior.

Responsibilities:

- owns `FoldOutPropertyName`
- owns foldout open/close lifecycle
- draws foldout title row
- draws children inside foldout content area

It must not be used for:

- non-foldout layout items
- generic feature/control wrappers

### 3. `BigBlockItem`

`BigBlockItem` inherits from `BlockItem`.

Responsibilities:

- defines the visual style of top-level blocks
- bold title
- spacing
- separator line

`BigBlockItem` is a style specialization of `BlockItem`, not a different composition system.

## Top-Level Block Rules

These top-level sections should inherit `BigBlockItem`:

- `ModeBigBlockItem`
- `BaseOptionBigBlockItem`
- `MainTexBigBlockItem`
- `LightBigBlockItem`
- `FeatureBigBlockItem`
- `TABigBlockItem`

These are first-level inspector sections and should use persisted foldout state.

## Secondary Block Rules

Only use `BlockItem` for a secondary section when:

- it has its own foldout state
- it is semantically a collapsible section
- it contains a group of child items

If a section is only doing layout and grouping, it must stay on `ShaderGUIItem`.

## Layout Rules

All normal inspector row layout must be owned by `ShaderGUIItem`.

Do not calculate label/control/reset positions independently in leaf items. Use the base rect pipeline:

- `GetRect()`
- `ApplyGlobalRectCompensation()`
- `SplitLineRect()`
- `SplitControlAndResetRect()`

Standard row responsibilities:

- `BaseRect` is the compensated full row rect.
- `LabelRect` uses `GetLabelWidth(BaseRect)`.
- `GetLabelWidth()` has a fixed minimum (`MinLabelWidth`) and then grows by `LabelWidthRatio` once the inspector row is wide enough.
- Do not read or hardcode a fixed label width in business items. If a custom layout needs the normal label/control split, call `SplitLineRect()`.
- `ControlRect` starts after `LabelRect` and reserves reset space.
- `ResetRect` is fixed to the right side and uses `ResetButtonSize`.
- `ControlResetGap` is the only standard gap between control and reset.

Do not add local magic numbers for reset width, reset gap, or standard control expansion in business items. If a layout constant is needed by more than one item, add it to `ShaderGUIItem`.

Global row compensation:

- `GlobalRectXOffset` shifts the whole row left/right.
- `GlobalRectWidthExpansion` expands the whole row width.
- These constants compensate meaningless inspector-side margin and must be applied at the rect entry point, before splitting label/control/reset.

Any item that calls `EditorGUILayout.GetControlRect()` directly must immediately pass the result through `ApplyGlobalRectCompensation()`, unless there is a specific documented reason not to.

Editor indent rules:

- `EditorGUI.indentLevel` is only a hierarchy level counter.
- Do not introduce a separate per-block indent-count constant.
- Unity's internal per-level indent width is represented by `UnityEditorGUIIndentWidth`.
- The desired NBShader GUI visual indent width is represented by `EditorGUIIndentWidth`.
- `ApplyLabelIndentWidth()` converts Unity's built-in `EditorGUI.LabelField` indent width into the desired visual indent width for the NBShader GUI rect system.
- Direct `GUI.Label`, `GUI.Toggle`, texture preview rects, and other non-`EditorGUI.LabelField` labels do not get Unity's built-in label indent. Use `ApplyDirectLabelIndentWidth()` for their label start.
- Foldout arrows must be positioned from the final visual label text x, not from `BaseRect.x`. Use `ShaderGUIFoldOutHelper` / `GetEditorLabelTextX()` and do not call `EditorGUI.Foldout()` directly in business items.
- Do not hardcode a local texture or foldout indent width. Three-line texture groups must use `ApplyDirectLabelIndentWidth()` for the texture preview left edge and `SplitLineRect()` for Tilling/Offset control/reset positions.

Control indent compensation:

- `ControlIndentCompensation` is not the same thing as row indent.
- It only adjusts `ControlRect` by moving `x` left and increasing `width`.
- Use it only to compensate controls whose Unity internal drawing is shifted right inside the passed rect.
- Do not apply it to controls that draw exactly inside the passed rect, such as single-line `ColorField`; call `GetRect(false)` or `SplitLineRect(..., false)` for those rows.
- `EditorGUI.ColorField` draws the visible swatch through `EditorStyles.colorField.padding.Remove(position)`. Labeled color rows may compensate this padding inside `ColorItem`; no-label color rows keep their own local inset. Do not add Color-only constants to `ShaderGUIItem`.
- Keep `LabelRect` and `ResetRect` independent from this compensation.
- Composite layouts may disable it with `applyControlIndentCompensation: false` and then apply a local compensation only to the actual control rect when needed.

Full-width or no-label rows:

- Rows such as full-width color bars should not automatically use label/control semantics.
- If a row has no label, call `SplitControlAndResetRect()` directly and disable control compensation when the row must preserve its left edge.
- Reset alignment should still come from `SplitControlAndResetRect()`.

Texture rows:

- All texture object inputs should use the shared three-row texture layout (`TexturePropertyGroupItem` through `TextureItem`) instead of `MaterialEditor.TexturePropertySingleLine()`.
- Texture rows without editable Tilling/Offset should still reserve the three-row texture object area and leave the Tilling/Offset rows empty.
- Texture color rows remain separate no-label color rows after the three-row texture object group.

## Animated Property Rules

Animated property highlight must follow Unity's native material inspector behavior.

Do not manually set animated controls to a fixed color such as `Color.red`.

Use the shared `ShaderGUIItem` animated scope helpers:

- `BeginAnimatedPropertyBackground(Rect totalPosition, MaterialProperty property)`
- `EndAnimatedPropertyBackground(bool scopeActive)`

These helpers must use `MaterialEditor.BeginAnimatedCheck()` and `MaterialEditor.EndAnimatedCheck()`. They must not implement their own color decision with `AnimationMode.IsPropertyAnimated()`.

Reason:

- Unity's original `MaterialEditor.ShaderProperty()` wraps controls with `BeginAnimatedCheck()` / `EndAnimatedCheck()`.
- Unity chooses between animated, recorded, and candidate colors internally.
- Unity restores the previous `GUI.backgroundColor` through a stack, so nested drawing does not leak color state.

Standard item rule:

- Normal leaf items that use `ShaderGUIItem.OnGUI()` should rely on the base animated scope around `DrawController()`.

Custom `OnGUI()` rule:

- If an item overrides `OnGUI()` or draws multiple independent controls manually, it must explicitly wrap each actual editable control with the correct `MaterialProperty`.
- Do not wrap a composite row with one property if the row contains controls backed by different properties.
- Always call `EndAnimatedPropertyBackground()` in the same draw path after the control is drawn.

Multi-property control rule:

- `ShaderGUISliderItem` with `RangePropertyName` must wrap the min/max fields with the range vector property and the slider with the main float property.
- Texture scale/offset controls must wrap their `Vector2Field` controls with the texture property when editing `textureScaleAndOffset`, or with the vector property when editing a vector ST property.

Animation refresh rule:

- `ShaderGUIRootItem` must update incoming `MaterialProperty[]` into `PropertyInfoDic` every `OnGUI`.
- In animation mode, the material inspector should repaint so animated values and highlight state refresh while scrubbing or recording.
- Do not cache animated-state booleans. Caching property path strings is allowed for diagnostics or non-background queries.

## Texture GUI Rules

For texture-related GUI:

- the texture layout parent item should inherit `ShaderGUIItem`
- it is a layout/composition item, not a `BlockItem`
- texture object field, texture color, and texture `Tilling/Offset` are separate GUI items
- the parent texture layout item is responsible only for arranging these child items
- texture layout must not move label text as a side effect of control compensation
- for `Tilling/Offset`, calculate label rects from the uncompensated vector rect first, then apply control compensation only to the `Vector2Field` rect if visual control alignment requires it
- texture, texture label, texture color, `Tilling/Offset`, and reset buttons must still use shared `ShaderGUIItem` layout constants where applicable
- texture ST animated highlight must use Unity's texture `MaterialProperty` animated check, not manual `_ST.x/y/z/w` color logic

Required structure:

- texture parent layout item
  - texture object field item
  - texture color item
  - texture `Tilling/Offset` item

## Anti-Rules

Do not introduce these kinds of abstractions again unless there is a concrete need:

- generic `ControlItem` base classes with no real behavior
- generic `FeatureItem` base classes with no real semantics
- wrapper layers that only rename existing base classes

If an abstraction does not define a stable responsibility boundary, do not keep it.
