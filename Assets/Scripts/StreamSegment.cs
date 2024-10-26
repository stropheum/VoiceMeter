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
        
        private StreamSegmentModel _model;
        private Image _image;
        private Rigidbody2D _rigidbody; 

        private void Start()
        {
            _image = GetComponent<Image>();
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
            _image.rectTransform.localPosition -= new Vector3(widthValuePerSecond * Time.fixedDeltaTime, 0, 0);
        }

        private void UpdateBarWidth()
        {
            if (_image == null)
            {
                return;
            }
            float parentWidth = VisualizerContext.rectTransform.rect.width;
            float percent = DurationInSeconds / VisualizerContext.TimeWindow;
            _image.rectTransform.sizeDelta = new Vector2(parentWidth * percent, _image.rectTransform.sizeDelta.y);
        }
        
        private float WidthValuePerSecond()
        {
            return VisualizerContext.rectTransform.rect.width / VisualizerContext.TimeWindow;            
        }
    }
}
