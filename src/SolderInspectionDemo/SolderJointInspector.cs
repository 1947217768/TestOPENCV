using OpenCvSharp;

namespace SolderInspectionDemo;

public sealed class SolderJointInspector
{
    public InspectionResult Inspect(
        Mat source,
        double minArea,
        double minCircularity,
        int binaryThreshold,
        bool invert)
    {
        if (source.Empty())
        {
            throw new ArgumentException("输入图像为空", nameof(source));
        }

        using var gray = new Mat();
        using var blur = new Mat();
        using var binary = new Mat();

        Cv2.CvtColor(source, gray, ColorConversionCodes.BGR2GRAY);
        Cv2.GaussianBlur(gray, blur, new Size(5, 5), 0);

        var thresholdType = invert ? ThresholdTypes.BinaryInv : ThresholdTypes.Binary;
        Cv2.Threshold(blur, binary, binaryThreshold, 255, thresholdType);

        Cv2.FindContours(binary, out var contours, out _, RetrievalModes.External, ChainApproxMethods.ChainApproxSimple);

        var marks = source.Clone();
        var joints = new List<SolderJointAssessment>();

        foreach (var contour in contours)
        {
            var area = Cv2.ContourArea(contour);
            if (area < 20)
            {
                continue;
            }

            var perimeter = Cv2.ArcLength(contour, true);
            var circularity = perimeter <= 0.0 ? 0.0 : 4 * Math.PI * area / (perimeter * perimeter);
            var box = Cv2.BoundingRect(contour);

            var isComplete = area >= minArea && circularity >= minCircularity;
            var color = isComplete ? Scalar.LimeGreen : Scalar.Red;

            Cv2.Rectangle(marks, box, color, 2);
            Cv2.PutText(
                marks,
                $"A:{area:F0} C:{circularity:F2}",
                new Point(box.X, Math.Max(0, box.Y - 5)),
                HersheyFonts.HersheySimplex,
                0.45,
                color,
                1);

            joints.Add(new SolderJointAssessment(box, area, circularity, isComplete));
        }

        return new InspectionResult(binary.Clone(), marks, joints);
    }
}

public sealed record SolderJointAssessment(Rect BoundingRect, double Area, double Circularity, bool IsComplete);

public sealed class InspectionResult : IDisposable
{
    public InspectionResult(Mat binary, Mat marked, IReadOnlyList<SolderJointAssessment> joints)
    {
        Binary = binary;
        Marked = marked;
        Joints = joints;
    }

    public Mat Binary { get; }

    public Mat Marked { get; }

    public IReadOnlyList<SolderJointAssessment> Joints { get; }

    public int CompleteCount => Joints.Count(j => j.IsComplete);

    public int IncompleteCount => Joints.Count - CompleteCount;

    public void Dispose()
    {
        Binary.Dispose();
        Marked.Dispose();
    }
}
