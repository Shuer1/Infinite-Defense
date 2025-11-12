using UnityEngine;
using System.Collections;

public class Chest : MonoBehaviour
{
    [Header("宝箱对应的敌人类型")]
    public EnemyType enemyType;

    [Header("动画与音效")]
    public Animator chestAnimator;
    public AudioClip openSound;

    private bool isOpened = false;

    // 新：明确标识是由玩家主动点击打开（用于动画事件校验）
    private bool openedByPlayer = false;

    private void OnMouseDown()
    {
        if (isOpened) return;

        // 玩家主动点击打开宝箱
        isOpened = true;
        openedByPlayer = true;

        // 播放动画
        if (chestAnimator != null)
        {
            chestAnimator.SetTrigger("Open");
        }

        // 播放音效
        if (openSound != null)
        {
            AudioSource.PlayClipAtPoint(openSound, transform.position);
        }

        // 重要：不要在这里直接给奖励（或Invoke GiveReward），
        // 让动画事件在合适帧调用 GiveReward()，并由 GiveReward 做 guard 与 UI 通知。
    }

    // 已由动画事件调用（或测试时可手动调用）
    // 注意：不要从其他脚本/逻辑直接调用此方法（除非你知道自己在做什么）
    public void GiveReward()
    {
        // Guard：确保这个 GiveReward 是来源于玩家主动点击的开启流程
        if (!openedByPlayer)
        {
            Debug.LogWarning($"Chest.GiveReward() 被调用，但 openedByPlayer==false（宝箱: {gameObject.name}）。拒绝发放。");
            return;
        }

        // 清理标志，防止重复调用路径再次发放
        openedByPlayer = false;

        // 正确行为：通知 UIManager 来显示首杀领取面板，由玩家确认后由 UIManager 发放奖励
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowFirstKillPanel(enemyType);
            Debug.Log($"Chest: 已通知 UIManager 显示首杀面板（敌人类型：{enemyType}）。等待玩家确认发放。");
        }
        else
        {
            Debug.LogError("Chest.GiveReward: UIManager.Instance 为 null，无法显示首杀面板。请检查 UIManager 是否已初始化。");
            // 如果 UIManager 不存在，出于安全可以选择临时不发放，或作为回退直接发放（不推荐）
            // FirstKillRewardManager.Instance?.GrantFirstKillReward(enemyType); // <- 不建议自动回退
        }

        // 动画播放完毕后安全销毁宝箱（使用 realtime 延迟，避免受 Time.timeScale 影响）
        StartCoroutine(DestroyAfterRealtime(1f));
    }

    private IEnumerator DestroyAfterRealtime(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime; // 使用 unscaledTime 避开 Time.timeScale
            yield return null;
        }

        Destroy(gameObject);
    }
}
