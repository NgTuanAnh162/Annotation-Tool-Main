using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Globalization;
using System.Diagnostics;
namespace Annotation
{
    public partial class frmPolygon : Form
    {
        #region Initialization
        private List<Polygon>   polygons = new List<Polygon>();
        private Polygon         currentPolygon = null;
        private Polygon         selectedPolygon = null;
        private bool            isDrawing, isDragging = false;
        private bool            isMoving = true;
        private Point           moveStart;
        private Point           dragStart;
        private int             dragIndex = -1;
        private bool            deleteRequested = false;

        private string          rootDir = System.Environment.CurrentDirectory;
        private string          datasetDirectory;
        private int             currentIndex = 0;
        private List<string>    imageFiles;
        private string          currentAnnotationFile = "";
        private ushort          classID;
        public frmPolygon()
        {
            InitializeComponent();
            this.KeyPreview = true;
            UpdateState();
            imageFiles = new List<string>();
            datasetDirectory = Path.Combine(rootDir, "images");
            loadDataset(datasetDirectory);
        }
        #endregion
        #region Mouse Event
        private void PictureBox1_MouseDoubleClick(object sender, EventArgs e)
        {
            if (currentPolygon != null)
            {
                currentPolygon.IsComplete = true;
                polygons.Add(currentPolygon);
                currentPolygon = null;
                pictureBox1.Invalidate();
            }
        }

        private void PictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (isMoving)
            {
                selectedPolygon = polygons.FirstOrDefault(p => p.IsInside(e.Location));
                if (selectedPolygon != null)
                {
                    selectedPolygon.IsSelected = true;
                    moveStart = e.Location;
                    pictureBox1.Cursor = Cursors.SizeAll;
                }
            }
            else if (isDragging)
            {
                selectedPolygon = polygons.FirstOrDefault(p => p.GetPointIndex(e.Location, out dragIndex));
                if (selectedPolygon != null)
                {
                    selectedPolygon.IsSelected = true;
                    dragStart = e.Location;
                    pictureBox1.Cursor = Cursors.Hand;
                }
            }
            else
            {
                if (currentPolygon == null)
                {
                    currentPolygon = new Polygon();
                }
                currentPolygon.AddPoint(e.Location);
                pictureBox1.Invalidate();
            }

            foreach (var polygon in polygons)
            {
                if (polygon != selectedPolygon)
                {
                    polygon.IsSelected = false;
                }
            }
            pictureBox1.Invalidate();
        }

