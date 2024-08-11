//using System;
//using System.Drawing;
//using System.IO;
//using System.Collections.Generic;
//using System.Windows.Forms;

//namespace Annotation
//{
//    public partial class frmRectangle : Form
//    {
//        private class RotatedRectangle
//        {
//            public PointF TopLeft { get; set; }
//            public PointF TopRight { get; set; }
//            public PointF BottomLeft { get; set; }
//            public PointF BottomRight { get; set; }

//            public RotatedRectangle(PointF topLeft, PointF topRight, PointF bottomLeft, PointF bottomRight)
//            {
//                TopLeft = topLeft;
//                TopRight = topRight;
//                BottomLeft = bottomLeft;
//                BottomRight = bottomRight;
//            }
//        }

//        private class RectangleInfo
//        {
//            public int Id { get; set; }
//            public PointF TopLeft { get; set; }
//            public PointF TopRight { get; set; }
//            public PointF BottomLeft { get; set; }
//            public PointF BottomRight { get; set; }
//        }

//        private enum DragMode
//        {
//            None,
//            Move,
//            Rotate,
//            ResizeTopLeft,
//            ResizeTopRight,
//            ResizeBottomLeft,
//            ResizeBottomRight
//        }

//        private List<RotatedRectangle> rectangles = new List<RotatedRectangle>();
//        private bool isDrawing = false;
//        private bool isNewDraw = false;
//        private DragMode dragMode = DragMode.None;
//        private Rectangle rect;
//        private Point startPoint;
//        private Point selectedOffset;
//        private const int HANDLE_SIZE = 5;
//        private PointF rotationCenter;
//        private RotatedRectangle selectedRectangle;
//        private List<RectangleInfo> rectangleInfos = new List<RectangleInfo>();

//        public frmRectangle()
//        {
//            InitializeComponent();
//            string RootImageFolder = Environment.CurrentDirectory + "\\images\\";
//            string CurrentAnnotationFile = RootImageFolder + "Annotations.txt";
//            LoadRectanglesFromFile(CurrentAnnotationFile);
//        }

//        private void PictureBox1_MouseDown(object sender, MouseEventArgs e)
//        {
//            selectedRectangle = null;

//            foreach (var r in rectangles)
//            {
//                if (IsHandleClicked(e.Location, r.TopRight))
//                {
//                    dragMode = DragMode.Rotate;
//                    rotationCenter = new PointF((r.TopLeft.X + r.TopRight.X + r.BottomLeft.X + r.BottomRight.X) / 4,
//                                                (r.TopLeft.Y + r.TopRight.Y + r.BottomLeft.Y + r.BottomRight.Y) / 4);
//                    selectedRectangle = r;
//                    break;
//                }
//                if (IsTopLeftResizeHandleClicked(e.Location, r.TopLeft))
//                {
//                    dragMode = DragMode.ResizeTopLeft;
//                    selectedRectangle = r;
//                    break;
//                }
//                if (IsTopRightResizeHandleClicked(e.Location, r.TopRight))
//                {
//                    dragMode = DragMode.ResizeTopRight;
//                    selectedRectangle = r;
//                    break;
//                }
//                if (IsBottomLeftResizeHandleClicked(e.Location, r.BottomLeft))
//                {
//                    dragMode = DragMode.ResizeBottomLeft;
//                    selectedRectangle = r;
//                    break;
//                }
//                if (IsBottomRightResizeHandleClicked(e.Location, r.BottomRight))
//                {
//                    dragMode = DragMode.ResizeBottomRight;
//                    selectedRectangle = r;
//                    break;
//                }
//                if (IsPointInPolygon(e.Location, new PointF[] { r.TopLeft, r.TopRight, r.BottomRight, r.BottomLeft }))
//                {
//                    dragMode = DragMode.Move;
//                    selectedOffset = new Point((int)(e.X - (r.TopLeft.X + r.TopRight.X + r.BottomLeft.X + r.BottomRight.X) / 4),
//                                               (int)(e.Y - (r.TopLeft.Y + r.TopRight.Y + r.BottomLeft.Y + r.BottomRight.Y) / 4));
//                    selectedRectangle = r;
//                    break;
//                }
//            }

//            if (selectedRectangle == null)
//            {
//                isDrawing = true;
//                startPoint = e.Location;
//                rect = new Rectangle(e.Location, new Size(0, 0));
//            }

//            pictureBox1.Invalidate();
//        }

