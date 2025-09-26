using System;
using System.Reflection;
using UnityEngine;

namespace NBShaderEditor
{
//因为GradientPicker窗口，在窗口打开时使用的是Gradient对象的Cache。
//如果有Undo操作，导致MaterialProperty更新了，但Gradient却更新不到ColorPicer里。所以在这里进行强制更新。
//后续需要强制更新Picker的时候，也可以用这里。
//DeepSeek写的。谨慎用。
    public static class GradientReflectionHelper
    {
        #region GradientPicker 相关

        private static Type _gradientPickerType;

        // SetCurrentGradient 方法
        private static MethodInfo _setGradientMethod;
        private static Action<Gradient> _setGradientDelegate;

        // RefreshGradientData 方法
        private static MethodInfo _refreshMethod;
        private static Action _refreshDelegate;

        #endregion

        #region GradientPreviewCache 相关

        private static Type _gradientCacheType;
        private static MethodInfo _clearCacheMethod;
        private static Action _clearCacheDelegate;

        #endregion

        // 初始化状态
        private static bool _initialized;
        private static bool _isValid;
        private static readonly object _initLock = new object();

        #region 公共API

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void SetCurrentGradient(Gradient gradient)
        {
            ExecuteAction(() =>
            {
                if (_setGradientDelegate != null)
                    _setGradientDelegate(gradient);
                else if (_setGradientMethod != null)
                    _setGradientMethod.Invoke(null, new object[] { gradient });
            }, "SetCurrentGradient");
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void RefreshGradientData()
        {
            ExecuteAction(() =>
            {
                if (_refreshDelegate != null)
                    _refreshDelegate();
                else if (_refreshMethod != null)
                    _refreshMethod.Invoke(null, null);
            }, "RefreshGradientData");
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void ClearGradientCache()
        {
            ExecuteAction(() =>
            {
                if (_clearCacheDelegate != null)
                    _clearCacheDelegate();
                else if (_clearCacheMethod != null)
                    _clearCacheMethod.Invoke(null, null);
            }, "ClearGradientCache");
        }

        #endregion

        #region 核心实现

        private static void ExecuteAction(Action action, string methodName)
        {
            if (!Application.isEditor) return;

            EnsureInitialized();
            if (!_isValid) return;

            try
            {
                action?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GradientReflection] {methodName} failed: {ex.Message}");
                // 可选择性地禁用后续调用
                // _isValid = false;
            }
        }

        private static void EnsureInitialized()
        {
            if (_initialized) return;

            lock (_initLock)
            {
                if (_initialized) return;

                try
                {
                    // 1. 初始化 GradientPicker 类型
                    _gradientPickerType = Type.GetType("UnityEditor.GradientPicker, UnityEditor");
                    if (_gradientPickerType != null)
                    {
                        // 初始化 SetCurrentGradient
                        _setGradientMethod = GetStaticMethod(
                            _gradientPickerType,
                            "SetCurrentGradient",
                            new[] { typeof(Gradient) }
                        );
                        if (_setGradientMethod != null)
                        {
                            _setGradientDelegate = CreateActionDelegate<Gradient>(_setGradientMethod);
                        }

                        // 初始化 RefreshGradientData
                        _refreshMethod = GetStaticMethod(
                            _gradientPickerType,
                            "RefreshGradientData",
                            Type.EmptyTypes
                        );
                        if (_refreshMethod != null)
                        {
                            _refreshDelegate = CreateActionDelegate(_refreshMethod);
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[GradientReflection] GradientPicker type not found");
                    }

                    // 2. 初始化 GradientPreviewCache
                    _gradientCacheType = Type.GetType("UnityEditorInternal.GradientPreviewCache, UnityEditor");
                    if (_gradientCacheType != null)
                    {
                        _clearCacheMethod = GetStaticMethod(
                            _gradientCacheType,
                            "ClearCache",
                            Type.EmptyTypes
                        );
                        if (_clearCacheMethod != null)
                        {
                            _clearCacheDelegate = CreateActionDelegate(_clearCacheMethod);
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[GradientReflection] GradientPreviewCache type not found");
                    }

                    // 3. 验证初始化状态
                    _isValid = (_setGradientMethod != null || _refreshMethod != null) &&
                               _clearCacheMethod != null;

                    if (!_isValid)
                    {
                        Debug.LogWarning($"[GradientReflection] Initialization incomplete. " +
                                         $"SetCurrentGradient: {(_setGradientMethod != null ? "OK" : "Missing")}, " +
                                         $"RefreshGradientData: {(_refreshMethod != null ? "OK" : "Missing")}, " +
                                         $"ClearCache: {(_clearCacheMethod != null ? "OK" : "Missing")}");
                    }
                    else
                    {
                        // Debug.Log("[GradientReflection] Initialized successfully");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[GradientReflection] Initialization failed: {e}");
                    _isValid = false;
                }
                finally
                {
                    _initialized = true;
                }
            }
        }

        private static MethodInfo GetStaticMethod(Type type, string methodName, Type[] parameterTypes)
        {
            if (type == null) return null;

            return type.GetMethod(
                methodName,
                BindingFlags.Public | BindingFlags.Static,
                null,
                parameterTypes,
                null
            );
        }

        private static Action CreateActionDelegate(MethodInfo method)
        {
            try
            {
                return (Action)Delegate.CreateDelegate(typeof(Action), method);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GradientReflection] Failed to create delegate for {method.Name}: {ex.Message}");
                return null;
            }
        }

        private static Action<T> CreateActionDelegate<T>(MethodInfo method)
        {
            try
            {
                return (Action<T>)Delegate.CreateDelegate(typeof(Action<T>), method);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GradientReflection] Failed to create delegate for {method.Name}: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region 辅助功能

        /// <summary>
        /// 检查是否所有反射方法都可用
        /// </summary>
        public static bool IsFullySupported => _isValid;

        /// <summary>
        /// 获取反射初始化状态
        /// </summary>
        public static string GetReflectionStatus()
        {
            if (!_initialized) return "Not Initialized";

            return $"GradientPicker: {(_gradientPickerType != null ? "Found" : "Missing")}\n" +
                   $"- SetCurrentGradient: {(_setGradientMethod != null ? "OK" : "Missing")}\n" +
                   $"- RefreshGradientData: {(_refreshMethod != null ? "OK" : "Missing")}\n" +
                   $"GradientPreviewCache: {(_gradientCacheType != null ? "Found" : "Missing")}\n" +
                   $"- ClearCache: {(_clearCacheMethod != null ? "OK" : "Missing")}";
        }

        #endregion
    }
}