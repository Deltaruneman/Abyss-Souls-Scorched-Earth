using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Danh sach cac kha nang co the mo khoa dan trong cot truyen.
/// Them skill moi vao day khi can, PlayerController se tu doi chieu qua IsUnlocked().
/// </summary>
public enum SkillType
{
    Dash,           // phim K
    RangedWeapon,   // cho phep SwitchWeapon sang sung (Ranged)
    MeleeSkill,     // skill I khi dang cam vu khi Melee
    RangedSkill     // skill I khi dang cam vu khi Ranged
}

[Serializable]
public class SkillUnlockedEvent : UnityEvent<SkillType> { }

/// <summary>
/// Singleton quan ly trang thai mo khoa ky nang cua nhan vat, ton tai xuyen suot cac scene.
/// Goi SkillUnlockManager.Instance.Unlock(SkillType.Dash) tu cutscene/dialogue/trigger
/// de mo khoa dan dan theo cot truyen. PlayerController tu dong doc trang thai nay,
/// khong can gan reference thu cong.
/// </summary>
public class SkillUnlockManager : MonoBehaviour
{
    public static SkillUnlockManager Instance { get; private set; }

    [Header("Mo khoa mac dinh khi bat dau game moi")]
    public List<SkillType> defaultUnlockedSkills = new List<SkillType>();

    [Header("Luu tien trinh")]
    public bool persistBetweenScenes = true;
    public bool saveToPlayerPrefs = true;
    public string playerPrefsKey = "SkillUnlockManager_UnlockedSkills";

    [Header("Events")]
    public SkillUnlockedEvent onSkillUnlocked; // ban co the noi vao day de hien thong bao "Da mo khoa: ..."

    private readonly HashSet<SkillType> unlockedSkills = new HashSet<SkillType>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (persistBetweenScenes) DontDestroyOnLoad(gameObject);

        LoadProgress();
    }

    // ===================== TRUY VAN =====================
    public bool IsUnlocked(SkillType skill) => unlockedSkills.Contains(skill);

    public IReadOnlyCollection<SkillType> GetUnlockedSkills() => unlockedSkills;

    // ===================== MO KHOA =====================
    // goi ham nay tu diem cot truyen (vd: cuoi 1 cutscene, khi nhat 1 item dac biet, khi qua 1 trigger...)
    public void Unlock(SkillType skill)
    {
        if (unlockedSkills.Add(skill))
        {
            onSkillUnlocked?.Invoke(skill);
            SaveProgress();
        }
    }

    // huu ich khi test nhanh trong Editor hoac cheat code
    public void UnlockAll()
    {
        foreach (SkillType skill in Enum.GetValues(typeof(SkillType)))
        {
            Unlock(skill);
        }
    }

    // reset lai tien trinh (vd: khi choi New Game)
    public void ResetProgress()
    {
        unlockedSkills.Clear();
        foreach (SkillType skill in defaultUnlockedSkills) unlockedSkills.Add(skill);

        if (saveToPlayerPrefs) SaveProgress();
    }

    // ===================== LUU / TAI =====================
    private void SaveProgress()
    {
        if (!saveToPlayerPrefs) return;

        string data = string.Join(",", unlockedSkills.Select(s => s.ToString()));
        PlayerPrefs.SetString(playerPrefsKey, data);
        PlayerPrefs.Save();
    }

    private void LoadProgress()
    {
        unlockedSkills.Clear();

        if (saveToPlayerPrefs && PlayerPrefs.HasKey(playerPrefsKey))
        {
            string data = PlayerPrefs.GetString(playerPrefsKey);
            if (!string.IsNullOrEmpty(data))
            {
                foreach (string entry in data.Split(','))
                {
                    if (Enum.TryParse(entry, out SkillType skill)) unlockedSkills.Add(skill);
                }
                return;
            }
        }

        // chua co du lieu luu (lan dau choi) -> dung danh sach mac dinh
        foreach (SkillType skill in defaultUnlockedSkills) unlockedSkills.Add(skill);
    }

#if UNITY_EDITOR
    [ContextMenu("Debug: Unlock All")]
    private void DebugUnlockAll() => UnlockAll();

    [ContextMenu("Debug: Reset Progress")]
    private void DebugResetProgress() => ResetProgress();
#endif
}