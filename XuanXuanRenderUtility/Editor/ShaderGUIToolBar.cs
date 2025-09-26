
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using NBShader;
namespace NBShaderEditor
{

    public class ShaderGUIToolBar
    {
        public ShaderGUIHelper Helper;

        private int viewModeIndex;
        private readonly string[] viewModes = { "List", "Grid" };
        // private string searchText = "";
        private MaterialEditor _editor => Helper.matEditor;
        public ShaderGUIToolBar(ShaderGUIHelper helper)
        {
            Helper = helper;
        }

        private static Material copiedMaterial;
        private static Shader copiedShader;
        
        // 帮助链接URL
        private const string HELP_URL = "https://owejt9diz2c.feishu.cn/wiki/BHz8wHHSjiYJagk7WrmcAcconlb?from=from_copylink";
        
        // private Vector2 imagePos;
        // private Texture2D icon;
        // private Texture2D image;

        public void DrawToolbar()
        {

            float BtnWidth = 30f;
            // 开始工具栏区域 (背景)
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        
            // 1. 选择当前材质
            if (GUILayout.Button(EditorGUIUtility.IconContent("Material On Icon","跳到当前材质|跳到当前材质"), EditorStyles.toolbarButton,GUILayout.Width(BtnWidth)))
            {
                EditorGUIUtility.PingObject(Helper.mats[0]);
            }
            
            if (GUILayout.Button(EditorGUIUtility.IconContent("TreeEditor.Trash","清除没有使用的贴图|清除没有使用的贴图"), EditorStyles.toolbarButton,GUILayout.Width(BtnWidth)))
            {
                foreach (var mat in Helper.mats)
                {
                    CleanUnusedTextureProperties(Helper.mats[0]);//先清理不属于当前Shader的贴图
                }
                Helper.isClearUnUsedTexture = true;
            }
            
            if (GUILayout.Button(new GUIContent("C","复制材质属性"), EditorStyles.toolbarButton,GUILayout.Width(BtnWidth)))
            {
                copiedMaterial = Helper.mats[0];
                copiedShader = copiedMaterial.shader;
            }
            
            if (GUILayout.Button(new GUIContent("V","粘贴材质属性"), EditorStyles.toolbarButton,GUILayout.Width(BtnWidth)))
            {
                if (copiedShader)
                {
                    Helper.mats[0].shader = copiedShader;
                }

                if (copiedMaterial)
                {
                    Helper.mats[0].CopyPropertiesFromMaterial(copiedMaterial);
                }
            }
            if (GUILayout.Button(new GUIContent("R","特殊重置功能"), EditorStyles.toolbarButton,GUILayout.Width(BtnWidth)))
            {
                ShowResetPopupMenu();
            }
            if (GUILayout.Button(EditorGUIUtility.IconContent("d_UnityEditor.HierarchyWindow","折叠所有控件|折叠所有控件"), EditorStyles.toolbarButton,GUILayout.Width(BtnWidth)))
            {
                for (int i = 0;i<Helper.shaderFlags.Length;i++)
                {
                    W9ParticleShaderFlags shaderFlags = (W9ParticleShaderFlags)Helper.shaderFlags[i];
                    for (int j = 3; j <= 5; j++)
                    {
                        Helper.mats[i].SetInteger(shaderFlags.GetShaderFlagsId(j),0);
                    }
                }
            }

            // 2. 添加下拉菜单
            // viewModeIndex = EditorGUILayout.Popup(viewModeIndex, viewModes, EditorStyles.toolbarPopup, GUILayout.Width(BtnWidth));

            // 3. 添加搜索框
            GUILayout.FlexibleSpace(); // 将搜索框推到中间
        
            // // 搜索框样式
            // GUIStyle searchField = new GUIStyle("SearchTextField");
            // GUIStyle cancelButton = new GUIStyle("SearchCancelButton");
            //
            // EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(300));
            // {
            //     EditorGUI.BeginChangeCheck();
            //     searchText = EditorGUILayout.TextField(searchText, searchField);
            //     if (EditorGUI.EndChangeCheck())
            //     {
            //         Helper.isSearchText = searchText.Length > 0;
            //         Helper.searchText = searchText;
            //     }
            //     
            //     // 清除搜索按钮
            //     if (GUILayout.Button("", cancelButton))
            //     {
            //         searchText = "";
            //         GUI.FocusControl(null); // 移除焦点
            //     }
            // }
            // EditorGUILayout.EndHorizontal();

            // // 4. 右侧按钮组
            // if (GUILayout.Button(EditorGUIUtility.IconContent("TreeEditor.Refresh"), EditorStyles.toolbarButton))
            // {
            //     Debug.Log("Refresh clicked");
            // }
            //
            // // 选项菜单
            // if (GUILayout.Button(EditorGUIUtility.IconContent("_Popup"), EditorStyles.toolbarButton))
            // {
            //     // 创建下拉菜单
            //     GenericMenu menu = new GenericMenu();
            //     menu.AddItem(new GUIContent("Option 1"), false, () => Debug.Log("Option 1"));
            //     menu.AddItem(new GUIContent("Option 2"), false, () => Debug.Log("Option 2"));
            //     menu.ShowAsContext();
            // }
            // 贴图加载
            // if (icon == null)
            //     icon = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath("eaa39f504c2ce7646aece103ba9c4766"));
            // if (image == null)
            //     image = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath("cc6c30349a33a1d4c8242a9ef1a68830"));
            // if (GUILayout.Button(new GUIContent(icon,"爸爸！"), EditorStyles.toolbarButton,GUILayout.Width(BtnWidth)))
            // {
            //     // 弹出 PopupWindow
            //     // 弹出浮动窗口
            //     FloatingImageWindow.ShowWindow(image);
            // }
            
            
            if (GUILayout.Button(EditorGUIUtility.IconContent("d__Help@2x","说明文档|说明文档"), EditorStyles.toolbarButton,GUILayout.Width(BtnWidth)))
            {
                // 打开浏览器跳转到帮助链接
                Application.OpenURL(HELP_URL);
            }

            EditorGUILayout.EndHorizontal(); // 结束工具栏
        }
        
