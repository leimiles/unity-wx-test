using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class LoadingUI : MonoBehaviour
{
    // 暴露参数：进度、消息
    [SerializeField] TextMeshProUGUI _progressText;
    [SerializeField] Slider _progressSlider;
    [SerializeField] Image _progressBackgroundImage;

    /// <summary>
    /// 设置进度，范围为 0-1
    /// </summary>
    /// <param name="progress"></param>
    public void SetProgress(float progress)
    {
        _progressText.text = $"{progress * 100:F1}%";
        _progressSlider.value = progress * 100;
    }
}
