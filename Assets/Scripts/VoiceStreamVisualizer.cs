using UnityEngine;
using UnityEngine.UI;
using VoiceMeter.Discord;

namespace VoiceMeter
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class VoiceStreamVisualizer : Graphic
    {
        [SerializeField] private DiscordVoiceListener _discordVoiceListener;

        protected override void Awake()
        {
            base.Awake();
            Debug.Assert(_discordVoiceListener != null);
        }
        
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            // Create vertices and colors
            Vector2 size = rectTransform.rect.size;
            Vector2 pivot = rectTransform.pivot;
            UIVertex vertex = UIVertex.simpleVert;

            // Set vertex positions and colors
            vertex.position = new Vector2(-pivot.x * size.x, -pivot.y * size.y);
            vertex.color = Color.red;
            vh.AddVert(vertex);

            vertex.position = new Vector2(-pivot.x * size.x, (1f - pivot.y) * size.y);
            vertex.color = Color.green;
            vh.AddVert(vertex);

            vertex.position = new Vector2((1f - pivot.x) * size.x, (1f - pivot.y) * size.y);
            vertex.color = Color.blue;
            vh.AddVert(vertex);

            vertex.position = new Vector2((1f - pivot.x) * size.x, -pivot.y * size.y);
            vertex.color = Color.white;
            vh.AddVert(vertex);

            // Create triangles
            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }
    }
}
