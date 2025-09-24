using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace TreeAlgorithms
{
    public class TreeAlgorithmModules
    {
        public static double GetNormalizedAngle(double angle)
        {
            if ((0 <= angle) && (angle < 360))
            {
                return angle;
            }
            else if (angle < 0)
            {
                return 360 - (-angle % 360);
            }
            else
            {
                return angle % 360;
            }
        }

        public static double CalculateContainmentRadius(double parentRadius, int n, bool isParentRoot, double arcAngle)
        {
            if (n < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(n), "n should not be lower than 1.");
            }

            if (parentRadius < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(parentRadius), "Parent radius should not be lower than 0.");
            }

            if (n == 1)
            {
                return parentRadius / 2;
            }
            else if (isParentRoot == true)
            {
                return Math.Sqrt(2 * Math.Pow(parentRadius, 2) * (1 - Math.Cos((Math.PI)/Convert.ToDouble(n))));
            }
            else
            {
                if ((arcAngle < 0) || (arcAngle > 180))
                {
                    throw new ArgumentOutOfRangeException(nameof(arcAngle), "Arc angle should not be outside the range of 0 to 180 inclusive..");
                }

                return Math.Sqrt(2 * Math.Pow(parentRadius, 2) * (1 - Math.Cos((arcAngle * (Math.PI/180))/(2*Convert.ToDouble(n) - 2))));
            }
        }

        public static double CalculateRelativeAngle(int n, int i, bool isParentRoot, double arcAngle)
        {
            if (n < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(n), "n should not be lower than 1.");
            }
            
            if ((i < 0) || (i > (n - 1)))
            {
                throw new ArgumentOutOfRangeException(nameof(i), "i should not be lower than the lowest index (0) or higher than the highest index (n - 1).");
            }

            if (n == 1)
            {
                return 0;
            }
            else if (isParentRoot == true)
            {
                return (360 / Convert.ToDouble(n)) * i;
            }
            else
            {
                if ((arcAngle < 0) || (arcAngle > 180))
                {
                    throw new ArgumentOutOfRangeException(nameof(arcAngle), "Arc angle should not be outside the range 0 to 180 inclusive.");
                }

                return (arcAngle * i) / (Convert.ToDouble(n) - 1) - (arcAngle / 2);
            }
        }

        public static double CalculateNonRelativeAngle(double relativeAngle, double parentAngle)
        {
            if ((parentAngle < 0) || (parentAngle >= 360))
            {
                throw new ArgumentOutOfRangeException(nameof(parentAngle), "Parent angle should be normalized (between 0 inclusive to 360).");
            }

            return GetNormalizedAngle(parentAngle + relativeAngle);
        }

        public static double[] CalculatePosition(double angle, double parentRadius, double[] parentPosition)
        {
            if ((angle < 0) || (angle >= 360))
            {
                throw new ArgumentOutOfRangeException(nameof(angle), "Angle should be normalized (between 0 inclusive to 360).");
            }

            if (parentRadius < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(parentRadius), "Parent radius should not be lower than 0.");
            }

            double x = parentPosition[0] + parentRadius * Math.Cos(angle * (Math.PI / 180));
            double y = parentPosition[1] + parentRadius * Math.Sin(angle * (Math.PI / 180));

            return new double[2] {x, y};
        }

        public static void PreorderTraversal(TreeClasses.Node node)
        {
            List<TreeClasses.Node> children = node.GetChildren();
            int n = children.Count; // Represents the number of children

            if (n == 0) { return; }

            double childRadius = CalculateContainmentRadius(node.Radius, n, node.IsRoot(), node.ArcAngle);
            int i = 0; // Represents the order of children
            
            foreach(TreeClasses.Node child in children)
            {
                child.Radius = childRadius;
                child.RelativeAngle = CalculateRelativeAngle(n, i, node.IsRoot(), node.ArcAngle);
                child.Angle = CalculateNonRelativeAngle(child.RelativeAngle, node.Angle);
                child.Position = CalculatePosition(child.Angle, node.Radius, node.Position);
                i++;
                PreorderTraversal(child);
            }

        }
    }
}
