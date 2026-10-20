using System;

namespace BombStack.Data
{
    /// <summary>폭탄 레벨은 셀에 복사하지 않고 RunBuildData에서 조회합니다.</summary>
    public readonly struct CellData
    {
        public CELL_TYPE Type { get; }
        public BOMB_TYPE BombType { get; }

        public CellData(CELL_TYPE type, BOMB_TYPE bombType = BOMB_TYPE.NONE)
        {
            if (!Enum.IsDefined(typeof(CELL_TYPE), type)) throw new ArgumentOutOfRangeException(nameof(type));
            if (!Enum.IsDefined(typeof(BOMB_TYPE), bombType)) throw new ArgumentOutOfRangeException(nameof(bombType));
            if ((type == CELL_TYPE.BOMB) != (bombType != BOMB_TYPE.NONE))
                throw new ArgumentException("B 셀에만 유효한 폭탄 종류를 지정해야 합니다.", nameof(bombType));
            Type = type;
            BombType = bombType;
        }
    }

    public sealed class BoardData
    {
        private readonly CellData[] CELLS;
        public int Width { get; }
        public int Height { get; }

        public BoardData(int width, int height)
        {
            if (width < 1) throw new ArgumentOutOfRangeException(nameof(width));
            if (height < 1) throw new ArgumentOutOfRangeException(nameof(height));
            Width = width;
            Height = height;
            CELLS = new CellData[checked(width * height)];
        }

        public bool Contains(GridPosition position) => position.X >= 0 && position.X < Width && position.Y >= 0 && position.Y < Height;

        public CellData GetCell(GridPosition position) => CELLS[GetIndex(position)];
        public void SetCell(GridPosition position, CellData cell) => CELLS[GetIndex(position)] = cell;

        private int GetIndex(GridPosition position)
        {
            if (!Contains(position)) throw new ArgumentOutOfRangeException(nameof(position));
            return position.Y * Width + position.X;
        }
    }
}
