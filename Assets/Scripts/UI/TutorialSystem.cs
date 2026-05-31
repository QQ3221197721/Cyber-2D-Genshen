using UnityEngine;
using System.Collections.Generic;

namespace CyberTerraria
{
    public class TutorialSystem : MonoBehaviour
    {
        public static TutorialSystem Instance { get; private set; }

        private int _currentStep = 0;
        private bool _tutorialActive = false;
        private float _stepTimer = 0f;
        private bool _tutorialFinished = false;

        // 预创建的纹理（避免OnGUI中每帧new）
        private Texture2D _bgTex;
        private Texture2D _dotActiveTex;
        private Texture2D _dotCurrentTex;
        private Texture2D _dotInactiveTex;

        // 教程步骤定义
        private struct TutorialStep
        {
            public string instruction;
            public string aiDialogue;
            public System.Func<bool> completionCheck;
            public float autoAdvanceTime;
        }

        private List<TutorialStep> _steps = new List<TutorialStep>();

        // 追踪玩家行为
        public bool HasMoved { get; set; }
        public bool HasJumped { get; set; }
        public bool HasMined { get; set; }
        public bool HasOpenedInventory { get; set; }
        public bool HasCrafted { get; set; }
        public bool HasEquippedWeapon { get; set; }
        public bool HasAttacked { get; set; }
        public bool HasPlacedBlock { get; set; }

        void Awake()
        {
            Instance = this;
            InitializeTextures();
            InitializeSteps();
        }

        void Start()
        {
            Invoke("StartTutorial", 3f);
        }

        private void InitializeTextures()
        {
            _bgTex = new Texture2D(1, 1);
            _bgTex.SetPixel(0, 0, new Color(0, 0, 0, 0.7f));
            _bgTex.Apply();

            _dotActiveTex = new Texture2D(1, 1);
            _dotActiveTex.SetPixel(0, 0, new Color(0.3f, 1f, 0.9f));
            _dotActiveTex.Apply();

            _dotCurrentTex = new Texture2D(1, 1);
            _dotCurrentTex.SetPixel(0, 0, Color.white);
            _dotCurrentTex.Apply();

            _dotInactiveTex = new Texture2D(1, 1);
            _dotInactiveTex.SetPixel(0, 0, new Color(0.3f, 0.3f, 0.3f));
            _dotInactiveTex.Apply();
        }

        private void InitializeSteps()
        {
            _steps.Add(new TutorialStep {
                instruction = "按 A/D 键移动",
                aiDialogue = "检测到生命体...系统初始化完毕。先试试移动吧，按A和D键左右走动。",
                completionCheck = () => HasMoved,
                autoAdvanceTime = 0
            });

            _steps.Add(new TutorialStep {
                instruction = "按 空格键 跳跃",
                aiDialogue = "很好！现在按空格键跳跃试试。",
                completionCheck = () => HasJumped,
                autoAdvanceTime = 0
            });

            _steps.Add(new TutorialStep {
                instruction = "对准方块按住 左键 挖掘",
                aiDialogue = "接下来学习采集资源。用鼠标对准地面方块，按住左键即可挖掘。",
                completionCheck = () => HasMined,
                autoAdvanceTime = 0
            });

            _steps.Add(new TutorialStep {
                instruction = "按 Tab 打开背包",
                aiDialogue = "不错！挖到的资源会进入背包。按Tab键打开背包查看。",
                completionCheck = () => HasOpenedInventory,
                autoAdvanceTime = 0
            });

            _steps.Add(new TutorialStep {
                instruction = "在背包中点击「合成」标签页，合成物品",
                aiDialogue = "在合成面板中你可以用材料制作武器和工具。试试合成一件物品吧！",
                completionCheck = () => HasCrafted,
                autoAdvanceTime = 15f
            });

            _steps.Add(new TutorialStep {
                instruction = "用数字键 1-9 切换快捷栏物品",
                aiDialogue = "按数字键1-9可以切换快捷栏中的物品。试试装备你的武器！",
                completionCheck = () => HasEquippedWeapon,
                autoAdvanceTime = 8f
            });

            _steps.Add(new TutorialStep {
                instruction = "对准敌人按 左键 攻击",
                aiDialogue = "装备武器后，对着敌人按左键就能攻击。如果附近有敌人的话...我会协助你战斗！",
                completionCheck = () => HasAttacked,
                autoAdvanceTime = 10f
            });

            _steps.Add(new TutorialStep {
                instruction = "对准空地按 右键 放置方块",
                aiDialogue = "你还可以放置方块来建造。选中可放置物品后，对空地按右键放置。",
                completionCheck = () => HasPlacedBlock,
                autoAdvanceTime = 10f
            });

            _steps.Add(new TutorialStep {
                instruction = "教程完成！",
                aiDialogue = "基础操作学习完成！记住：探索废墟可以找到幸运方块，按R让我修复能获得永久加成。按N给我取个名字吧！祝你好运，幸存者。",
                completionCheck = null,
                autoAdvanceTime = 6f
            });
        }

