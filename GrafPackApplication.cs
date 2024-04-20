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
        private Point startPoint; // Used for rubber-banding and moving shapes
        private MainMenu mainMenu;

        public GrafPackApplication()
        {
            InitializeComponent();
            SetupMainMenu();
        }

        private void SetupMainMenu()
        {
            mainMenu = new MainMenu();
            MenuItem createItem = new MenuItem("Create");
            createItem.MenuItems.Add("Square", (s, e) => StartShapeCreation(typeof(Square)));
            //createItem.MenuItems.Add("Triangle", (s, e) => StartShapeCreation(typeof(Triangle)));
            //createItem.MenuItems.Add("Circle", (s, e) => StartShapeCreation(typeof(Circle)));

            MenuItem selectItem = new MenuItem("Select", SelectShape);

            MenuItem transformItem = new MenuItem("Transform");
            transformItem.MenuItems.Add("Move", (s, e) => StartMove());
            transformItem.MenuItems.Add("Rotate", (s, e) => StartRotate());

            MenuItem deleteItem = new MenuItem("Delete", DeleteShape);

            MenuItem exitItem = new MenuItem("Exit", (s, e) => Close());

            mainMenu.MenuItems.Add(createItem);
            mainMenu.MenuItems.Add(selectItem);
            mainMenu.MenuItems.Add(transformItem);
            mainMenu.MenuItems.Add(deleteItem);
            mainMenu.MenuItems.Add(exitItem);

            this.Menu = mainMenu;
        }

        private void StartShapeCreation(Type shapeType)
        {
            MessageBox.Show($"Click to start creating a {shapeType.Name}");
            this.MouseDown += (s, args) => // Changed from sender, e to s, args
            {
                startPoint = args.Location;
                this.MouseUp += OnMouseUpCreateShape;
            };
        }

        private void SelectShape(object sender, EventArgs e)
        {
            MessageBox.Show("Click a shape to select it.");
            this.MouseDown += (s, args) => // Changed from sender, e to s, args
            {
                foreach (var shape in shapes)
                {
                    if (shape.ContainsPoint(args.Location))
                    {
                        selectedShape = shape;
                        break;
                    }
                }
                HighlightSelectedShape();
            };
        }

        private void OnMouseUpCreateShape(object sender, MouseEventArgs e)
        {
            // Factory method to create specific shape instance
            Shape newShape = ShapeFactory.CreateShape(startPoint, e.Location);
            shapes.Add(newShape);
            this.Invalidate(); // Trigger redraw to show new shape
            this.MouseUp -= OnMouseUpCreateShape; // Clean up event handlers
        }

        private void HighlightSelectedShape()
        {
            // Logic to visually highlight the selected shape
            this.Invalidate();
        }

        private Point lastLocation;  // To store last location for movement

        private void StartMove()
        {
            if (selectedShape == null) return;
            MessageBox.Show("Drag to move the selected shape.");
            this.MouseDown += BeginMove;
            this.MouseMove += MoveShape;
            this.MouseUp += EndMove;
        }

        private void BeginMove(object sender, MouseEventArgs e)
        {
            if (selectedShape != null && selectedShape.ContainsPoint(e.Location))
            {
                lastLocation = e.Location;
            }
        }

        private void MoveShape(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && selectedShape != null)
            {
                var dx = e.X - lastLocation.X;
                var dy = e.Y - lastLocation.Y;
                selectedShape.Move(dx, dy);
                lastLocation = e.Location;
                this.Invalidate(); // Refresh the form to update the position of the shape
            }
        }

        private void EndMove(object sender, MouseEventArgs e)
        {
            this.MouseMove -= MoveShape;
            this.MouseUp -= EndMove;
            this.MouseDown -= BeginMove;
        }


        private void StartRotate()
        {
            // Implement rotation functionality here
        }

        private void DeleteShape(object sender, EventArgs e)
        {
            if (selectedShape != null)
            {
                shapes.Remove(selectedShape);
                selectedShape = null;
                this.Invalidate(); // Redraw to update the screen
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
        }
    }

    public abstract class Shape
    {
        public abstract void Draw(Graphics g);
        public abstract bool ContainsPoint(Point p);
        public abstract void Move(int dx, int dy);

    }

    public class Square : Shape
    {
        private Point start, end;
        public Square(Point start, Point end)
        {
            this.start = start;
            this.end = end;
        }
        public override void Move(int dx, int dy)
        {
            // Logic to move the square by dx and dy
            start = new Point(start.X + dx, start.Y + dy);
            end = new Point(end.X + dx, end.Y + dy);
        }

        public override void Draw(Graphics g)
        {
            g.DrawRectangle(Pens.Black, start.X, start.Y, end.X - start.X, end.Y - start.Y);
        }

        public override bool ContainsPoint(Point p)
        {
            // Implement logic to determine if a point is inside the square
            return (p.X >= start.X && p.X <= end.X && p.Y >= start.Y && p.Y <= end.Y);
        }
    }

    // Implement Triangle, Circle, etc.

    public static class ShapeFactory
    {
        public static Shape CreateShape(Point start, Point end)
        {
            // Example of creating a Square; can be expanded for other shapes
            return new Square(start, end);
        }
    }
}
