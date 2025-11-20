using UnityEngine;
using UnityEngine.UI;

public class ImageSlider : MonoBehaviour
{
    // 显示当前轮播图的 Image 组件（拖入场景中的 SliderImage）
    public Image sliderImage;
    // 轮播图片数组（拖入准备好的 Sprite 图片）
    public Sprite[] sliderSprites;
    // 当前显示的图片索引（默认从第 0 张开始）
    private int currentIndex = 0;

    void Start()
    {
        // 初始化：显示第一张图片
        if (sliderSprites != null && sliderSprites.Length > 0)
        {
            sliderImage.sprite = sliderSprites[currentIndex];
        }
    }

    // 左按钮点击事件：切换到上一张（循环）
    public void LeftButtonClick()
    {
        // 索引减 1，若小于 0 则切换到最后一张
        currentIndex = (currentIndex - 1 + sliderSprites.Length) % sliderSprites.Length;
        UpdateSliderImage();
    }

    // 右按钮点击事件：切换到下一张（循环）
    public void RightButtonClick()
    {
        // 索引加 1，若大于数组长度则切换到第一张
        currentIndex = (currentIndex + 1) % sliderSprites.Length;
        UpdateSliderImage();
    }

    // 更新显示当前索引的图片
    private void UpdateSliderImage()
    {
        if (sliderSprites != null && sliderSprites.Length > 0)
        {
            sliderImage.sprite = sliderSprites[currentIndex];
        }
    }
}