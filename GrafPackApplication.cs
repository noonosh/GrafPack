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
        private Point lastMousePosition; // Store the last mouse position for dragging
        private bool isDragging;
        private Shape tempShape; // Temporary shape for dynamic creation
        private MainMenu mainMenu;
        private bool isCreateMode = false;
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
        }

        private void SetupMainMenu()
        {
            mainMenu = new MainMenu();
            var createItem = new MenuItem("Create");
            createItem.MenuItems.Add("Square", (s, e) => { isCreateMode = true; shapeToCreate = typeof(Square); });
            createItem.MenuItems.Add("Circle", (s, e) => { isCreateMode = true; shapeToCreate = typeof(Circle); });
            mainMenu.MenuItems.Add(createItem);
            mainMenu.MenuItems.Add("Select", (s, e) => isCreateMode = false);
            mainMenu.MenuItems.Add("Move", (s, e) => StartMove());
            mainMenu.MenuItems.Add("Rotate", (s, e) => StartRotate());
            MenuItem deleteItem = new MenuItem("Delete", (s, e) => DeleteSelectedShape());
            mainMenu.MenuItems.Add("Exit", (s, e) => Close());
            mainMenu.MenuItems.Add(deleteItem);
            this.Menu = mainMenu;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

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
                            selectedShape.IsSelected = false; // Deselect the previously selected shape
                        }
                        selectedShape = shape;
                        selectedShape.IsSelected = true; // Highlight the newly selected shape
                        shapeFound = true;

                        // Prepare to move immediately without needing to select "Move" from the menu
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
                if (tempShape is Square || tempShape is Circle)
                {
                    ((dynamic)tempShape).UpdateEndPoint(e.Location);
                }
                // Other shapes logic...
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

        /////////////////////////////////////////////////////////////////////

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
            // Already set up in the OnMouseDown, ensure we're over the selected shape
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


        /////////////////////////////////////////////////////////////////////



        private void StartRotate()
        {
            // Rotation logic...
        }

        private void DeleteSelectedShape()
        {
            if (selectedShape != null)
            {
                // Remove the selected shape from the list of shapes
                shapes.Remove(selectedShape);
                // Clear the selectedShape as it no longer exists
                selectedShape = null;
                // Force the form to redraw to reflect the removal of the shape
                this.Invalidate();
            }
            else
            {
                // Optionally handle the case where no shape was selected but delete was attempted
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
            tempShape?.Draw(g); // Draw the temporary shape if it's not null
        }

        // Other event handlers and methods...
    }

    public abstract class Shape
    {
        public abstract void Draw(Graphics g);
        public abstract bool ContainsPoint(Point p);
        public abstract void Move(int dx, int dy);
        public abstract void UpdateEndPoint(Point newEndPoint);
        public bool IsSelected { get; set; }
        public Point StartPoint { get; protected set; }
        public Point EndPoint { get; protected set; }
    }

    public static class ShapeFactory
    {
        public static Shape CreateShape(Type shapeType, Point start)
        {
            if (shapeType == typeof(Square))
            {
                return new Square(start);
            }
            else if (shapeType == typeof(Circle))
            {
                return new Circle(start);
            }
            // Add logic for other shapes if necessary
            throw new ArgumentException("Invalid shape type");
        }
    }

    // Implementation for Square, Circle, and other shapes...
    public class Square : Shape
    {
        public Square(Point start) : base()
        {
            this.StartPoint = start;
            this.EndPoint = start; // Initially, the end point is the same as the start point.
        }

        public override void Draw(Graphics g)
        {
            using (Pen pen = IsSelected ? new Pen(Color.Red, 3) : new Pen(Color.Black))
            {
                // Calculate the size based on start and end points.
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
    }

    public class Circle : Shape
    {
        public Circle(Point center) : base()
        {
            this.StartPoint = center;
            this.EndPoint = center; // Initially, the end point is the same as the center for the radius.
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
    }

}