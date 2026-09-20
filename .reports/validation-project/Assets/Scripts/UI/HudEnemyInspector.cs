using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TowerDefense.Actors;
using TowerDefense.Core;
using TowerDefense.Data;

namespace TowerDefense.UI
{
    /// <summary>
    /// 敌影信息卡：「敌人 UI 也要克制 —— 默认只显示 HP，锁定后才展开」。
    ///
    /// 1) 平时敌人身上只有一条血条（HealthBarView），不占任何屏幕空间；
    /// 2) 只有指针放到敌人身上（玩家主动表达「我想知道它是谁」）时卡片才出现；
    /// 3) 卡片贴着敌人（世界锚定）+ 四角锁定框，不是弹一个居中面板；
    /// 4) 只回答「我该怎么处理它」：耐久 / 速度 / 赏金 / 破界 / 余程 / 是否减速；
    /// 5) 摆放交给 <see cref="TdLayout.BesideUnit"/>，自动避让上下安全区。
    /// </summary>
    public sealed class HudEnemyInspector : MonoBehaviour
    {
        private const float PanelW = 236f;
        private const float PanelH = 132f;
        private const float LockSize = 40f;

        // 拾取：先用宽松半径取最近敌人，再用「半径 + 容差」精确判定，避免大体型 Boss 点不中。
        private const float PickRadius = 2.2f;
        private const float PickSlack = 0.28f;

        private RectTransform _canvasRect;
        private GameObject _root;
        private RectTransform _panel;
        private RectTransform _lockRoot;
        private RectTransform _lockCorners;
        private Image _leader;
        private Image _spine;

        private UiText _title;
        private UiText _tagText;
        private RoundedRectImage _hpFill;
        private UiText _hpText;
        private UiText _rowStats;
        private UiText _rowThreat;
        private UiText _rowState;

        private Enemy _current;
        private bool _visible;
        private float _entrance;

        private void Awake()
        {
            _canvasRect = (RectTransform)transform;

            _root = new GameObject("EnemyInspector", typeof(RectTransform));
            _root.transform.SetParent(transform, false);
            TdLayout.Fill(_root.GetComponent<RectTransform>());

            _leader = UiFactory.CreateImage(_root.transform, "Leader", TdTheme.Barrier);
            _leader.raycastTarget = false;
            TdLayout.PinToCanvas(_leader.rectTransform, new Vector2(0f, 0.5f));
            _leader.rectTransform.sizeDelta = new Vector2(1f, 1f);

            // 锁定框：四角 L 型角标，贴在被查看的敌人身上（「我正在看这一个」）
            _lockRoot = TdLayout.NewChild(_root.transform, "Lock");
            TdLayout.PinToCanvas(_lockRoot, new Vector2(0.5f, 0.5f));
            _lockRoot.sizeDelta = new Vector2(LockSize, LockSize);
            _lockCorners = TdKit.Corners(_lockRoot, "LockBrackets", TdTheme.Barrier, 11f, 2f);

            var panel = TdKit.Glass(_root.transform, "Panel", TdTheme.Radius.Panel, TdTheme.Glass, TdTheme.Edge, 1f);
            _panel = panel.rectTransform;
            TdLayout.PinToCanvas(_panel, new Vector2(0.5f, 0.5f));
            _panel.sizeDelta = new Vector2(PanelW, PanelH);

            // 左侧威胁色脊线：常驻=冰蓝，首领=神樱红
            _spine = UiFactory.CreateImage(_panel, "Spine", TdTheme.Barrier);
            _spine.raycastTarget = false;
            TdLayout.At(_spine.rectTransform, 0f, PanelH * 0.5f, 3f, PanelH);

            _title = TdKit.Text(_panel, "Title", string.Empty, TdTheme.Body, TdTheme.Ink,
                TextAnchor.MiddleLeft, true);
            TdLayout.At(_title.rectTransform, 16f, 26f, 150f, 22f);

            var tagBg = TdKit.Glass(_panel, "Tag", TdTheme.Radius.Sm, TdTheme.BtnActive, TdTheme.SlotEdge, 1f);
            tagBg.raycastTarget = false;
            TdLayout.AtRight(tagBg.rectTransform, 14f, 26f, 48f, 20f);
            _tagText = TdKit.Text(tagBg.transform, "TagText", "常驻", TdTheme.Tiny, TdTheme.Star,
                TextAnchor.MiddleCenter, true);
            UiFactory.Stretch(_tagText.rectTransform);

            var hp = UiFactory.CreateProgressBar(_panel, "Hp", TdTheme.Barrier, TdTheme.Rail);
            TdLayout.At(hp.background.rectTransform, 16f, 52f, 148f, 8f);
            _hpFill = hp.fill;

            _hpText = TdKit.Text(_panel, "HpText", string.Empty, TdTheme.Tiny, TdTheme.InkDim,
                TextAnchor.MiddleRight, true);
            TdLayout.AtRight(_hpText.rectTransform, 16f, 52f, 60f, 14f);

            _rowStats = TdKit.Text(_panel, "Stats", string.Empty, TdTheme.Tiny, TdTheme.InkDim,
                TextAnchor.MiddleLeft);
            TdLayout.At(_rowStats.rectTransform, 16f, 74f, PanelW - 32f, 14f);

            _rowThreat = TdKit.Text(_panel, "Threat", string.Empty, TdTheme.Tiny, TdTheme.InkDim,
                TextAnchor.MiddleLeft);
            TdLayout.At(_rowThreat.rectTransform, 16f, 92f, PanelW - 32f, 14f);

            _rowState = TdKit.Text(_panel, "State", string.Empty, TdTheme.Tiny, TdTheme.InkFaint,
                TextAnchor.MiddleLeft);
            TdLayout.At(_rowState.rectTransform, 16f, 112f, PanelW - 32f, 14f);

            _root.SetActive(false);
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            var enemy = gm != null ? PickEnemy() : null;

            if (enemy == null)
            {
                if (_visible)
                {
                    _visible = false;
                    _current = null;
                    _root.SetActive(false);
                }
                return;
            }

            if (!_visible)
            {
                _visible = true;
                _entrance = 0f;
                _root.SetActive(true);
            }
            _entrance = Mathf.Clamp01(_entrance + Time.unscaledDeltaTime / TdTheme.Motion.Fast);
            _current = enemy;

            Position(enemy);
            RefreshContent(enemy);
        }