        private void PictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (isMoving && selectedPolygon != null && e.Button == MouseButtons.Left)
            {
                var dx = e.X - moveStart.X;
                var dy = e.Y - moveStart.Y;
                selectedPolygon.Move(dx, dy, pictureBox1.ClientSize);
                moveStart = e.Location;
                pictureBox1.Invalidate();
            }
            else if (isDragging && selectedPolygon != null && e.Button == MouseButtons.Left && dragIndex != -1)
            {
                var newLocation = new Point(
                    Math.Max(0, Math.Min(e.X, pictureBox1.ClientSize.Width)),
                    Math.Max(0, Math.Min(e.Y, pictureBox1.ClientSize.Height))
                );
                selectedPolygon.Points[dragIndex] = newLocation;
                pictureBox1.Invalidate();
            }
        }

        private void PictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            selectedPolygon = null;
            dragIndex = -1;
            pictureBox1.Cursor = Cursors.Default;
        }
        #endregion
        #region Form event
        private void PictureBox1_Paint(object sender, PaintEventArgs e)
        {
            foreach (var polygon in polygons)
            {
                polygon.Draw(e.Graphics);
            }
            currentPolygon?.Draw(e.Graphics);
        }
        private void frmPolygon_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete && selectedPolygon != null)
            {
                deleteRequested = true;
                polygons.Remove(selectedPolygon);
                selectedPolygon = null;
                deleteRequested = false;
                pictureBox1.Invalidate(); // Redraw the PictureBox to reflect changes
            }
        }

        private void loadDatasetButton_Click(object sender, EventArgs e)
        {
            using (var folderBrowserDialog = new FolderBrowserDialog())
            {
                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    var folderPath = folderBrowserDialog.SelectedPath;
                    loadDataset(folderPath);
                    //imageFiles = Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly)
                    //                      .Where(f => f.EndsWith(".jpg") || f.EndsWith(".jpeg") || f.EndsWith(".png"))
                    //                      .ToList();

                    //if (imageFiles.Any())
                    //{
                    //    currentIndex = 0;
                    //    DisplayCurrentImage();
                    //}
                    //else
                    //{
                    //    MessageBox.Show("No image files found in the selected folder.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    //}
                }
            }
        }
        private void loadDataset(string folderPath)
        {
            try
            {
                imageFiles = Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly)
                              .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                                          f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                                          f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                                          f.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase))
                              .ToList();

                if (imageFiles.Any())
                {
                    currentIndex = 0;
                    DisplayCurrentImage();
                }
                else
                {
                    MessageBox.Show("No image files found in the selected folder.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error accessing the folder: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Debug.WriteLine($"Error accessing the folder: {ex.Message}");
            }
        }
        private void DisplayCurrentImage()
        {
            if (imageFiles.Any() && currentIndex >= 0 && currentIndex < imageFiles.Count)
            {
                pictureBox1.Image = ResizeImage(Image.FromFile(imageFiles[currentIndex]), pictureBox1.Size);
                sequenceNumber.Text = $"{currentIndex + 1}/{imageFiles.Count}";

                currentAnnotationFile = Path.ChangeExtension(imageFiles[currentIndex], ".txt");
                // Check if the annotation file exists
                if (!File.Exists(currentAnnotationFile))
                {
                    // Create an empty .txt file with the same name as the image
                    File.Create(currentAnnotationFile).Dispose(); // Use Dispose to close the file handle
                }
                LoadPolygonsFromYoloFile(currentAnnotationFile);
            }
        }

        private Image ResizeImage(Image image, Size size)
        {
            var newImage = new Bitmap(size.Width, size.Height);
            using (var graphics = Graphics.FromImage(newImage))
            {
                graphics.DrawImage(image, 0, 0, size.Width, size.Height);
            }
            return newImage;
        }

        private void previousButton_Click(object sender, EventArgs e)
        {
            if (currentIndex > 0)
            {
                savePolygonsToFile(currentAnnotationFile);
                currentIndex--;
                DisplayCurrentImage();
            }
        }

        private void nextButton_Click(object sender, EventArgs e)
        {
            if (currentIndex < imageFiles.Count - 1)
            {
                savePolygonsToFile(currentAnnotationFile);
                currentIndex++;
                DisplayCurrentImage();
            }
        }

        private void frmPolygon_Resize(object sender, EventArgs e)
        {
            DisplayCurrentImage(); // Resize the image when the form is resized
        }

        private void drawButton_Click(object sender, EventArgs e)
        {
            isDrawing = true;
            isMoving = isDragging = false;
            UpdateState();
        }

        private void moveButton_Click(object sender, EventArgs e)
        {
            isMoving = true;
            isDragging = isDrawing = false;
            UpdateState();
        }

        private void dragButton_Click(object sender, EventArgs e)
        {
            isDragging = true;
            isMoving = isDrawing = false;
            UpdateState();
        }

        private void UpdateState()
        {
            var buttonStateMap = new Dictionary<Button, bool>
                {
                    { drawButton, isDrawing },
                    { dragButton, isDragging },
                    { moveButton, isMoving }
                };
            // Update the button colors based on the state
            foreach (var buttonState in buttonStateMap)
            {
                buttonState.Key.BackColor = buttonState.Value ? Color.LightSkyBlue : Color.Transparent;
            }
        }
        #endregion
        #region Save and Load annotation file
        private void saveButton_Click(object sender, EventArgs e)
        {
            if(currentAnnotationFile != "")
            savePolygonsToFile(currentAnnotationFile);
        }

        public void savePolygonsToFile(string filePath)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    foreach (var polygon in polygons)
                    {
                        // Convert the points to YOLO format with classID
                        string yoloFormattedPoints = ConvertToYoloFormat(polygon.Points, 0); // Here, 0 is the classID
                        writer.WriteLine(yoloFormattedPoints);
                    }
                }
                //MessageBox.Show("Polygons saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving polygons: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string ConvertToYoloFormat(List<Point> points, int classID)
        {
            // Convert points to YOLO format and prepend the classID
            string pointsString = string.Join(" ", points.Select(p =>
                $"{((double)p.X / pictureBox1.Width).ToString(CultureInfo.InvariantCulture)} " +
                $"{((double)p.Y / pictureBox1.Height).ToString(CultureInfo.InvariantCulture)}"
            ));
            return $"{classID} {pointsString}";
        }
        public void LoadPolygonsFromYoloFile(string filePath)
        {
            try
            {
                polygons.Clear();
                using (StreamReader reader = new StreamReader(filePath))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        var parts = line.Split(' ');
                        // Parse the classID
                        int classID = int.Parse(parts[0]);
                        // Parse coordinates and convert from YOLO format to pixel coordinates
                        var points = parts.Skip(1)
                                          .Select((s, i) => new { Value = double.Parse(s, CultureInfo.InvariantCulture), Index = i })
                                          .GroupBy(x => x.Index / 2)
                                          .Select(g => new Point(
                                              (int)(g.First(x => x.Index % 2 == 0).Value * pictureBox1.Width),
                                              (int)(g.First(x => x.Index % 2 == 1).Value * pictureBox1.Height)
                                          ))
                                          .ToList();
                        Polygon polygon = new Polygon
                        {
                            ClassID = classID,
                            Points = points,
                            IsComplete = true
                        };
                        polygons.Add(polygon);
                    }
                }
                pictureBox1.Invalidate();
                //MessageBox.Show("Polygons loaded successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading polygons: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        #endregion

        #region Polygon properties
        public class Polygon
        {
            public int ClassID { get; set; }
            //public List<Point> Points { get; private set; } = new List<Point>();
            public List<Point> Points { get; set; } = new List<Point>();
            private const int pointRadius = 5;
            public bool IsComplete { get; set; } = false;
            public bool IsSelected { get; set; } = false;
            public void AddPoint(Point p)
            {
                Points.Add(p);
            }
            public void Draw(Graphics g)
            {
                if (Points.Count > 1)
                {
                    //Pen pen = IsSelected ? Pens.Blue : Pens.Black;
                    //g.DrawPolygon(pen, Points.ToArray());
                    g.DrawPolygon(IsSelected ? Pens.Blue : Pens.Black, Points.ToArray());
                }
                foreach (var point in Points)
                {
                    g.FillRectangle(IsComplete ? Brushes.Blue : Brushes.Red, point.X - pointRadius, point.Y - pointRadius, pointRadius * 2, pointRadius * 2);
                }
            }
            public bool IsInside(Point p)
            {
                using (GraphicsPath path = new GraphicsPath())
                {
                    path.AddPolygon(Points.ToArray());
                    return path.IsVisible(p);
                }
            }

            public bool GetPointIndex(Point p, out int index)
            {
                index = Points.FindIndex(pt => Math.Abs(pt.X - p.X) < pointRadius && Math.Abs(pt.Y - p.Y) < pointRadius);
                return index != -1;
            }

            public void Move(int dx, int dy, Size bounds)
            {
                // Check if movement is within bounds
                if (Points.Any(pt => pt.X + dx < 0 || pt.X + dx > bounds.Width || pt.Y + dy < 0 || pt.Y + dy > bounds.Height))
                {
                    return;
                }
                for (int i = 0; i < Points.Count; i++)
                {
                    Points[i] = new Point(Points[i].X + dx, Points[i].Y + dy);
                }
            }
        }
        #endregion
    }
}