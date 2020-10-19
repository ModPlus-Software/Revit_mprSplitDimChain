﻿namespace mprSplitDimChain
{
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI.Selection;

    /// <summary>
    /// Фильтр выбора размеров
    /// </summary>
    public class DimensionsFilter : ISelectionFilter
    {
        /// <inheritdoc/>
        public bool AllowElement(Element elem)
        {
            return elem is Dimension dimension && dimension.NumberOfSegments > 1;
        }

        /// <inheritdoc/>
        public bool AllowReference(Reference reference, XYZ position)
        {
            throw new System.NotImplementedException();
        }
    }
}