        /// <summary>指针是否指向某个敌人（透视相机下按敌人的视觉高度平面求交，避免与地面拾取错位）。</summary>
        private static Enemy PickEnemy()
        {
            var cam = Camera.main;
            var gm = GameManager.Instance;
            if (cam == null || gm == null) return null;

            // 指针停在 UI 上时不做世界拾取，避免与按钮 / 部署卡抢焦点。
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return null;
            if (gm.State != GameState.Running || gm.IsPaused) return null;

            var ray = cam.ScreenPointToRay(Input.mousePosition);
            var plane = new Plane(Vector3.forward, new Vector3(0f, 0f, GameConfig.EnemyVisualHeight));
            if (!plane.Raycast(ray, out float hit)) return null;

            var point = (Vector2)ray.GetPoint(hit);
            var enemy = gm.GetNearestEnemy(point, PickRadius);
            if (enemy == null || !enemy.IsAlive) return null;

            return Vector2.Distance(point, enemy.transform.position) <= enemy.Radius + PickSlack ? enemy : null;
        }

        private void Position(Enemy enemy)
        {
            var cam = Camera.main;
            if (cam == null) return;

            var unit = TdLayout.WorldToCanvasLocal(_canvasRect, enemy.transform.position, cam);
            float halfW = PanelW * 0.5f;
            float halfH = PanelH * 0.5f;
            var pos = TdLayout.BesideUnit(_canvasRect, unit, halfW, halfH, TdTheme.Space.Lg, out bool flip);

            float eased = UiEasings.OutCubic(_entrance);
            float scale = Mathf.Lerp(0.94f, 1f, eased);
            _panel.localScale = new Vector3(scale, scale, 1f);
            _panel.anchoredPosition = TdLayout.ToAnchored(_canvasRect, pos);

            float edgeX = flip ? pos.x + halfW * scale : pos.x - halfW * scale;
            var from = new Vector2(edgeX, pos.y);
            var delta = unit - from;
            _leader.rectTransform.anchoredPosition = TdLayout.ToAnchored(_canvasRect, from);
            _leader.rectTransform.sizeDelta = new Vector2(delta.magnitude, 1f);
            _leader.rectTransform.localRotation =
                Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

            // 锁定框跟随敌人：尺寸随敌人体型缩放，让「多大一团威胁」一眼可感
            _lockRoot.anchoredPosition = TdLayout.ToAnchored(_canvasRect, unit);
            float lockScale = Mathf.Clamp(enemy.Radius / 0.30f, 0.7f, 2.6f) * Mathf.Lerp(1.18f, 1f, eased);
            _lockRoot.localScale = new Vector3(lockScale, lockScale, 1f);
        }

        private void RefreshContent(Enemy enemy)
        {
            var def = enemy.Definition;
            if (def == null) return;

            bool boss = enemy.IsBoss;
            var accent = boss ? TdTheme.Barrier : TdTheme.Glass2;

            _title.text = def.DisplayName;
            _title.color = boss ? TdTheme.Barrier : TdTheme.Ink;
            _tagText.text = TypeLabel(def.Type);
            _tagText.color = accent;
            _spine.color = new Color(accent.r, accent.g, accent.b, 0.95f);
            TdKit.TintCorners(_lockCorners, accent);
            _leader.color = new Color(accent.r, accent.g, accent.b, 0.45f * Mathf.Clamp01(_entrance + 0.2f));

            float ratio = enemy.HealthRatio;
            _hpFill.Progress = ratio;
            _hpFill.color = accent;
            _hpText.text = Mathf.CeilToInt(enemy.Health) + " / " + Mathf.CeilToInt(enemy.MaxHealth);

            _rowStats.text = "速度 " + def.Speed.ToString("0.##") + " · 赏金 " + def.GoldReward;
            _rowThreat.text = def.DamageToBase > 0
                ? "破界 " + def.DamageToBase + " · 余程 " + Mathf.Max(0, Mathf.RoundToInt(enemy.DistanceToEnd)) + " 格"
                : "无破界 · 余程 " + Mathf.Max(0, Mathf.RoundToInt(enemy.DistanceToEnd)) + " 格";
            _rowThreat.color = def.DamageToBase > 0 ? TdTheme.Barrier : TdTheme.InkFaint;

            if (enemy.IsSlowed)
            {
                _rowState.text = "霜缓中 · 移速下降";
                _rowState.color = TdTheme.Glass2;
            }
            else
            {
                _rowState.text = "状态正常";
                _rowState.color = TdTheme.InkFaint;
            }
        }

        /// <summary>当前正在查看的敌人（供其他 HUD 组件判断是否让位）。</summary>
        public Enemy Current => _current;

        private static string TypeLabel(EnemyType type)
        {
            switch (type)
            {
                case EnemyType.Fast: return "疾行";
                case EnemyType.Tank: return "重甲";
                case EnemyType.Boss: return "首领";
                default: return "常驻";
            }
        }
    }
}
