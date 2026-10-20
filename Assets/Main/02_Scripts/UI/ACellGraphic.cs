using BombStack.Data;
using UnityEngine;
using UnityEngine.UI;

namespace BombStack.UI
{
    /// <summary>보드와 다음 조각에서 공유하는 셀 도형입니다. 별도 이미지 에셋이 필요하지 않습니다.</summary>
    public abstract class ACellGraphic : MaskableGraphic
    {
        public static readonly Color NORMAL_COLOR = new Color(0.31f, 0.41f, 0.55f);
        public static readonly Color BASIC_COLOR = new Color(1f, 0.39f, 0.40f);
        public static readonly Color HORIZONTAL_COLOR = new Color(0.34f, 0.73f, 1f);
        public static readonly Color VERTICAL_COLOR = new Color(0.73f, 0.58f, 1f);
        public static readonly Color RELAY_COLOR = new Color(0.31f, 0.91f, 0.74f);
        public static readonly Color TRIGGER_COLOR = new Color(1f, 0.76f, 0.32f);
        protected static readonly Color INK = new Color(0.055f, 0.075f, 0.12f);

        public static Color GetCellColor(CellData cell)
        {
            if (cell.Type == CELL_TYPE.NORMAL) return NORMAL_COLOR;
            if (cell.Type == CELL_TYPE.TRIGGER) return TRIGGER_COLOR;
            switch (cell.BombType)
            {
                case BOMB_TYPE.B02_HORIZONTAL: return HORIZONTAL_COLOR;
                case BOMB_TYPE.B03_VERTICAL: return VERTICAL_COLOR;
                case BOMB_TYPE.B04_RELAY: return RELAY_COLOR;
                default: return BASIC_COLOR;
            }
        }

        protected static void AddRect(VertexHelper mesh, Rect rect, Color tint)
        {
            int start = mesh.currentVertCount;
            mesh.AddVert(new Vector3(rect.xMin, rect.yMin), tint, Vector2.zero);
            mesh.AddVert(new Vector3(rect.xMin, rect.yMax), tint, Vector2.zero);
            mesh.AddVert(new Vector3(rect.xMax, rect.yMax), tint, Vector2.zero);
            mesh.AddVert(new Vector3(rect.xMax, rect.yMin), tint, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start + 2, start + 3, start);
        }

        protected static void AddOutline(VertexHelper mesh, Rect rect, Color tint, float width)
        {
            AddRect(mesh, new Rect(rect.xMin, rect.yMin, rect.width, width), tint);
            AddRect(mesh, new Rect(rect.xMin, rect.yMax - width, rect.width, width), tint);
            AddRect(mesh, new Rect(rect.xMin, rect.yMin, width, rect.height), tint);
            AddRect(mesh, new Rect(rect.xMax - width, rect.yMin, width, rect.height), tint);
        }

        protected static void AddLine(VertexHelper mesh, Vector2 from, Vector2 to, Color tint, float width)
        {
            Vector2 delta = to - from;
            if (delta.sqrMagnitude < 0.001f) return;
            Vector2 side = new Vector2(-delta.y, delta.x).normalized * (width * 0.5f);
            int start = mesh.currentVertCount;
            mesh.AddVert((Vector3)(from - side), tint, Vector2.zero);
            mesh.AddVert((Vector3)(from + side), tint, Vector2.zero);
            mesh.AddVert((Vector3)(to + side), tint, Vector2.zero);
            mesh.AddVert((Vector3)(to - side), tint, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start + 2, start + 3, start);
        }

        private static void AddTriangle(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Color tint)
        {
            int start = mesh.currentVertCount;
            mesh.AddVert((Vector3)a, tint, Vector2.zero);
            mesh.AddVert((Vector3)b, tint, Vector2.zero);
            mesh.AddVert((Vector3)c, tint, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2);
        }

        private static void AddRing(VertexHelper mesh, Vector2 center, float radius, Color tint, float width)
        {
            const int SEGMENTS = 16;
            for (int i = 0; i < SEGMENTS; i++)
            {
                float angle = i * Mathf.PI * 2 / SEGMENTS;
                float next = (i + 1) * Mathf.PI * 2 / SEGMENTS;
                AddLine(mesh, center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius,
                    center + new Vector2(Mathf.Cos(next), Mathf.Sin(next)) * radius, tint, width);
            }
        }

        protected static void AddCell(VertexHelper mesh, Rect rect, CellData cell, bool ghost, bool active, bool heatproof)
        {
            if (cell.Type == CELL_TYPE.EMPTY) return;
            Color tint = GetCellColor(cell);
            Color fill = tint;
            fill.a = ghost ? 0.13f : 1f;
            AddRect(mesh, rect, fill);
            AddOutline(mesh, rect, ghost ? tint : new Color(1, 1, 1, active ? 0.95f : 0.16f), active ? 1.5f : 1f);
            Color symbol = ghost ? tint : INK;
            Vector2 center = rect.center;
            float radius = rect.width * 0.22f;
            float stroke = Mathf.Max(1.2f, rect.width * 0.06f);
            if (cell.Type == CELL_TYPE.TRIGGER)
            {
                AddTriangle(mesh, center + Vector2.up * radius, center + Vector2.left * radius, center + Vector2.right * radius, symbol);
                AddTriangle(mesh, center + Vector2.down * radius, center + Vector2.right * radius, center + Vector2.left * radius, symbol);
                if (heatproof) AddOutline(mesh, new Rect(rect.x + 3, rect.y + 3, rect.width - 6, rect.height - 6), Color.white, 1);
            }
            else if (cell.Type == CELL_TYPE.BOMB)
            {
                if (cell.BombType == BOMB_TYPE.B01_BASIC) AddRing(mesh, center, radius, symbol, stroke);
                else if (cell.BombType == BOMB_TYPE.B04_RELAY)
                {
                    AddLine(mesh, center + Vector2.down * radius, center + Vector2.up * radius, symbol, stroke);
                    AddLine(mesh, center + Vector2.up * radius, center + new Vector2(-radius, radius * 0.2f), symbol, stroke);
                    AddLine(mesh, center + Vector2.up * radius, center + new Vector2(radius, radius * 0.2f), symbol, stroke);
                    AddRect(mesh, new Rect(center.x - radius, center.y - radius, radius * 2, stroke), symbol);
                }
                else
                {
                    Vector2 axis = cell.BombType == BOMB_TYPE.B02_HORIZONTAL ? Vector2.right : Vector2.up;
                    Vector2 side = new Vector2(-axis.y, axis.x) * radius * 0.65f;
                    AddLine(mesh, center - axis * radius, center + axis * radius, symbol, stroke);
                    AddTriangle(mesh, center + axis * radius * 1.25f, center + side, center - side, symbol);
                    AddTriangle(mesh, center - axis * radius * 1.25f, center - side, center + side, symbol);
                }
            }
        }
    }
}
