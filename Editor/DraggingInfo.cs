using UnityEditor;

namespace TwentyFiveSlicer.TFSEditor.Editor
{
    public class DraggingInfo
    {
        private DraggingState _state = DraggingState.None;
        private int _verticalIndex = -1;
        private int _horizontalIndex = -1;
        private MouseCursor _currentDragCursor = MouseCursor.Arrow;

        public void SetDraggingState(DraggingState state, int verticalIndex = -1, int horizontalIndex = -1,
            MouseCursor dragCursor = MouseCursor.Arrow)
        {
            UnityEngine.Debug.Assert(
                state != DraggingState.Intersection || (verticalIndex != -1 && horizontalIndex != -1),
                "Intersection requires valid indices");
            _state = state;
            _verticalIndex = verticalIndex;
            _horizontalIndex = horizontalIndex;
            _currentDragCursor = dragCursor;
        }

        public (DraggingState, int, int) GetDraggingState()
        {
            return (_state, _verticalIndex, _horizontalIndex);
        }

        public MouseCursor GetCurrentDragCursor()
        {
            return _currentDragCursor;
        }

        public void ClearDraggingState()
        {
            _state = DraggingState.None;
            _verticalIndex = -1;
            _horizontalIndex = -1;
            _currentDragCursor = MouseCursor.Arrow;
        }

        public bool IsDragging()
        {
            return _state != DraggingState.None;
        }
    }
}