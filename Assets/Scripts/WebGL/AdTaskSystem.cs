using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_WEBGL || UNITY_EDITOR
public class AdTaskSystem : MonoBehaviour
{
    public static AdTaskSystem Instance;

    [Header("每日最大计次")]
    public int dailyMax = 10;

    private List<TaskData> tasks = new List<TaskData>();
    private const string SAVE_KEY = "ad_task_save";

    [System.Serializable]
    private class TaskData
    {
        public string id;           // 任务编号
        public int goal;            // 要求次数
        public int progress;        // 已看次数
        public long lastTs;         // 最后一次完成时间（Unix秒）
        public bool rewarded;       // 大奖是否已领
    }

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else Destroy(gameObject);
        Load();
    }

    // 广告看完立即调
    public void OnRewardedFinished(bool success)
    {
        if (!success) return;
        long now = DateTimeOffset.Now.ToUnixTimeSeconds();
        // 简单防刷：两次广告间隔 <30s 不计
        if (tasks.Count > 0 && now - tasks[tasks.Count - 1].lastTs < 30) return;
        // 每日封顶
        int todayCount = 0;
        foreach (var t in tasks) if (IsToday(t.lastTs)) todayCount += t.progress;
        if (todayCount >= dailyMax) return;

        // 默认只维护一条“每日看广告”任务
        if (tasks.Count == 0) tasks.Add(NewDailyTask());
        var cur = tasks[0];
        if (!IsToday(cur.lastTs)) ResetDaily(cur); // 跨天重置
        cur.progress++;
        cur.lastTs = now;
        if (cur.progress >= cur.goal && !cur.rewarded)
        {
            cur.rewarded = true;
            GiveReward(cur.id);
        }
        Save();
    }

    private TaskData NewDailyTask() => new TaskData
    { id = "daily_ad", goal = 3, progress = 0, lastTs = 0, rewarded = false };

    private void ResetDaily(TaskData t)
    {
        t.progress = 0; t.rewarded = false; t.lastTs = DateTimeOffset.Now.ToUnixTimeSeconds();
    }

    private bool IsToday(long unixSec)
    {
        var dt = DateTimeOffset.FromUnixTimeSeconds(unixSec).DateTime;
        return dt.Date == DateTime.Now.Date;
    }

    private void GiveReward(string taskId)
    {
        // 这里发大奖：钻石、金币、道具……
        int diamond = 100;
        // 举例：DataManager.Diamond += diamond;
        Debug.Log($"[AdTask] 任务 {taskId} 完成，发放奖励 {diamond} 钻石");
    }

    private void Load()
    {
        string json = PlayerPrefs.GetString(SAVE_KEY, "[]");
        tasks = JsonUtility.FromJson<List<TaskData>>(json) ?? new List<TaskData>();
    }

    private void Save()
    {
        PlayerPrefs.SetString(SAVE_KEY, JsonUtility.ToJson(tasks));
    }
}
#endif