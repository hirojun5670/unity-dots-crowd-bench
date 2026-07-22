using UnityDotsCrowdLab.Features.Targeting;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityDotsCrowdLab.Core.UI
{
    public class DOTSView : MonoBehaviour
    {
        [SerializeField] UIDocument uIDocument;

        private Label countLabel;
        private Label fpsLabel;
        private Label fpsAverageLabel;
        private Label targetingModeLabel;


        private readonly string countLabelName = "label-count";
        private readonly string fpsLabelName = "label-fps";
        private readonly string fpsAverageLabelName = "label-fps-average";
        private readonly string targetingModeLabelName = "label-targeting-mode";
        void OnEnable()
        {
            var root = uIDocument.rootVisualElement;
            countLabel = root.Q<Label>(countLabelName);
            fpsLabel = root.Q<Label>(fpsLabelName);
            fpsAverageLabel = root.Q<Label>(fpsAverageLabelName);
            targetingModeLabel = root.Q<Label>(targetingModeLabelName);
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