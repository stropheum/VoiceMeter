using UnityEngine;
using UnityEngine.UI;

namespace VoiceMeter
{
    public class EquityMeter : MonoBehaviour
    {
        [SerializeField] private Image _bars;

        private void Awake()
        {
            Debug.Assert(_bars != null);
        }

        private void Start()
        {
            _bars.fillAmount = 0f;
        }

        public void DisplayPercent(float percent)
        {
            float truncatedPercent = Mathf.RoundToInt(percent * 10) / 10f;
            _bars.fillAmount = truncatedPercent;
            UpdateColor();
        }

        private void UpdateColor()
        {
            Color dangerColor = _bars.fillAmount < 0.5f ? Color.gray : Color.red;
            Color goodColor = Color.green;
            float safetyPercent = 1.0f - (Mathf.Abs(0.5f - _bars.fillAmount) * 2f);
            _bars.color = Color.Lerp(dangerColor, goodColor, safetyPercent);
        }
    }
}
