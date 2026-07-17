using UnityEngine;
using UnityEngine.UIElements;

namespace UnityDotsCrowdLab.Core.UI
{
    public class DOTSView : MonoBehaviour
    {
        [SerializeField] UIDocument uIDocument;

        private Label countLabel;
        private readonly string countLabelName = "label-count";

        void OnEnable()
        {
            var root = uIDocument.rootVisualElement;
            countLabel = root.Q<Label>(countLabelName);
        }

        public void SetCount(int value)
        {
            if (countLabel != null)
            {
                countLabel.text = value.ToString();
            }
        }
    }
}