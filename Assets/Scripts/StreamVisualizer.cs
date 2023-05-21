using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace VoiceMeter
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class StreamVisualizer : Graphic
    {
        [field: SerializeField] public Color Color { get; set; } = Color.blue;
        private List<UIVertex> _vertices = new();
        
        public float TimeWindow { get; set; }
        public List<StreamSegmentModel> Models { get; set; } = new();

        protected override void Start()
        {
            base.Start();
            Color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 1f);
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Debug.Assert(_vertices.Count % 3 == 0);
            vh.AddUIVertexTriangleStream(_vertices);
        }

        private void FixedUpdate()
        {
            RenderSegments();
        }

        private void RenderSegments()
        {
            Vector2 size = rectTransform.rect.size;
            Vector2 pivot = rectTransform.pivot;
            
            _vertices.Clear();
            foreach (StreamSegmentModel model in Models.ToArray())
            {
                Vector2 xBounds = MapTimeSpanToPercent(model);
                var verts = new UIVertex[6];
                verts[0].position = new Vector2(xBounds.x, -pivot.y * size.y);
                verts[0].color = Color.black;
                verts[1].position = new Vector2(xBounds.x, (1f - pivot.y) * size.y);
                verts[1].color = Color;
                verts[2].position = new Vector2(xBounds.y, (1f - pivot.y) * size.y);
                verts[2].color = Color;
                verts[3] = verts[2];
                verts[4].position = new Vector2(xBounds.y, -pivot.y * size.y);
                verts[4].color = Color.black;
                verts[5] = verts[0];
                _vertices.AddRange(verts); // Add 2 triangles to make the quad
                SetVerticesDirty();
            }
        }

        private Vector2 MapTimeSpanToPercent(StreamSegmentModel model)
        {
            Vector2 size = rectTransform.rect.size;
            Vector2 pivot = rectTransform.pivot;

            var segmentDuration = (float)(model.End - model.Start).TotalSeconds;
            if (segmentDuration == 0f)
            {
                return Vector2.zero;
            }

            var startOffset = (float)(model.Start - (DateTime.Now - TimeSpan.FromSeconds(TimeWindow))).TotalSeconds;
            var bound = pivot.x * size.x;
            var result = new Vector2(
                math.remap(0f, 1f, -bound, bound, startOffset / TimeWindow), 
                math.remap(0f, 1f, -bound, bound, (startOffset + segmentDuration) / TimeWindow));
            return result;
        }

        private void RenderQuad(VertexHelper vh)
        {
            Vector2 size = rectTransform.rect.size;
            Vector2 pivot = rectTransform.pivot;
            UIVertex vertex = UIVertex.simpleVert;

            vertex.position = new Vector2(-pivot.x * size.x, -pivot.y * size.y);
            vertex.color = Color.black;
            vh.AddVert(vertex);
            
            vertex.position = new Vector2(-pivot.x * size.x, (1f - pivot.y) * size.y);
            vertex.color = Color.blue;
            vh.AddVert(vertex);
            
            vertex.position = new Vector2((1f - pivot.x) * size.x, (1f - pivot.y) * size.y);
            vertex.color = Color.blue;
            vh.AddVert(vertex);
            
            vertex.position = new Vector2((1f - pivot.x) * size.x, -pivot.y * size.y);
            vertex.color = Color.black;
            vh.AddVert(vertex);
            
            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }
    }
}
