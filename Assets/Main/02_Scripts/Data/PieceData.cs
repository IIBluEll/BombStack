using System;

namespace BombStack.Data
{
    /// <summary>회전 전 생성 정보. 특수 셀은 Cells의 인덱스로 추적하므로 O도 위치가 회전합니다.</summary>
    public sealed class PieceData
    {
        public PIECE_SHAPE Shape { get; }
        public CELL_TYPE Attribute { get; }
        public int SpecialCellIndex { get; }
        public BOMB_TYPE BombType { get; }

        public PieceData(PIECE_SHAPE shape, CELL_TYPE attribute, int specialCellIndex = -1, BOMB_TYPE bombType = BOMB_TYPE.NONE)
        {
            if (!Enum.IsDefined(typeof(PIECE_SHAPE), shape)) throw new ArgumentOutOfRangeException(nameof(shape));
            if (attribute != CELL_TYPE.NORMAL && attribute != CELL_TYPE.BOMB && attribute != CELL_TYPE.TRIGGER)
                throw new ArgumentOutOfRangeException(nameof(attribute));
            if (attribute == CELL_TYPE.NORMAL ? specialCellIndex != -1 : specialCellIndex < 0 || specialCellIndex > 3)
                throw new ArgumentOutOfRangeException(nameof(specialCellIndex));
            // CellData와 같은 B/종류 정합성을 적용합니다.
            _ = new CellData(attribute, bombType);
            Shape = shape;
            Attribute = attribute;
            SpecialCellIndex = specialCellIndex;
            BombType = bombType;
        }

        public CellData GetCell(int index)
        {
            if (index < 0 || index > 3) throw new ArgumentOutOfRangeException(nameof(index));
            return index == SpecialCellIndex ? new CellData(Attribute, BombType) : new CellData(CELL_TYPE.NORMAL);
        }
    }

    public sealed class ActivePieceData
    {
        public PieceData Piece { get; }
        public GridPosition Origin { get; set; }
        public PIECE_ROTATION Rotation { get; set; }
        public float FallElapsedSeconds { get; set; }
        public float LockElapsedSeconds { get; set; }
        public bool HasTouchedGround { get; set; }

        public ActivePieceData(PieceData piece, GridPosition origin)
        {
            Piece = piece ?? throw new ArgumentNullException(nameof(piece));
            Origin = origin;
        }
    }
}
