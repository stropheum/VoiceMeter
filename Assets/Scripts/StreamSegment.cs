using System;
using UnityEngine;
using UnityEngine.UI;

namespace VoiceMeter
{
    [RequireComponent(typeof(Image))]
    public class StreamSegment : MonoBehaviour
    {
        public float LifeSpan { get; private set; }
        public float? DestroyAfterInactiveSeconds { get; set; }
        public float DurationInSeconds => (float)(_model.End - _model.Start).TotalSeconds;
        public StreamVisualizer VisualizerContext { get; set; }
        public DateTime StartTime => _model.Start;
        public DateTime EndTime => _model.End;
        public Image Image { get; private set; }
        
        private StreamSegmentModel _model;
        private Rigidbody2D _rigidbody;


        private void Awake()
        {
            Image = GetComponent<Image>();
        }
        
        private void Start()
        {
            UpdateBarWidth();
        }
        
        private void Update()
        {
            LifeSpan += Time.deltaTime;
            if (DestroyAfterInactiveSeconds.HasValue && LifeSpan - DurationInSeconds >= DestroyAfterInactiveSeconds)
            {
                Destroy(gameObject);
            }
        }

        private void FixedUpdate()
        {
            if (VisualizerContext == null)
            {
                return;
            }

            UpdateOffset();
        }

        public void SetStreamSegmentModel(StreamSegmentModel model)
        {
            _model = model;
            UpdateBarWidth();
        }

        public void StitchSegmentModel(StreamSegmentModel modelToStitch)
        {
            _model.End = modelToStitch.End;
            UpdateBarWidth();
        }
        
        private void UpdateOffset()
        {
            float widthValuePerSecond = WidthValuePerSecond();
            Image.rectTransform.localPosition -= new Vector3(widthValuePerSecond * Time.fixedDeltaTime, 0, 0);
        }

        private void UpdateBarWidth()
        {
            if (Image == null)
            {
                return;
            }
            float parentWidth = VisualizerContext.RectTransform.rect.width;
            float percent = DurationInSeconds / VisualizerContext.TimeWindow;
            Image.rectTransform.sizeDelta = new Vector2(parentWidth * percent, Image.rectTransform.sizeDelta.y);
        }
        
        private float WidthValuePerSecond()
        {
            return VisualizerContext.RectTransform.rect.width / VisualizerContext.TimeWindow;            
        }
    }
}