        private void ShowResetPopupMenu()
        {
            GenericMenu menu = new GenericMenu();

            menu.AddItem(new GUIContent("重置特殊UV通道"), false, () =>
            {
                Helper.ResetTool.ResetItemDict[("特殊UV通道选择","_SpecialUVChannelMode")].Execute();
            });
            menu.AddItem(new GUIContent("重置旋转扭曲"), false, () =>
            {
                Helper.ResetTool.ResetItemDict[("","_UTwirlEnabled")].Execute();
            });
            menu.AddItem(new GUIContent("重置极坐标"), false, () =>
            {
                Helper.ResetTool.ResetItemDict[("","_PolarCoordinatesEnabled")].Execute();
            });

            // 弹出位置可以用 Event.current.mousePosition
            menu.DropDown(new Rect(Event.current.mousePosition, Vector2.zero));
        }
        
        private void CleanUnusedTextureProperties(Material mat)
        {
            if (mat == null || mat.shader == null) return;

            Shader shader = mat.shader;

            // 收集 Shader 里声明过的贴图属性
            var shaderTexProps = new HashSet<string>();
            int count = ShaderUtil.GetPropertyCount(shader);
            for (int i = 0; i < count; i++)
            {
                if (ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
                {
                    shaderTexProps.Add(ShaderUtil.GetPropertyName(shader, i));
                }
            }

            // 遍历材质所有贴图属性，找到 shader 不再声明的
            var allProps = mat.GetTexturePropertyNames();
            foreach (var propName in allProps)
            {
                if (!shaderTexProps.Contains(propName))
                {
                    if (mat.GetTexture(propName) != null)
                    {
                        mat.SetTexture(propName, null);
                        Debug.Log($"清理 {mat.name} 的无效贴图属性: {propName}");
                    }
                }
            }
        }
        
        // EditorWindow 显示图片
        public class FloatingImageWindow : EditorWindow
        {
            private Texture2D popupImage;

            public static void ShowWindow(Texture2D image)
            {
                // 创建窗口
                FloatingImageWindow window = CreateInstance<FloatingImageWindow>();
                window.titleContent = new GUIContent("谢谢爸爸");
                window.popupImage = image;

                // 设置初始尺寸
                if (image != null)
                    window.position = new Rect(Screen.width / 2f - image.width / 2f,
                        Screen.height / 2f - image.height / 2f,
                        image.width,
                        image.height);
                else
                    window.position = new Rect(Screen.width / 2f - 100, Screen.height / 2f - 100, 200, 200);

                window.ShowUtility(); // 浮动窗口
            }

            private void OnGUI()
            {
                if (popupImage != null)
                {
                    // 绘制图片
                    Rect rect = new Rect(0, 0, position.width, position.height);
                    GUI.DrawTexture(rect, popupImage, ScaleMode.ScaleToFit);
                }

                // 可选：增加关闭按钮
                if (GUI.Button(new Rect(position.width - 25, 5, 20, 20), "X"))
                {
                    Close();
                }
            }
        }

    }
}