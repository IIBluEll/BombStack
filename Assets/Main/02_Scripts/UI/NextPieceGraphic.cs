using BombStack.Data;
using UnityEngine;
using UnityEngine.UI;

namespace BombStack.UI
{
    public sealed class NextPieceGraphic : ACellGraphic
    {
        private PieceDefinition _definition;
        private PieceData _piece;
        private bool _heatproof;

        public void SetPiece(PieceDefinition definition, PieceData piece, bool heatproof)
        {
            if (_definition == definition && _piece == piece && _heatproof == heatproof) return;
            _definition = definition;
            _piece = piece;
            _heatproof = heatproof;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (_piece == null || _definition == null) return;
            Rect rect = GetPixelAdjustedRect();
            int minX = 4, minY = 4, maxX = 0, maxY = 0;
            foreach (GridPosition p in _definition.Cells)
            {
                minX = Mathf.Min(minX, p.X); minY = Mathf.Min(minY, p.Y);
                maxX = Mathf.Max(maxX, p.X); maxY = Mathf.Max(maxY, p.Y);
            }
            float size = Mathf.Min(rect.width / 4, rect.height / 3);
            Vector2 origin = rect.center - new Vector2(maxX - minX + 1, maxY - minY + 1) * (size * 0.5f);
            for (int i = 0; i < 4; i++)
            {
                GridPosition p = _definition.Cells[i];
                var cellRect = new Rect(origin.x + (p.X - minX) * size + 1, origin.y + (p.Y - minY) * size + 1, size - 2, size - 2);
                AddCell(mesh, cellRect, _piece.GetCell(i), false, false, _heatproof);
            }
        }
    }
}
