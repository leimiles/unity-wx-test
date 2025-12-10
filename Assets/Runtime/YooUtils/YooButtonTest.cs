using UnityEngine;
using System.Collections;

public class YooButtonTest : MonoBehaviour
{
    [SerializeField] private string prefabAddress;
    public void TestLoadPrefab()
    {
        StartCoroutine(YooUtils.Instance.LoadAssetRoutine<GameObject>(
            prefabAddress,
            onSuccess: OnLoadSuccess,
            onFail: OnLoadFail
        ));
    }


    public void OnLoadSuccess(GameObject prefab)
    {
        Debug.Log($"成功加载: {prefab.name}");
    }

    public void OnLoadFail(string error)
    {
        Debug.LogError($"加载失败: {error}");
    }

}