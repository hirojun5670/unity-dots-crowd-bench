using UnityDotsCrowdLab.Features.Targeting;
using UnityEngine;
using UnityEngine.UIElements;
using R3;

namespace UnityDotsCrowdLab.Core.UI
{
    public class DOTSView : MonoBehaviour
    {
        [SerializeField] UIDocument uIDocument;

        private Label countLabel;
        private Label fpsLabel;
        private Label fpsAverageLabel;
        private Label targetingModeLabel;
        private Button spawnerToggleButton;


        private readonly string countLabelName = "label-count";
        private readonly string fpsLabelName = "label-fps";
        private readonly string fpsAverageLabelName = "label-fps-average";
        private readonly string targetingModeLabelName = "label-targeting-mode";
        private readonly string spawnerToggleButtonName = "button-spawner-toggle";

        private readonly ReactiveProperty<bool> spawnerActive = new(false);
        public ReadOnlyReactiveProperty<bool> SpawnerActive => spawnerActive;

        void OnEnable()
        {
            var root = uIDocument.rootVisualElement;
            countLabel = root.Q<Label>(countLabelName);
            fpsLabel = root.Q<Label>(fpsLabelName);
            fpsAverageLabel = root.Q<Label>(fpsAverageLabelName);
            targetingModeLabel = root.Q<Label>(targetingModeLabelName);

            spawnerToggleButton = root.Q<Button>(spawnerToggleButtonName);
            if (spawnerToggleButton != null)
            {
                spawnerToggleButton.clicked += OnClickSpawnerToggle;
                UpdateSpawnerButtonText(spawnerActive.Value);
            }
        }

        void OnDisable()
        {
            if (spawnerToggleButton != null)
            {
                spawnerToggleButton.clicked -= OnClickSpawnerToggle;
            }
        }

        private void OnClickSpawnerToggle()
        {
            var next = !spawnerActive.Value;
            spawnerActive.Value = next;
            UpdateSpawnerButtonText(next);
        }

        private void UpdateSpawnerButtonText(bool active)
        {
            if (spawnerToggleButton != null)
            {
                spawnerToggleButton.text = active ? "Active: ON" : "Active: OFF";
            }
        }


        public void SetCount(int value)
        {
            if (countLabel != null)
            {
                countLabel.text = value.ToString();
            }
        }

        public void SetFpsInfo(float current, float average, TargetingMode mode)
        {
            if (fpsLabel != null)
            {
                fpsLabel.text = $"{current:F0}";
            }
            if (fpsAverageLabel != null)
            {
                fpsAverageLabel.text = $"{average:F0}";
            }
            if (targetingModeLabel != null)
            {
                targetingModeLabel.text = $"{mode}";
            }
        }
    }
}