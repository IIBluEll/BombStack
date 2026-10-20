using System;
using BombStack.Data;

namespace BombStack.Core
{
    public static class BoardRules
    {
        public static GridPosition[] GetPositions(GameDefinition definition, PieceData piece, GridPosition origin, PIECE_ROTATION rotation)
        {
            PieceDefinition shape = definition.GetPiece(piece.Shape);
            var positions = new GridPosition[4];
            for (int i = 0; i < positions.Length; i++)
            {
                GridPosition p = shape.Cells[i];
                for (int turn = 0; turn < (int)rotation; turn++)
                    p = new GridPosition((shape.RotationCenterTwice.X + 2 * p.Y - shape.RotationCenterTwice.Y) / 2,
                        (shape.RotationCenterTwice.Y - 2 * p.X + shape.RotationCenterTwice.X) / 2);
                positions[i] = new GridPosition(p.X + origin.X, p.Y + origin.Y);
            }
            return positions;
        }

        public static bool CanPlace(GameDefinition definition, BoardData board, PieceData piece, GridPosition origin, PIECE_ROTATION rotation)
        {
            foreach (GridPosition p in GetPositions(definition, piece, origin, rotation))
                if (!board.Contains(p) || board.GetCell(p).Type != CELL_TYPE.EMPTY) return false;
            return true;
        }

        public static GridPosition GetLandingOrigin(GameDefinition definition, BoardData board, ActivePieceData active)
        {
            GridPosition origin = active.Origin;
            while (CanPlace(definition, board, active.Piece, new GridPosition(origin.X, origin.Y - 1), active.Rotation))
                origin = new GridPosition(origin.X, origin.Y - 1);
            return origin;
        }

        public static void Place(GameDefinition definition, BoardData board, PieceData piece, GridPosition origin, PIECE_ROTATION rotation)
        {
            if (!CanPlace(definition, board, piece, origin, rotation)) throw new InvalidOperationException("유효하지 않은 조각 고정입니다.");
            GridPosition[] positions = GetPositions(definition, piece, origin, rotation);
            for (int i = 0; i < positions.Length; i++) board.SetCell(positions[i], piece.GetCell(i));
        }

        public static BoardData Copy(BoardData board)
        {
            var copy = new BoardData(board.Width, board.Height);
            for (int y = 0; y < board.Height; y++)
                for (int x = 0; x < board.Width; x++)
                {
                    var p = new GridPosition(x, y);
                    copy.SetCell(p, board.GetCell(p));
                }
            return copy;
        }
    }
}
