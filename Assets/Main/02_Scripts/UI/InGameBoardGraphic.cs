using BombStack.Data;
using UnityEngine;
using UnityEngine.UI;

namespace BombStack.UI
{
    public sealed class InGameBoardGraphic : ACellGraphic
    {
        private InGameUIModel_model _model;
        [SerializeField] private bool _drawEmptyCells = true;

        public void SetEmptyCellBackground(bool drawEmptyCells)
        {
            _drawEmptyCells = drawEmptyCells;
            SetVerticesDirty();
        }

        public void SetModel(InGameUIModel_model model)
        {
            _model = model;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (_model?.Board == null) return;
            BoardData board = _model.Board;
            Rect bounds = GetPixelAdjustedRect();
            float size = Mathf.Min(bounds.width / board.Width, bounds.height / board.Height);
            Vector2 origin = bounds.center - new Vector2(board.Width, board.Height) * (size * 0.5f);
            bool heatproof = _model.Run.Build.Relics.Contains(RELIC_TYPE.R06_HEATPROOF_TRIGGER);
            for (int y = 0; y < board.Height; y++)
                for (int x = 0; x < board.Width; x++)
                {
                    Rect cellRect = CellRect(origin, size, x, y);
                    if (_drawEmptyCells) AddRect(mesh, cellRect, new Color(0.045f, 0.065f, 0.10f));
                    AddCell(mesh, cellRect, board.GetCell(new GridPosition(x, y)), false, false, heatproof);
                }
            ActivePieceData active = _model.Run.ActivePiece;
            if (_model.GhostPositions != null)
                for (int i = 0; i < 4; i++)
                {
                    GridPosition p = _model.GhostPositions[i];
                    AddCell(mesh, CellRect(origin, size, p.X, p.Y), active.Piece.GetCell(i), true, false, heatproof);
                }

            ExplosionWaveData wave = _model.Wave;
            if (wave != null && (_model.PlaybackPhase == -1 || _model.PlaybackPhase == 1))
            {
                foreach (RemovedCellData removed in wave.RemovedCells)
                    AddOutline(mesh, CellRect(origin, size, removed.Position.X, removed.Position.Y), TRIGGER_COLOR, 1.3f);
                foreach (BombActivationData bomb in wave.ActivatedBombs)
                    AddOutline(mesh, CellRect(origin, size, bomb.Position.X, bomb.Position.Y), Color.white, 2);
                foreach (RelayConnectionData link in wave.RelayConnections)
                {
                    Vector2 from = origin + new Vector2(link.Source.X + 0.5f, link.Source.Y + 0.5f) * size;
                    Vector2 to = origin + new Vector2(link.Target.X + 0.5f, link.Target.Y + 0.5f) * size;
                    for (int dot = 1; dot < 8; dot++)
                    {
                        Vector2 point = Vector2.Lerp(from, to, dot / 8f);
                        AddRect(mesh, new Rect(point.x - 1.5f, point.y - 1.5f, 3, 3), RELAY_COLOR);
                    }
                }
            }
            if (_model.ActivePositions != null)
                for (int i = 0; i < 4; i++)
                {
                    GridPosition p = _model.ActivePositions[i];
                    AddCell(mesh, CellRect(origin, size, p.X, p.Y), active.Piece.GetCell(i), false, true, heatproof);
                }
            if (wave != null && _model.PlaybackPhase == 2)
                foreach (RemovedCellData removed in wave.RemovedCells)
                    AddRect(mesh, CellRect(origin, size, removed.Position.X, removed.Position.Y), new Color(1f, 0.76f, 0.32f, 0.3f));
            if (wave != null && _model.PlaybackPhase == 3)
                foreach (CellMovementData movement in wave.Movements)
                {
                    float y = Mathf.Lerp(movement.From.Y, movement.To.Y, _model.GravityProgress);
                    AddCell(mesh, CellRect(origin, size, movement.From.X, y), movement.Cell, false, false, heatproof);
                }
            if (_model.HasTopWarning)
            {
                int warningRow = _model.Definition.Board.WarningStartRow;
                AddOutline(mesh, new Rect(origin.x, origin.y + warningRow * size, board.Width * size, (board.Height - warningRow) * size), BASIC_COLOR, 2);
            }
        }

        private static Rect CellRect(Vector2 origin, float size, float x, float y) => new Rect(origin.x + x * size + 1, origin.y + y * size + 1, size - 2, size - 2);
    }
}
