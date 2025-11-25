using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[DisallowMultipleComponent]
public class PressureMaker : MonoBehaviour
{
    IEnumerator Start()
    {
        while (true)
        {
            var rt = new RenderTexture(256, 256, 0, RenderTextureFormat.ARGB32);
            rt.Create();

            yield return new WaitForSeconds(0.1f);

            rt.Release();
            Destroy(rt);

            yield return null;
        }
    }
}
