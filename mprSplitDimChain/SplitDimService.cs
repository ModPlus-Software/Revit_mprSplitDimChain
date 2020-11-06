namespace mprSplitDimChain
{
    using System;
    using System.Linq;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;
    using ModPlusAPI;
    using ModPlusAPI.Windows;

    /// <summary>
    /// Сервис разбивки размеров
    /// </summary>
    public class SplitDimService
    {
        private readonly UIApplication _application;

        /// <summary>
        /// Initializes a new instance of the <see cref="SplitDimService"/> class.
        /// </summary>
        /// <param name="application"><see cref="UIApplication"/></param>
        public SplitDimService(UIApplication application)
        {
            _application = application;
        }

        /// <summary>
        /// Выполнить процедуру разбивки размерных цепочек
        /// </summary>
        public void SplitExecute()
        {
            var selection = _application.ActiveUIDocument.Selection;
            var doc = _application.ActiveUIDocument.Document;

            try
            {
                var dimensions = selection
                    .PickElementsByRectangle(new DimensionsFilter(), Language.GetItem("h1"))
                    .OfType<Dimension>()
                    .ToList();

                if (!dimensions.Any())
                    return;

                var transactionName = Language.GetFunctionLocalName(new ModPlusConnector());
                if (string.IsNullOrEmpty(transactionName))
                    transactionName = "Merge dimensions";

                using (var t = new TransactionGroup(doc, transactionName))
                {
                    t.Start();

                    foreach (var dimension in dimensions)
                    {
                        try
                        {
                            if (!(dimension.Curve is Line line))
                                continue;
                            using (var tr = new Transaction(doc, "Split dims"))
                            {
                                tr.Start();

                                for (var i = 0; i < dimension.NumberOfSegments; i++)
                                {
                                    var segment = dimension.Segments.get_Item(i);

                                    var referenceLeft = dimension.References.get_Item(i);
                                    {
                                        if (referenceLeft.ElementId != ElementId.InvalidElementId &&
                                            doc.GetElement(referenceLeft.ElementId) is Grid grid)
                                            referenceLeft = GetReferenceFromGrid(grid);
                                    }
                                    
                                    var referenceRight = dimension.References.get_Item(i + 1);
                                    {
                                        if (referenceRight.ElementId != ElementId.InvalidElementId &&
                                            doc.GetElement(referenceRight.ElementId) is Grid grid)
                                            referenceRight = GetReferenceFromGrid(grid);
                                    }

                                    var refArray = new ReferenceArray();
                                    refArray.Append(referenceLeft);
                                    refArray.Append(referenceRight);

                                    var createdDimension = doc.Create.NewDimension(
                                        dimension.View, line, refArray, dimension.DimensionType);
                                    
                                    if (createdDimension != null)
                                    {
                                        createdDimension.Prefix = segment.Prefix;
                                        createdDimension.Suffix = segment.Suffix;
                                        createdDimension.Above = segment.Above;
                                        createdDimension.Below = segment.Below;
                                        createdDimension.TextPosition = segment.TextPosition;
                                    }
                                }

                                doc.Delete(dimension.Id);

                                tr.Commit();
                            }
                        }
                        catch (Exception exception)
                        {
                            ExceptionBox.Show(exception);
                        }
                    }

                    t.Assimilate();
                }
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                // ignore
            }
        }

        private static Reference GetReferenceFromGrid(Grid grid)
        {
            var optionsAllGeometry = new Options
            {
                ComputeReferences = true,
                View = grid.Document.ActiveView,
                IncludeNonVisibleObjects = true
            };
            var wasException = false;
            try
            {
                var geometry = grid.get_Geometry(optionsAllGeometry).GetTransformed(Transform.Identity);
                if (geometry != null)
                {
                    foreach (var geometryObject in geometry)
                    {
                        if (geometryObject is Line line && line.Reference != null)
                        {
                            return line.Reference;
                        }
                    }
                }
            }
            catch
            {
                wasException = true;
            }

            if (wasException)
            {
                try
                {
                    var gridLine = grid.Curve as Line;
                    if (gridLine != null && gridLine.Reference != null)
                    {
                        return gridLine.Reference;
                    }
                }
                catch (Exception exception)
                {
                    ExceptionBox.Show(exception);
                }
            }

            return null;
        }
    }
}