//        private void PictureBox1_MouseMove(object sender, MouseEventArgs e)
//        {
//            if (isDrawing)
//            {
//                isNewDraw = true;
//                Point endPoint = e.Location;
//                rect = new Rectangle(
//                    Math.Min(startPoint.X, endPoint.X),
//                    Math.Min(startPoint.Y, endPoint.Y),
//                    Math.Abs(startPoint.X - endPoint.X),
//                    Math.Abs(startPoint.Y - endPoint.Y)
//                );
//            }
//            else if (selectedRectangle != null)
//            {
//                int index = rectangles.IndexOf(selectedRectangle);
//                RotatedRectangle updatedRect = selectedRectangle;

//                switch (dragMode)
//                {
//                    case DragMode.Move:
//                        float dx = e.X - (selectedRectangle.TopLeft.X + selectedRectangle.TopRight.X + selectedRectangle.BottomLeft.X + selectedRectangle.BottomRight.X) / 4 - selectedOffset.X;
//                        float dy = e.Y - (selectedRectangle.TopLeft.Y + selectedRectangle.TopRight.Y + selectedRectangle.BottomLeft.Y + selectedRectangle.BottomRight.Y) / 4 - selectedOffset.Y;
//                        updatedRect.TopLeft = new PointF(selectedRectangle.TopLeft.X + dx, selectedRectangle.TopLeft.Y + dy);
//                        updatedRect.TopRight = new PointF(selectedRectangle.TopRight.X + dx, selectedRectangle.TopRight.Y + dy);
//                        updatedRect.BottomLeft = new PointF(selectedRectangle.BottomLeft.X + dx, selectedRectangle.BottomLeft.Y + dy);
//                        updatedRect.BottomRight = new PointF(selectedRectangle.BottomRight.X + dx, selectedRectangle.BottomRight.Y + dy);
//                        break;
//                    case DragMode.ResizeTopLeft:
//                        updatedRect.TopLeft = new PointF(e.X, e.Y);
//                        break;
//                    case DragMode.ResizeTopRight:
//                        updatedRect.TopRight = new PointF(e.X, e.Y);
//                        break;
//                    case DragMode.ResizeBottomLeft:
//                        updatedRect.BottomLeft = new PointF(e.X, e.Y);
//                        break;
//                    case DragMode.ResizeBottomRight:
//                        updatedRect.BottomRight = new PointF(e.X, e.Y);
//                        break;
//                    case DragMode.Rotate:
//                        // Logic for rotation can be added here
//                        break;
//                }
//                rectangles[index] = updatedRect;
//                selectedRectangle = updatedRect;
//            }

//            pictureBox1.Invalidate();
//        }

//        private void PictureBox1_MouseUp(object sender, MouseEventArgs e)
//        {
//            if (isDrawing)
//            {
//                isDrawing = false;
//                isNewDraw = false;
//                if (rect.Width > 0 && rect.Height > 0)
//                {
//                    rectangles.Add(new RotatedRectangle(
//                        new PointF(rect.Left, rect.Top),
//                        new PointF(rect.Right, rect.Top),
//                        new PointF(rect.Left, rect.Bottom),
//                        new PointF(rect.Right, rect.Bottom)));
//                }
//            }
//            dragMode = DragMode.None;
//            selectedRectangle = null;
//            pictureBox1.Invalidate();
//        }

//        private bool IsHandleClicked(Point clickPoint, PointF handlePoint)
//        {
//            return Math.Abs(clickPoint.X - handlePoint.X) < HANDLE_SIZE && Math.Abs(clickPoint.Y - handlePoint.Y) < HANDLE_SIZE;
//        }

//        private bool IsTopLeftResizeHandleClicked(Point clickPoint, PointF handlePoint)
//        {
//            return Math.Abs(clickPoint.X - handlePoint.X) < HANDLE_SIZE && Math.Abs(clickPoint.Y - handlePoint.Y) < HANDLE_SIZE;
//        }

//        private bool IsTopRightResizeHandleClicked(Point clickPoint, PointF handlePoint)
//        {
//            return Math.Abs(clickPoint.X - handlePoint.X) < HANDLE_SIZE && Math.Abs(clickPoint.Y - handlePoint.Y) < HANDLE_SIZE;
//        }

//        private bool IsBottomLeftResizeHandleClicked(Point clickPoint, PointF handlePoint)
//        {
//            return Math.Abs(clickPoint.X - handlePoint.X) < HANDLE_SIZE && Math.Abs(clickPoint.Y - handlePoint.Y) < HANDLE_SIZE;
//        }

