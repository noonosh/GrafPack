using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace GrafPack
{
    public partial class GrafPackApplication : Form
    {
        private List<Shape> shapes = new List<Shape>();
        private Shape selectedShape;
        private Point lastMousePosition;
        private bool isDragging;
        private bool isRotating;
        private Shape tempShape;
        private MenuStrip menuStrip;
        private bool isCreateMode = false;
        private bool isTextAnnotationMode = false;
        private Type shapeToCreate;
        private Point startPoint;

        public GrafPackApplication()
        {
            InitializeComponent();
            SetupMainMenu();
            this.DoubleBuffered = true;
            isDragging = false;
            lastMousePosition = new Point();
            this.WindowState = FormWindowState.Maximized;
            this.Text = "GrafPack App";
        }

        private void SetupMainMenu()
        {
            MenuStrip menuStrip = new MenuStrip();

            ToolStripMenuItem createItem = new ToolStripMenuItem("Create");
            createItem.DropDownItems.Add("Square", null, (s, e) => { isCreateMode = true; shapeToCreate = typeof(Square); });
            createItem.DropDownItems.Add("Circle", null, (s, e) => { isCreateMode = true; shapeToCreate = typeof(Circle); });
            createItem.DropDownItems.Add("Triangle", null, (s, e) => { isCreateMode = true; shapeToCreate = typeof(Triangle); });
            createItem.DropDownItems.Add("Hexagon", null, (s, e) => { isCreateMode = true; shapeToCreate = typeof(Hexagon); });
            createItem.DropDownItems.Add("Rectangle", null, (s, e) => { isCreateMode = true; shapeToCreate = typeof(Rect); });
            menuStrip.Items.Add(createItem);


            menuStrip.Items.Add(new ToolStripMenuItem("Select", null, (s, e) => StartSelect()));
            menuStrip.Items.Add(new ToolStripMenuItem("Move", null, (s, e) => StartMove()));

            ToolStripMenuItem rotateItem = new ToolStripMenuItem("Rotate");
            rotateItem.DropDownItems.Add("45 Degrees", null, (s, e) => RotateSelectedShape(45));
            rotateItem.DropDownItems.Add("90 Degrees", null, (s, e) => RotateSelectedShape(90));
            rotateItem.DropDownItems.Add("135 Degrees", null, (s, e) => RotateSelectedShape(135));
            menuStrip.Items.Add(rotateItem);

            ToolStripMenuItem textAnnotationItem = new ToolStripMenuItem("Text Annotation");
            textAnnotationItem.Click += TextAnnotation_Click;
            menuStrip.Items.Add(textAnnotationItem);

            ToolStripMenuItem deleteItem = new ToolStripMenuItem("Delete", null, (s, e) => DeleteSelectedShape());
            menuStrip.Items.Add(deleteItem);
            menuStrip.Items.Add("Exit", null, (s, e) => Close());

            ToolStripMenuItem exportItem = new ToolStripMenuItem("Export to JPEG", null, saveAsJPEGToolStripMenuItem_Click);
            exportItem.Alignment = ToolStripItemAlignment.Right;
            menuStrip.Items.Add(exportItem);

            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);
        }

        private void ExportCanvasToImage(string filename)
        {
            using (Bitmap bitmap = new Bitmap(this.ClientSize.Width, this.ClientSize.Height))
            {
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    graphics.Clear(Color.White);

                    foreach (var shape in shapes)
                    {
                        shape.Draw(graphics);
                    }
                }

                bitmap.Save(filename, System.Drawing.Imaging.ImageFormat.Jpeg);
            }
        }

        private void saveAsJPEGToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "JPEG Image|*.jpg";
                saveFileDialog.Title = "Save as JPEG";

                if (saveFileDialog.ShowDialog() == DialogResult.OK && saveFileDialog.FileName != "")
                {
                    try
                    {
                        ExportCanvasToImage(saveFileDialog.FileName);
                        MessageBox.Show("Canvas exported successfully.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to export canvas. Error: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void TextAnnotation_Click(object sender, EventArgs e)
        {
            isTextAnnotationMode = true;
        }

        private void CreateTextBoxAtLocation(Point location)
        {
            TextBox txtBox = new TextBox();
            txtBox.Location = location;
            txtBox.Width = 100; txtBox.Font = new Font("Helvetica", 16);
            txtBox.Leave += TxtBox_Leave; txtBox.KeyDown += TxtBox_KeyDown;
            this.Controls.Add(txtBox);
            txtBox.Focus();
        }

        private void TxtBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                TextBox txtBox = sender as TextBox;
                if (txtBox != null)
                {
                    Label label = new Label();
                    label.Text = txtBox.Text;
                    label.Location = txtBox.Location;
                    label.Font = txtBox.Font;
                    label.AutoSize = true;
                    this.Controls.Remove(txtBox);
                    this.Controls.Add(label);
                    txtBox.Dispose();

                    e.SuppressKeyPress = true;
                }
            }
        }

        private void TxtBox_Leave(object sender, EventArgs e)
        {
            TextBox txtBox = sender as TextBox;
            if (txtBox != null)
            {
                Label label = new Label();
                label.Text = txtBox.Text;
                label.Location = txtBox.Location;
                label.Font = txtBox.Font;
                label.AutoSize = true;
                this.Controls.Remove(txtBox);
                this.Controls.Add(label);
                txtBox.Dispose();
            }
        }



        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (isTextAnnotationMode)
            {
                CreateTextBoxAtLocation(e.Location);
                isTextAnnotationMode = false;
            }

            if (isCreateMode)
            {
                startPoint = e.Location;
                tempShape = ShapeFactory.CreateShape(shapeToCreate, startPoint);
                this.MouseMove += OnMouseMoveCreateShape;
                this.MouseUp += OnMouseUpFinalizeShape;
            }
            else
            {
                bool shapeFound = false;
                foreach (var shape in shapes)
                {
                    if (shape.ContainsPoint(e.Location))
                    {
                        if (selectedShape != null)
                        {
                            selectedShape.IsSelected = false;
                        }
                        selectedShape = shape;
                        selectedShape.IsSelected = true; shapeFound = true;

                        lastMousePosition = e.Location;
                        isDragging = true;
                        this.MouseDown += OnMouseDownStartDrag;
                        this.MouseMove += OnMouseMoveDrag;
                        this.MouseUp += OnMouseUpEndDrag;
                        break;
                    }
                }

                if (!shapeFound && selectedShape != null)
                {
                    selectedShape.IsSelected = false;
                    selectedShape = null;
                }
                Invalidate();
            }
        }


        private void OnMouseMoveCreateShape(object sender, MouseEventArgs e)
        {
            if (tempShape != null)
            {
                ((dynamic)tempShape).UpdateEndPoint(e.Location);
                this.Invalidate();
            }
        }

        private void OnMouseUpFinalizeShape(object sender, MouseEventArgs e)
        {
            if (tempShape != null)
            {
                shapes.Add(tempShape);
                tempShape = null;
                isCreateMode = false;
                this.MouseMove -= OnMouseMoveCreateShape;
                this.MouseUp -= OnMouseUpFinalizeShape;
                this.Invalidate();
            }
        }


        private void StartMove()
        {
            if (selectedShape != null)
            {
                MessageBox.Show("You can now drag the selected shape to move it.", "Move", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("No shape selected to move. Please select a shape first.", "Move Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OnMouseDownStartDrag(object sender, MouseEventArgs e)
        {
            if (selectedShape != null && selectedShape.ContainsPoint(e.Location) && !isDragging)
            {
                lastMousePosition = e.Location;
                isDragging = true;
            }
        }

        private void OnMouseMoveDrag(object sender, MouseEventArgs e)
        {
            if (isDragging && selectedShape != null)
            {
                int dx = e.X - lastMousePosition.X;
                int dy = e.Y - lastMousePosition.Y;
                selectedShape.Move(dx, dy);
                lastMousePosition = e.Location;
                Invalidate();
            }
        }

        private void OnMouseUpEndDrag(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                isDragging = false;
                this.MouseDown -= OnMouseDownStartDrag;
                this.MouseMove -= OnMouseMoveDrag;
                this.MouseUp -= OnMouseUpEndDrag;
                Invalidate();
            }
        }



        private void StartSelect()
        {
            MessageBox.Show("Select the shape to perform actions", "Shape selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
            isCreateMode = false;
        }


        private void RotateSelectedShape(float degrees)
        {
            if (selectedShape != null)
            {
                selectedShape.Rotate(degrees);
                this.Invalidate();
            }
            else
            {
                MessageBox.Show("No shape selected to rotate.", "Rotation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }


        private void DeleteSelectedShape()
        {
            if (selectedShape != null)
            {
                shapes.Remove(selectedShape);
                selectedShape = null;
                this.Invalidate();
            }
            else
            {
                MessageBox.Show("No shape selected to delete.", "Deletion Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            foreach (var shape in shapes)
            {
                shape.Draw(g);
            }
            tempShape?.Draw(g);
        }

    }

    public abstract class Shape
    {
        public abstract void Draw(Graphics g);
        public abstract bool ContainsPoint(Point p);
        public abstract void Move(int dx, int dy);
        public abstract void UpdateEndPoint(Point newEndPoint);
        public abstract void Rotate(float angle);
        public abstract Point GetCenter();
        public bool IsSelected { get; set; }
        public Point StartPoint { get; protected set; }
        public Point EndPoint { get; protected set; }
    }

    public static class ShapeFactory
    {
        public static Shape CreateShape(Type shapeType, Point start, int size = 200)
        {
            if (shapeType == typeof(Square))
            {
                return new Square(start);
            }
            else if (shapeType == typeof(Circle))
            {
                return new Circle(start);
            }
            else if (shapeType == typeof(Hexagon))
            {
                return new Hexagon(start);
            }
            else if (shapeType == typeof(Triangle))
            {
                return new Triangle(start);
            }
            else if (shapeType == typeof(Rect))
            {
                return new Rect(start);
            }

            throw new ArgumentException("Invalid shape type");
        }


    }

    public class Square : Shape
    {
        private PointF center;
        private int size;
        public Square(Point start) : base()
        {
            this.StartPoint = start;
            this.EndPoint = start; this.center = new PointF(start.X + size / 2f, start.Y + size / 2f);
            this.size = size;
        }

        public override void Draw(Graphics g)
        {
            using (Pen pen = IsSelected ? new Pen(Color.Red, 3) : new Pen(Color.Black))
            {
                int size = Math.Max(Math.Abs(EndPoint.X - StartPoint.X), Math.Abs(EndPoint.Y - StartPoint.Y));
                Rectangle rect = new Rectangle(StartPoint.X, StartPoint.Y, size, size);
                g.DrawRectangle(pen, rect);
            }
        }

        public override bool ContainsPoint(Point p)
        {
            int size = Math.Max(Math.Abs(EndPoint.X - StartPoint.X), Math.Abs(EndPoint.Y - StartPoint.Y));
            return new Rectangle(StartPoint.X, StartPoint.Y, size, size).Contains(p);
        }

        public override void Move(int dx, int dy)
        {
            StartPoint = new Point(StartPoint.X + dx, StartPoint.Y + dy);
            EndPoint = new Point(EndPoint.X + dx, EndPoint.Y + dy);
        }

        public override void UpdateEndPoint(Point newEndPoint)
        {
            EndPoint = newEndPoint;
        }

        public override void Rotate(float angle)
        {
            double radians = angle * Math.PI / 180.0;

            float cosTheta = (float)Math.Cos(radians);
            float sinTheta = (float)Math.Sin(radians);

            float newStartX = (float)(center.X + ((StartPoint.X - center.X) * cosTheta - (StartPoint.Y - center.Y) * sinTheta));
            float newStartY = (float)(center.Y + ((StartPoint.X - center.X) * sinTheta + (StartPoint.Y - center.Y) * cosTheta));
            StartPoint = new Point((int)newStartX, (int)newStartY);

            float newEndX = (float)(center.X + ((EndPoint.X - center.X) * cosTheta - (EndPoint.Y - center.Y) * sinTheta));
            float newEndY = (float)(center.Y + ((EndPoint.X - center.X) * sinTheta + (EndPoint.Y - center.Y) * cosTheta));
            EndPoint = new Point((int)newEndX, (int)newEndY);

            center = new PointF((StartPoint.X + EndPoint.X) / 2f, (StartPoint.Y + EndPoint.Y) / 2f);
        }

        public override Point GetCenter()
        {
            return Point.Round(center);
        }

    }

    public class Circle : Shape
    {
        public Circle(Point center) : base()
        {
            this.StartPoint = center;
            this.EndPoint = center;
        }

        public override void Draw(Graphics g)
        {
            int radius = (int)Math.Sqrt(Math.Pow(EndPoint.X - StartPoint.X, 2) + Math.Pow(EndPoint.Y - StartPoint.Y, 2));
            Point topLeft = new Point(StartPoint.X - radius, StartPoint.Y - radius);
            Size size = new Size(radius * 2, radius * 2);
            Rectangle rect = new Rectangle(topLeft, size);
            using (Pen pen = IsSelected ? new Pen(Color.Red, 3) : new Pen(Color.Black))
            {
                g.DrawEllipse(pen, rect);
            }
        }

        public override bool ContainsPoint(Point p)
        {
            int radius = (int)Math.Sqrt(Math.Pow(EndPoint.X - StartPoint.X, 2) + Math.Pow(EndPoint.Y - StartPoint.Y, 2));
            return (Math.Pow(p.X - StartPoint.X, 2) + Math.Pow(p.Y - StartPoint.Y, 2)) <= (radius * radius);
        }

        public override void Move(int dx, int dy)
        {
            StartPoint = new Point(StartPoint.X + dx, StartPoint.Y + dy);
            EndPoint = new Point(EndPoint.X + dx, EndPoint.Y + dy);
        }

        public override void UpdateEndPoint(Point newEndPoint)
        {
            EndPoint = newEndPoint;
        }

        public override Point GetCenter()
        {
            return StartPoint;
        }

        public override void Rotate(float angle)
        {
            throw new NotImplementedException();
        }
    }

    public class Triangle : Shape
    {
        private Point[] vertices = new Point[3];

        public Triangle(Point p1)
        {
            vertices[0] = p1; vertices[1] = p1;
            vertices[2] = p1;
        }

        public override void Draw(Graphics g)
        {
            using (Pen pen = IsSelected ? new Pen(Color.Red, 3) : new Pen(Color.Black))
            {
                g.DrawPolygon(pen, vertices);
            }
        }

        public override bool ContainsPoint(Point p)
        {
            float Area(Point a, Point b, Point c)
            {
                return Math.Abs(a.X * (b.Y - c.Y) + b.X * (c.Y - a.Y) + c.X * (a.Y - b.Y)) / 2.0f;
            }

            float areaMain = Area(vertices[0], vertices[1], vertices[2]);
            float area1 = Area(p, vertices[1], vertices[2]);
            float area2 = Area(vertices[0], p, vertices[2]);
            float area3 = Area(vertices[0], vertices[1], p);

            return Math.Abs(areaMain - (area1 + area2 + area3)) < 1e-5;
        }



        public override void Move(int dx, int dy)
        {
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i].Offset(dx, dy);
            }
        }

        public override void UpdateEndPoint(Point newEndPoint)
        {
            vertices[1] = new Point(vertices[0].X, newEndPoint.Y);
            Point midBase = new Point((vertices[0].X + vertices[1].X) / 2, (vertices[0].Y + vertices[1].Y) / 2);
            vertices[2] = new Point(midBase.X + (midBase.Y - newEndPoint.Y), midBase.Y - (newEndPoint.X - midBase.X));
        }

        public override void Rotate(float angle)
        {
            Point center = GetCenter();
            double radians = angle * Math.PI / 180.0;
            for (int i = 0; i < vertices.Length; i++)
            {
                RotatePoint(ref vertices[i], center, radians);
            }
        }

        public override Point GetCenter()
        {
            int centerX = (vertices[0].X + vertices[1].X + vertices[2].X) / 3;
            int centerY = (vertices[0].Y + vertices[1].Y + vertices[2].Y) / 3;
            return new Point(centerX, centerY);
        }

        private void RotatePoint(ref Point point, Point center, double radians)
        {
            int x = point.X - center.X;
            int y = point.Y - center.Y;
            point.X = center.X + (int)(x * Math.Cos(radians) - y * Math.Sin(radians));
            point.Y = center.Y + (int)(x * Math.Sin(radians) + y * Math.Cos(radians));
        }
    }

    public class Hexagon : Shape
    {
        private Point center;
        private int radius;
        private Point[] vertices = new Point[6];

        public Hexagon(Point center)
        {
            this.center = center;
            this.radius = 0; CalculateVertices();
        }

        private void CalculateVertices()
        {
            for (int i = 0; i < 6; i++)
            {
                double angle = Math.PI / 3 * i;
                vertices[i] = new Point(
                    center.X + (int)(radius * Math.Cos(angle)),
                    center.Y + (int)(radius * Math.Sin(angle))
                );
            }
        }

        public override void Draw(Graphics g)
        {
            using (Pen pen = IsSelected ? new Pen(Color.Red, 3) : new Pen(Color.Black))
            {
                g.DrawPolygon(pen, vertices);
            }
        }

        public override bool ContainsPoint(Point p)
        {
            int crossingNumber = 0;
            for (int i = 0, j = vertices.Length - 1; i < vertices.Length; j = i++)
            {
                if (((vertices[i].Y > p.Y) != (vertices[j].Y > p.Y)) &&
                    (p.X < (vertices[j].X - vertices[i].X) * (p.Y - vertices[i].Y) / (vertices[j].Y - vertices[i].Y) + vertices[i].X))
                    crossingNumber++;
            }
            return (crossingNumber % 2 == 1);
        }

        public override void Move(int dx, int dy)
        {
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i].X += dx;
                vertices[i].Y += dy;
            }
        }

        public override void UpdateEndPoint(Point newEndPoint)
        {
            radius = (int)Math.Sqrt(Math.Pow(newEndPoint.X - center.X, 2) + Math.Pow(newEndPoint.Y - center.Y, 2));
            CalculateVertices();
        }

        public override void Rotate(float angle)
        {
            Point center = GetCenter();
            double radians = angle * Math.PI / 180.0;

            for (int i = 0; i < vertices.Length; i++)
            {
                int x = vertices[i].X - center.X;
                int y = vertices[i].Y - center.Y;
                vertices[i].X = center.X + (int)(x * Math.Cos(radians) - y * Math.Sin(radians));
                vertices[i].Y = center.Y + (int)(x * Math.Sin(radians) + y * Math.Cos(radians));
            }
        }

        public override Point GetCenter()
        {
            int sumX = 0, sumY = 0;
            foreach (Point vertex in vertices)
            {
                sumX += vertex.X;
                sumY += vertex.Y;
            }
            return new Point(sumX / vertices.Length, sumY / vertices.Length);
        }
    }


    public class Rect : Shape
    {
        private Point topLeft;
        private Point bottomRight;

        public Rect(Point topLeft, Point bottomRight)
        {
            this.topLeft = topLeft;
            this.bottomRight = bottomRight;
            this.StartPoint = topLeft; this.EndPoint = bottomRight;
        }

        public Rect(Point start) : base()
        {
            this.topLeft = start;
            this.bottomRight = start;
        }

        public override void Draw(Graphics g)
        {
            using (Pen pen = IsSelected ? new Pen(Color.Red, 3) : new Pen(Color.Black))
            {
                g.DrawRectangle(pen, new System.Drawing.Rectangle(topLeft.X, topLeft.Y, bottomRight.X - topLeft.X, bottomRight.Y - topLeft.Y));
            }
        }

        public override bool ContainsPoint(Point p)
        {
            return p.X >= topLeft.X && p.X <= bottomRight.X && p.Y >= topLeft.Y && p.Y <= bottomRight.Y;
        }

        public override void Move(int dx, int dy)
        {
            topLeft.Offset(dx, dy);
            bottomRight.Offset(dx, dy);
        }

        public override void UpdateEndPoint(Point newEndPoint)
        {
            this.bottomRight = newEndPoint;
        }

        public override void Rotate(float angle)
        {
        }

        public override Point GetCenter()
        {
            int centerX = (topLeft.X + bottomRight.X) / 2;
            int centerY = (topLeft.Y + bottomRight.Y) / 2;
            return new Point(centerX, centerY);
        }
    }

}