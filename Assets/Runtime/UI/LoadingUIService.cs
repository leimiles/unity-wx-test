using UnityEngine;

public sealed class LoadingUIService
{
    static LoadingUIService _instance;
    public static LoadingUIService Instance => _instance ??= new LoadingUIService();

    static GameObject _prefab;
    static GameObject _instanceGO;

    const string kPath = "UI/Canvas_Loading";

    public void ShowLoadingUI()
    {
        if (_instanceGO != null) return;

        _prefab ??= Resources.Load<GameObject>(kPath);
        if (_prefab == null)
        {
            Debug.LogError($"Loading UI prefab not found: Resources/{kPath}.prefab");
            return;
        }

        _instanceGO = Object.Instantiate(_prefab);
        _instanceGO.name = "[BootstrapUI] Loading";
        GameObject.DontDestroyOnLoad(_instanceGO);
    }

    // 可选：给 BootstrapComplete 或外部显式退场用
    public void Hide()
    {
        if (_instanceGO == null) return;
        _instanceGO.SetActive(false);
    }

    public void Destroy()
    {
        if (_instanceGO == null) return;
        Object.Destroy(_instanceGO);
        _instanceGO = null;
    }

    public void Dispose()
    {
        Destroy();
    }
}
