using System;
using System.Collections.Generic;

namespace BombStack.Data
{
    public sealed class PieceDefinition
    {
        public PIECE_SHAPE Shape { get; }

        /// <summary>초기 방향의 네 셀. 배열 순서가 특수 셀 인덱스의 기준입니다.</summary>
        public IReadOnlyList<GridPosition> Cells { get; }

        /// <summary>회전 중심의 두 배 좌표. (3,3)은 실제 (1.5,1.5)입니다.</summary>
        public GridPosition RotationCenterTwice { get; }

        internal PieceDefinition(PIECE_SHAPE shape, GridPosition rotationCenterTwice, params GridPosition[] cells)
        {
            Shape = shape;
            RotationCenterTwice = rotationCenterTwice;
            Cells = Array.AsReadOnly(cells);
        }
    }
}
