using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace VoiceMeter
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class StreamVisualizer : MaskableGraphic
    {
        [field: SerializeField] public float DisplayWindowInSeconds { get; private set; } = 30f;
        [field: SerializeField] public Color Color { get; set; } = Color.blue;
        
        public float TimeWindow { get; set; }
        public List<StreamSegment> StreamSegments { get; } = new();
        private float _elapsedTime;

        protected override void Start()
        {
            base.Start();
            Color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 1f);
        }

        // protected override void OnPopulateMesh(VertexHelper vh)
        // {
        //     vh.Clear();
        //     Debug.Assert(_vertices.Count % 3 == 0);
        //     vh.AddUIVertexTriangleStream(_vertices);
        // }

        private void Update()
        {
            _elapsedTime += Time.deltaTime;
            PruneExpiredSegments();
        }

        private void FixedUpdate()
        {
            RenderSegments();
        }
        
        private void PruneExpiredSegments()
        {
            if (StreamSegments == null || StreamSegments.Count == 0)
            {
                return;
            }
            
            StreamSegments.RemoveAll(x => x== null);
            
            // var itemRemoved = true;
            // while (itemRemoved)
            // {
            //     // itemRemoved = false;
            //     //TODO: remove. have each segment handle its own lifespan. Visualizer is only responsible for instantiating and maintaining references
            //     // StreamSegment last = StreamSegments.Last();
            //     // DateTime epoch = DateTime.Now - TimeSpan.FromSeconds(TimeWindow);
            //     // if (last.End < epoch)
            //     // {
            //     //     StreamSegments.RemoveAt(StreamSegments.Count - 1);
            //     //     itemRemoved = true;
            //     // }
            // }
        }

        private void RenderSegments()
        {
            // Vector2 size = rectTransform.rect.size;
            // Vector2 pivot = rectTransform.pivot;
            
            // _vertices.Clear();
            // foreach (StreamSegmentModel model in StreamSegments.Select(segment => segment.Model))
            // {
            //     Vector2 xBounds = MapTimeSpanToPercent(model);
            //     var verts = new UIVertex[6];
            //     verts[0].position = new Vector2(xBounds.x, -pivot.y * size.y);
            //     verts[0].color = Color.black;
            //     verts[1].position = new Vector2(xBounds.x, (1f - pivot.y) * size.y);
            //     verts[1].color = Color;
            //     verts[2].position = new Vector2(xBounds.y, (1f - pivot.y) * size.y);
            //     verts[2].color = Color;
            //     verts[3] = verts[2];
            //     verts[4].position = new Vector2(xBounds.y, -pivot.y * size.y);
            //     verts[4].color = Color.black;
            //     verts[5] = verts[0];
            //     _vertices.AddRange(verts); // Add 2 triangles to make the quad
            //     SetVerticesDirty();
            // }
        }

        // private Vector2 MapTimeSpanToPercent(StreamSegmentModel model)
        // {
        //     Vector2 size = rectTransform.rect.size;
        //     Vector2 pivot = rectTransform.pivot;
        //
        //     var segmentDuration = (float)(model.End - model.Start).TotalSeconds;
        //     if (segmentDuration == 0f)
        //     {
        //         return Vector2.zero;
        //     }
        //
        //     var startOffset = (float)(model.Start - (DateTime.Now - TimeSpan.FromSeconds(TimeWindow))).TotalSeconds;
        //     float bound = pivot.x * size.x;
        //     var result = new Vector2(
        //         math.remap(0f, 1f, -bound, bound, startOffset / TimeWindow), 
        //         math.remap(0f, 1f, -bound, bound, (startOffset + segmentDuration) / TimeWindow));
        //     return result;
        // }

        public void StitchLastSegment(StreamSegmentModel modelToStitch)
        {
            StreamSegment lastSegment = StreamSegments[^1];
            lastSegment.Model = new StreamSegmentModel
            {
                Start = lastSegment.Model.Start,
                End = modelToStitch.End
            };
        }

        public void GenerateSegment(StreamSegment prefab, StreamSegmentModel model)
        {
            StreamSegment segment = Instantiate(prefab, transform);
            segment.transform.localPosition = Vector3.zero;
            segment.VisualizerContext = this;
            segment.Model = model;
            //TODO: map image size to parent window rect. clamp vertical, map duration to width. left-anchored so it will only grow to the right when width is changed
            //TODO: also need a scroll that will displace its position backwards one second's worth for every second it's alive
        }
    }
}
