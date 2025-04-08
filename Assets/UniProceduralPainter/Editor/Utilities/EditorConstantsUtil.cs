using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UniProceduralPainter.Editor
{
#if UNITY_EDITOR
    public static class EditorConstantsUtil
    {
        #region ----File Extension----
        public const string FILE_EXTENSION_PNG = ".png";
        public const string FILE_EXTENSION_EXR = ".exr";
        #endregion

        #region ----Menu----
        public const int MENU_UNIPMAT_ORDER = 114514;

        public const string MENU_CREAT_PIXELPMAT = "Assets/UniPMaterial/Create/New PixelPMaterial";
        #endregion

        #region ----Unity Editor Icons----
        //ref: https://github.com/halak/unity-editor-icons
        public const string EDIT_ICON_PMAT = "ProceduralMaterial Icon";
        #endregion
    }
#endif
}