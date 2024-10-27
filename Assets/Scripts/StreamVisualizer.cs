using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace VoiceMeter
{
    [RequireComponent(typeof(CanvasRenderer))]
    [RequireComponent(typeof(RectTransform))]
    public class StreamVisualizer : MonoBehaviour
    {
        [field: SerializeField] public float DisplayWindowInSeconds { get; private set; } = 30f;
        [field: SerializeField] public Color Color { get; set; } = Color.blue;
        
        
        public float TimeWindow { get; set; }
        public List<StreamSegment> StreamSegments { get; } = new();
        public RectTransform RectTransform { get; private set; }


        private void Awake()
        {
            RectTransform = GetComponent<RectTransform>();
        }
        
        private void Start()
        {
            Color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 1f);
        }

        private void Update()
        {
            PruneExpiredSegments();
        }

        private void PruneExpiredSegments()
        {
            if (StreamSegments == null || StreamSegments.Count == 0)
            {
                return;
            }
            
            StreamSegments.RemoveAll(x => x== null);
        }

        public void StitchLastSegment(StreamSegmentModel modelToStitch)
        {
            StreamSegment lastSegment = StreamSegments[^1];
            lastSegment.StitchSegmentModel(modelToStitch);
        }

        public void GenerateSegment(StreamSegment prefab, StreamSegmentModel model)
        {
            Vector3 position = Vector3.zero;
            StreamSegment segment = Instantiate(prefab, transform);
            segment.Image.color = Color;
            segment.DestroyAfterInactiveSeconds = TimeWindow;
            segment.transform.localPosition = position;
            segment.VisualizerContext = this;
            segment.SetStreamSegmentModel(model);
            StreamSegments.Add(segment);
        }
    }
}
