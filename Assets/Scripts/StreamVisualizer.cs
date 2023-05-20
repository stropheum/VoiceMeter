using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using VoiceMeter.Discord;

namespace VoiceMeter
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class StreamVisualizer : Graphic
    {
        public long TrackedUserId { get; set; }

        [SerializeField] private float _streamWindowInSeconds = 30f;
        [SerializeField] private DiscordVoiceListener _discordVoiceListener;

        private List<UIVertex> _vertices = new();
        private List<Vector3> _triangles = new();

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            RenderQuad(vh);
        }

        public void SetSegments(IEnumerable<VoiceReceiveEvent> segments)
        {
            Vector2 size = rectTransform.rect.size;
            Vector2 pivot = rectTransform.pivot;
            UIVertex vertex = UIVertex.simpleVert;
            
            _vertices.Clear();
            IEnumerable<VoiceReceiveEvent> filteredEvents = segments
                .Where(x => x.UserId == TrackedUserId);
            
            foreach (VoiceReceiveEvent voiceReceiveEvent in filteredEvents)
            {
                // vertex.position = new Vector2(-);
            }
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