//        private bool IsBottomRightResizeHandleClicked(Point clickPoint, PointF handlePoint)
//        {
//            return Math.Abs(clickPoint.X - handlePoint.X) < HANDLE_SIZE && Math.Abs(clickPoint.Y - handlePoint.Y) < HANDLE_SIZE;
//        }

//        private void PictureBox1_Paint(object sender, PaintEventArgs e)
//        {
//            if (isNewDraw == true && rect != null && rect.Width > 0 && rect.Height > 0)
//            {
//                e.Graphics.DrawRectangle(Pens.Black, rect);
//                e.Graphics.FillRectangle(Brushes.Red, rect.Right - HANDLE_SIZE / 2, rect.Top - HANDLE_SIZE / 2, HANDLE_SIZE, HANDLE_SIZE);
//                isNewDraw = false;
//            }

//            rectangleInfos.Clear();
//            int id = 0;

//            foreach (var r in rectangles)
//            {
//                PointF[] points = { r.TopLeft, r.TopRight, r.BottomRight, r.BottomLeft };
//                e.Graphics.DrawPolygon(Pens.Black, points);

//                rectangleInfos.Add(new RectangleInfo
//                {
//                    Id = id++,
//                    TopLeft = r.TopLeft,
//                    TopRight = r.TopRight,
//                    BottomLeft = r.BottomLeft,
//                    BottomRight = r.BottomRight
//                });

//                e.Graphics.FillRectangle(Brushes.Red, r.TopRight.X - HANDLE_SIZE / 2, r.TopRight.Y - HANDLE_SIZE / 2, HANDLE_SIZE, HANDLE_SIZE);
//                e.Graphics.FillRectangle(Brushes.Blue, r.TopLeft.X - HANDLE_SIZE / 2, r.TopLeft.Y - HANDLE_SIZE / 2, HANDLE_SIZE, HANDLE_SIZE);
//                e.Graphics.FillRectangle(Brushes.Blue, r.BottomRight.X - HANDLE_SIZE / 2, r.BottomRight.Y - HANDLE_SIZE / 2, HANDLE_SIZE, HANDLE_SIZE);
//                e.Graphics.FillRectangle(Brushes.Blue, r.BottomLeft.X - HANDLE_SIZE / 2, r.BottomLeft.Y - HANDLE_SIZE / 2, HANDLE_SIZE, HANDLE_SIZE);
//            }
//        }

//        private bool IsPointInPolygon(Point point, PointF[] polygon)
//        {
//            int polygonLength = polygon.Length, i = 0;
//            bool inside = false;
//            float pointX = point.X, pointY = point.Y;
//            float startX, startY, endX, endY;
//            PointF endPoint = polygon[polygonLength - 1];
//            endX = endPoint.X;
//            endY = endPoint.Y;
//            while (i < polygonLength)
//            {
//                startX = endX; startY = endY;
//                endPoint = polygon[i++];
//                endX = endPoint.X; endY = endPoint.Y;
//                inside ^= (endY > pointY ^ startY > pointY) && ((pointX - endX) < (pointY - endY) * (startX - endX) / (startY - endY));
//            }
//            return inside;
//        }

//        private void LoadRectanglesFromFile(string filePath)
//        {
//            rectangles.Clear();

//            var lines = File.ReadAllLines(filePath);

//            foreach (var line in lines)
//            {
//                if (!string.IsNullOrWhiteSpace(line))
//                {
//                    var parts = line.Split(' ');

//                    if (parts.Length == 9)
//                    {
//                        int classIndex = int.Parse(parts[0]);
//                        float x1 = float.Parse(parts[1]);
//                        float y1 = float.Parse(parts[2]);
//                        float x2 = float.Parse(parts[3]);
//                        float y2 = float.Parse(parts[4]);
//                        float x3 = float.Parse(parts[5]);
//                        float y3 = float.Parse(parts[6]);
//                        float x4 = float.Parse(parts[7]);
//                        float y4 = float.Parse(parts[8]);

//                        PointF topLeft = new PointF(x1, y1);
//                        PointF topRight = new PointF(x2, y2);
//                        PointF bottomLeft = new PointF(x3, y3);
//                        PointF bottomRight = new PointF(x4, y4);

//                        rectangles.Add(new RotatedRectangle(topLeft, topRight, bottomLeft, bottomRight));
//                    }
//                }
//            }

//            pictureBox1.Invalidate();
//        }
//    }
//}