        private void StartTutorial()
        {
            if (_tutorialFinished) return;
            _tutorialActive = true;
            _currentStep = 0;
            ShowCurrentStep();
        }

        private void ShowCurrentStep()
        {
            if (_currentStep >= _steps.Count)
            {
                FinishTutorial();
                return;
            }

            var step = _steps[_currentStep];
            _stepTimer = 0f;

            if (AICompanion.Instance != null && !string.IsNullOrEmpty(step.aiDialogue))
            {
                AICompanion.Instance.ShowDialogue(step.aiDialogue, 8f);
            }
        }

        void Update()
        {
            if (!_tutorialActive || _tutorialFinished) return;
            if (_currentStep >= _steps.Count) return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                FinishTutorial();
                return;
            }

            var step = _steps[_currentStep];
            _stepTimer += Time.deltaTime;

            bool completed = false;
            if (step.completionCheck != null)
            {
                completed = step.completionCheck();
            }

            if (step.autoAdvanceTime > 0 && _stepTimer >= step.autoAdvanceTime)
            {
                completed = true;
            }

            if (completed)
            {
                _currentStep++;
                ShowCurrentStep();
            }
        }

        private void FinishTutorial()
        {
            _tutorialActive = false;
            _tutorialFinished = true;
        }

        void OnGUI()
        {
            if (!_tutorialActive || _tutorialFinished) return;
            if (_currentStep >= _steps.Count) return;

            var step = _steps[_currentStep];

            // 屏幕底部中央显示指引文字
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 18;
            style.alignment = TextAnchor.MiddleCenter;
            style.normal.textColor = new Color(0.3f, 1f, 0.9f);
            style.fontStyle = FontStyle.Bold;

            float barWidth = 500f;
            float barHeight = 40f;
            float barX = (Screen.width - barWidth) / 2f;
            float barY = Screen.height - 80f;

            // 半透明黑色背景
            GUI.DrawTexture(new Rect(barX - 10, barY - 5, barWidth + 20, barHeight + 10), _bgTex);
            GUI.Label(new Rect(barX, barY, barWidth, barHeight), step.instruction, style);

            // 右下角ESC跳过提示
            GUIStyle skipStyle = new GUIStyle(GUI.skin.label);
            skipStyle.fontSize = 12;
            skipStyle.normal.textColor = new Color(1f, 1f, 1f, 0.5f);
            skipStyle.alignment = TextAnchor.LowerRight;
            GUI.Label(new Rect(Screen.width - 170, Screen.height - 35, 150, 20), "按 ESC 跳过教程", skipStyle);

            // 步骤进度指示器（小圆点）
            float dotStartX = (Screen.width - _steps.Count * 15) / 2f;
            for (int i = 0; i < _steps.Count; i++)
            {
                Texture2D dotTex = i < _currentStep ? _dotActiveTex :
                                   i == _currentStep ? _dotCurrentTex :
                                   _dotInactiveTex;
                GUI.DrawTexture(new Rect(dotStartX + i * 15, Screen.height - 40, 8, 8), dotTex);
            }
        }

        public bool IsActive => _tutorialActive && !_tutorialFinished;
    }
}
