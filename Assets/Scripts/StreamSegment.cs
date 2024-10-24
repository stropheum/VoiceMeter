using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace VoiceMeter
{
    [RequireComponent(typeof(Image))]
    public class StreamSegment : MonoBehaviour
    {
        public StreamSegmentModel Model { get; set; }
        public float LifeSpan { get; private set; }
        public float? DestroyAfterInactiveSeconds { get; set; } = null;
        public double DurationInSeconds => (Model.End - Model.Start).TotalSeconds;
        public StreamVisualizer VisualizerContext { get; set; }
        
        private List<UIVertex> _vertices = new();
        private Image _image;

        private void Start()
        {
            _image = GetComponent<Image>();
        }
        
        private void Update()
        {
            LifeSpan += Time.deltaTime;
            if (DestroyAfterInactiveSeconds.HasValue && LifeSpan - DurationInSeconds >= DestroyAfterInactiveSeconds)
            {
                Destroy(this);
            }
        }

        private void FixedUpdate()
        {
            if (VisualizerContext == null)
            {
                return;
            }
            RenderSegments();
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

        private void RenderSegments()
        {
            Vector2 size = _image.rectTransform.rect.size;
            Vector2 pivot = _image.rectTransform.pivot;
            
            _vertices.Clear();
            // Vector2 xBounds = MapTimeSpanToPercent(Model);
            var verts = new UIVertex[6];
            verts[0].position = new Vector2(-pivot.x * size.x, -pivot.y * size.y);
            verts[0].color = Color.black;
            verts[1].position = new Vector2(-pivot.x * size.x, (1f - pivot.y) * size.y);
            verts[1].color = VisualizerContext.Color;
            verts[2].position = new Vector2(pivot.x * size.x, (1f - pivot.y) * size.y);
            verts[2].color = VisualizerContext.Color;
            verts[3] = verts[2];
            verts[4].position = new Vector2(pivot.x * size.x, -pivot.y * size.y);
            verts[4].color = Color.black;
            verts[5] = verts[0];
            _vertices.AddRange(verts); // Add 2 triangles to make the quad
            _image.SetVerticesDirty();
        }
        
        private Vector2 MapTimeSpanToPercent(StreamSegmentModel model)
        {
            Vector2 size = _image.rectTransform.rect.size;
            Vector2 pivot = _image.rectTransform.pivot;
        
            var segmentDuration = (float)(model.End - model.Start).TotalSeconds;
            if (segmentDuration == 0f)
            {
                return Vector2.zero;
            }
        
            float timeWindow = VisualizerContext.TimeWindow;
            var startOffset = (float)(model.Start - (DateTime.Now - TimeSpan.FromSeconds(timeWindow))).TotalSeconds;
            float bound = pivot.x * size.x;
            var result = new Vector2(
                math.remap(0f, 1f, -bound, bound, startOffset / timeWindow), 
                math.remap(0f, 1f, -bound, bound, (startOffset + segmentDuration) / timeWindow));
            return result;
        }
    }
}
