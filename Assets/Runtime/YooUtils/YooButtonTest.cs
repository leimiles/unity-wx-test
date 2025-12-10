using UnityEngine;
using System.Collections;

public class QuickTest : MonoBehaviour
{
    public void TestLoadSuit210()
    {
        StartCoroutine(YooUtils.Instance.LoadAssetRoutine<GameObject>(
            "JiJiGuoWang@Suit_580",
            onSuccess: (prefab) => Debug.Log($"成功加载: {prefab.name}"),
            onFail: (error) => Debug.LogError($"加载失败: {error}")
        ));
    }
}