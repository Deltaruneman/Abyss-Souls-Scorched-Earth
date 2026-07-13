using System;
using UnityEngine;

/// <summary>
/// Chứa toàn bộ chỉ số cấu hình (balance) của Player, nhóm theo từng nhóm chức năng
/// giống hệt các [Header] cũ trong PlayerController để dễ đối chiếu.
/// Tạo asset qua menu: Assets > Create > Game > Player Stats.
/// Lợi ích: có thể tạo nhiều bộ chỉ số khác nhau (ví dụ Player_Normal, Player_Hardcore,
/// hoặc chỉ số test khi tune game) và chỉ cần đổi asset gán vào PlayerController,
/// không cần sửa từng field trên prefab / object trong scene.
/// </summary>
[CreateAssetMenu(fileName = "PlayerStats", menuName = "Game/Player Stats")]
public class PlayerStatsSO : ScriptableObject
{
    [Serializable]
    public class MovementSettings
    {
        [Tooltip("Tốc độ di chuyển tối đa")]
        public float moveSpeed = 8f;
        [Tooltip("Tốc độ tăng tốc khi bắt đầu di chuyển (đơn vị/giây^2)")]
        public float acceleration = 60f;
        [Tooltip("Tốc độ giảm tốc khi dừng di chuyển")]
        public float deceleration = 70f;
        [Tooltip("Giảm tốc khi đổi hướng đột ngột (giúp điều khiển nhạy hơn)")]
        public float turnSpeedMultiplier = 1.6f;
        [Tooltip("Hệ số giảm khả năng điều khiển khi đang ở trên không")]
        [Range(0f, 1f)] public float airControlMultiplier = 0.8f;
        [Tooltip("Độ giảm tốc theo phương ngang khi đang ở trên không và KHÔNG có input")]
        public float airDrag = 5f;
    }

    [Serializable]
    public class JumpSettings
    {
        [Tooltip("Lực nhảy ban đầu")]
        public float jumpForce = 14f;
        [Tooltip("Lực nhảy lần 2 (double jump), thường thấp hơn lần đầu")]
        public float doubleJumpForce = 12f;
        [Tooltip("Số lần nhảy tối đa (2 = double jump)")]
        public int maxJumpCount = 2;
        [Tooltip("Trọng lực nhân thêm khi đang rơi xuống")]
        public float fallGravityMultiplier = 2.2f;
        [Tooltip("Trọng lực nhân thêm khi thả nút nhảy sớm (nhảy thấp hơn)")]
        public float lowJumpGravityMultiplier = 2.8f;
        [Tooltip("Tốc độ rơi tối đa")]
        public float maxFallSpeed = 20f;
        [Tooltip("Thời gian (giây) vẫn cho phép nhảy sau khi rời khỏi mặt đất")]
        public float coyoteTime = 0.12f;
        [Tooltip("Thời gian (giây) lưu lại input nhảy trước khi chạm đất")]
        public float jumpBufferTime = 0.12f;
    }

    [Serializable]
    public class DashSettings
    {
        [Tooltip("Phím dùng để lướt")]
        public KeyCode dashKey = KeyCode.K;
        [Tooltip("Tốc độ di chuyển ngang khi đang lướt")]
        public float dashSpeed = 20f;
        [Tooltip("Thời gian (giây) của một lần lướt")]
        public float dashDuration = 0.15f;
        [Tooltip("Thời gian chờ (giây) giữa 2 lần lướt")]
        public float dashCooldown = 0.8f;
        [Tooltip("Cho phép lướt khi đang ở trên không")]
        public bool allowAirDash = true;
        [Tooltip("Hệ số giảm tốc độ theo phương dọc khi lướt chéo")]
        [Range(0f, 1f)] public float dashVerticalMultiplier = 0.5f;

        [Header("Dash Wall Bounce")]
        [Tooltip("Layer chứa các collider được coi là chướng ngại vật/tường")]
        public LayerMask dashBounceLayer;
        [Tooltip("Lực bật ngược lại theo hướng ngược với hướng dash ban đầu khi va chạm")]
        public float dashBounceForce = 10f;
        [Tooltip("Số lượt nhảy được cộng thêm khi bị bật ngược sau va chạm lúc dash")]
        public int dashBounceJumpBonus = 1;
        [Tooltip("Thời gian (giây) khoá input di chuyển ngang sau khi bị bounce")]
        public float bounceLockDuration = 0.2f;

        [Header("Dash Damage")]
        [Tooltip("Sát thương gây ra cho mỗi địch mà player va chạm trong lúc đang dash")]
        public int dashDamage = 15;
    }

    [Serializable]
    public class CombatSettings
    {
        [Tooltip("Phím dùng để tấn công")]
        public KeyCode attackKey = KeyCode.J;
        [Tooltip("Kích thước vùng hitbox tấn công")]
        public Vector2 attackHitboxSize = new Vector2(0.8f, 0.6f);
        [Tooltip("Sát thương gây ra mỗi lần trúng đòn")]
        public int attackDamage = 10;
        [Tooltip("Thời gian chờ (giây) giữa 2 lần tấn công")]
        public float attackCooldown = 0.4f;
        [Tooltip("Layer chứa các object địch")]
        public LayerMask enemyLayer;

        [Header("Weapon Switch")]
        [Tooltip("Phím dùng để chuyển đổi giữa vũ khí cận chiến và vũ khí tầm xa")]
        public KeyCode switchWeaponKey = KeyCode.U;
        [Tooltip("Loại vũ khí mặc định khi bắt đầu")]
        public WeaponType defaultWeapon = WeaponType.Melee;
    }

    [Serializable]
    public class RangedWeaponSettings
    {
        [Tooltip("Prefab đạn (Bullet) sẽ được bắn ra khi tấn công bằng vũ khí tầm xa")]
        public GameObject bulletPrefab;
        [Tooltip("Tốc độ bay của đạn")]
        public float bulletSpeed = 15f;
        [Tooltip("Sát thương của đạn")]
        public int bulletDamage = 10;
    }

    [Serializable]
    public class HealthSettings
    {
        [Tooltip("Máu tối đa của Player")]
        public int maxHealth = 100;
        [Tooltip("Thời gian (giây) bất tử ngay sau khi trúng đòn")]
        public float invulnerabilityTime = 0.5f;
    }

    public MovementSettings movement = new MovementSettings();
    public JumpSettings jump = new JumpSettings();
    public DashSettings dash = new DashSettings();
    public CombatSettings combat = new CombatSettings();
    public RangedWeaponSettings rangedWeapon = new RangedWeaponSettings();
    public HealthSettings health = new HealthSettings();
}