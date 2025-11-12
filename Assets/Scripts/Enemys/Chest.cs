using UnityEngine;

public class Chest : MonoBehaviour
{
    [Header("宝箱对应的敌人类型")]
    public EnemyType enemyType;

    [Header("动画与音效")]
    public Animator chestAnimator;
    public AudioClip openSound;

    private bool isOpened = false;

    private void OnMouseDown()
    {
        if (isOpened) return;
        isOpened = true;

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

        // 延时奖励触发（给动画留出时间）-- 已使用动画事件替代触发
        //Invoke(nameof(GiveReward), 0.6f);
    }

    private void GiveReward()
    {
        if (FirstKillRewardManager.Instance != null)
        {
            FirstKillRewardManager.Instance.GrantFirstKillReward(enemyType);
        }

        // 播放完动画后销毁
        Destroy(gameObject, 1f);
    }
}
