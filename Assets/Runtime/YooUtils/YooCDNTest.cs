using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
public class YooCDNTest : MonoBehaviour
{
    [SerializeField] private string prefabAddress;
    GameObject instance = null;
    public void Load()
    {
        StartCoroutine(YooUtils.Instance.LoadAssetRoutine<GameObject>(
            prefabAddress,
            onSuccess: OnLoadSuccess,
            onFail: OnLoadFail
        ));
    }


    void OnLoadSuccess(GameObject prefab)
    {
        instance = prefab;
        Debug.Log($"成功加载: {instance.name}");
    }

    void OnLoadFail(string error)
    {
        Debug.LogError($"加载失败: {error}");
    }

    public void Spawn()
    {
        if (instance == null)
        {
            Debug.LogError("实例为空");
            return;
        }
        instance = Instantiate(instance, transform);
    }

    public void Unload()
    {
        Destroy(instance);
        YooUtils.Instance.ReleaseAsset(prefabAddress);
        instance = null;
    }

}