using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VoiceMeter
{
    [RequireComponent(typeof(Image))]
    public class StreamSegment : MonoBehaviour
    {
        public float LifeSpan { get; private set; }
        public float? DestroyAfterInactiveSeconds { get; set; } = null;
        public float DurationInSeconds => (float)(_model.End - _model.Start).TotalSeconds;
        public StreamVisualizer VisualizerContext { get; set; }
        public DateTime StartTime => _model.Start;
        public DateTime EndTime => _model.End;
        
        private StreamSegmentModel _model;
        private List<UIVertex> _vertices = new();
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
            // LifeSpan += Time.fixedDeltaTime;
            if (VisualizerContext == null)
            {
                return;
            }

            UpdateOffset();
            // RenderSegments();
        }

        // protected override void OnPopulateMesh(VertexHelper vh)
        // {
        //     vh.Clear();
        //     Debug.Assert(_vertices.Count % 3 == 0);
        //     vh.AddUIVertexTriangleStream(_vertices);
        // }

        // private void OnDrawGizmos()
        // {
        //     Gizmos.color = Color.magenta;
        //     var localVerts = _vertices.Select(x => transform.TransformPoint(x.position)).ToArray();
        //     for (int i = 0; i < localVerts.Length - 1; i++)
        //     {
        //         Gizmos.DrawLine(localVerts[i], localVerts[i + 1]);
        //     }
        //     Gizmos.DrawLine(localVerts[^1], localVerts[0]);
        // }
        
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
            // float xOffset = VisualizerContext.rectTransform.rect.width - (widthValuePerSecond * LifeSpan);
            // float xOffset = -(widthValuePerSecond * LifeSpan);
            _image.rectTransform.localPosition -= new Vector3(widthValuePerSecond * Time.fixedDeltaTime, 0, 0);
        }

        // private void RenderSegments()
        // {
        //     Vector2 size = _sprite.rectTransform.rect.size;
        //     Vector2 pivot = _sprite.rectTransform.pivot;
        //     
        //     _vertices.Clear();
        //     // Vector2 xBounds = MapTimeSpanToPercent(Model);
        //     var verts = new UIVertex[6];
        //     verts[0].position = new Vector2(-pivot.x * size.x, -pivot.y * size.y);
        //     verts[0].color = Color.black;
        //     verts[1].position = new Vector2(-pivot.x * size.x, (1f - pivot.y) * size.y);
        //     verts[1].color = VisualizerContext.Color;
        //     verts[2].position = new Vector2(pivot.x * size.x, (1f - pivot.y) * size.y);
        //     verts[2].color = VisualizerContext.Color;
        //     verts[3] = verts[2];
        //     verts[4].position = new Vector2(pivot.x * size.x, -pivot.y * size.y);
        //     verts[4].color = Color.black;
        //     verts[5] = verts[0];
        //     _vertices.AddRange(verts); // Add 2 triangles to make the quad
        //     _sprite.SetVerticesDirty();
        // }
        
        // private Vector2 MapTimeSpanToPercent(StreamSegmentModel model)
        // {
        //     Vector2 size = _sprite.rectTransform.rect.size;
        //     Vector2 pivot = _sprite.rectTransform.pivot;
        //
        //     var segmentDuration = (float)(model.End - model.Start).TotalSeconds;
        //     if (segmentDuration == 0f)
        //     {
        //         return Vector2.zero;
        //     }
        //
        //     float timeWindow = VisualizerContext.TimeWindow;
        //     var startOffset = (float)(model.Start - (DateTime.Now - TimeSpan.FromSeconds(timeWindow))).TotalSeconds;
        //     float bound = pivot.x * size.x;
        //     var result = new Vector2(
        //         math.remap(0f, 1f, -bound, bound, startOffset / timeWindow), 
        //         math.remap(0f, 1f, -bound, bound, (startOffset + segmentDuration) / timeWindow));
        //     return result;
        // }

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
