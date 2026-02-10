using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace SolderInspectionDemo;

public sealed class MainForm : Form
{
    private readonly PictureBox _pictureOriginal;
    private readonly PictureBox _pictureResult;
    private readonly NumericUpDown _numericMinArea;
    private readonly NumericUpDown _numericCircularity;
    private readonly NumericUpDown _numericThreshold;
    private readonly CheckBox _checkInvert;
    private readonly Label _labelSummary;
    private readonly SolderJointInspector _inspector;

    private Mat? _currentSource;

    public MainForm()
    {
        Text = "焊点完整性检测 DEMO (OpenCvSharp)";
        Width = 1300;
        Height = 760;
        StartPosition = FormStartPosition.CenterScreen;

        _inspector = new SolderJointInspector();

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(8)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var panelTop = BuildTopPanel();

        var imagePanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1
        };
        imagePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        imagePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        _pictureOriginal = CreatePictureBox("原图");
        _pictureResult = CreatePictureBox("检测结果");

        imagePanel.Controls.Add(WrapPictureBox("原图", _pictureOriginal), 0, 0);
        imagePanel.Controls.Add(WrapPictureBox("结果图（红色=疑似不完整，绿色=合格）", _pictureResult), 1, 0);

        root.Controls.Add(panelTop, 0, 0);
        root.Controls.Add(imagePanel, 0, 1);

        Controls.Add(root);
    }

    private Panel BuildTopPanel()
    {
        var panel = new Panel { Dock = DockStyle.Fill };

        var btnLoad = new Button
        {
            Text = "加载待检图片",
            Width = 120,
            Height = 32,
            Left = 10,
            Top = 10
        };
        btnLoad.Click += (_, _) => LoadImage();

        var btnInspect = new Button
        {
            Text = "执行检测",
            Width = 120,
            Height = 32,
            Left = 140,
            Top = 10
        };
        btnInspect.Click += (_, _) => RunInspection();

        var lblArea = new Label { Text = "最小面积:", Left = 280, Top = 16, Width = 70 };
        _numericMinArea = new NumericUpDown
        {
            Left = 350,
            Top = 12,
            Width = 90,
            DecimalPlaces = 0,
            Minimum = 10,
            Maximum = 100000,
            Value = 250
        };

        var lblCirc = new Label { Text = "最小圆度:", Left = 460, Top = 16, Width = 70 };
        _numericCircularity = new NumericUpDown
        {
            Left = 530,
            Top = 12,
            Width = 90,
            DecimalPlaces = 2,
            Increment = 0.01M,
            Minimum = 0,
            Maximum = 1,
            Value = 0.50M
        };

        var lblThreshold = new Label { Text = "阈值:", Left = 640, Top = 16, Width = 45 };
        _numericThreshold = new NumericUpDown
        {
            Left = 685,
            Top = 12,
            Width = 80,
            DecimalPlaces = 0,
            Minimum = 0,
            Maximum = 255,
            Value = 120
        };

        _checkInvert = new CheckBox
        {
            Text = "反色阈值（焊点偏暗时使用）",
            Left = 780,
            Top = 15,
            Width = 220,
            Checked = true
        };

        _labelSummary = new Label
        {
            Text = "请先加载图片",
            Left = 10,
            Top = 55,
            Width = 1200,
            Height = 50,
            Font = new Font(Font, FontStyle.Bold)
        };

        panel.Controls.Add(btnLoad);
        panel.Controls.Add(btnInspect);
        panel.Controls.Add(lblArea);
        panel.Controls.Add(_numericMinArea);
        panel.Controls.Add(lblCirc);
        panel.Controls.Add(_numericCircularity);
        panel.Controls.Add(lblThreshold);
        panel.Controls.Add(_numericThreshold);
        panel.Controls.Add(_checkInvert);
        panel.Controls.Add(_labelSummary);

        return panel;
    }

    private static PictureBox CreatePictureBox(string name) => new()
    {
        Name = name,
        Dock = DockStyle.Fill,
        BorderStyle = BorderStyle.FixedSingle,
        SizeMode = PictureBoxSizeMode.Zoom,
        BackColor = Color.Black
    };

    private static GroupBox WrapPictureBox(string title, PictureBox picture)
    {
        var group = new GroupBox { Text = title, Dock = DockStyle.Fill, Padding = new Padding(8) };
        group.Controls.Add(picture);
        return group;
    }

    private void LoadImage()
    {
        using var ofd = new OpenFileDialog
        {
            Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff",
            Title = "选择待检测焊点图片"
        };

        if (ofd.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        _currentSource?.Dispose();
        _currentSource = Cv2.ImRead(ofd.FileName, ImreadModes.Color);

        if (_currentSource.Empty())
        {
            MessageBox.Show("图片加载失败，请检查文件。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        _pictureOriginal.Image?.Dispose();
        _pictureResult.Image?.Dispose();

        _pictureOriginal.Image = _currentSource.ToBitmap();
        _pictureResult.Image = null;
        _labelSummary.Text = "图片已加载，点击“执行检测”开始分析。";
    }

    private void RunInspection()
    {
        if (_currentSource is null || _currentSource.Empty())
        {
            MessageBox.Show("请先加载一张焊点图片。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var result = _inspector.Inspect(
            _currentSource,
            minArea: (double)_numericMinArea.Value,
            minCircularity: (double)_numericCircularity.Value,
            binaryThreshold: (int)_numericThreshold.Value,
            invert: _checkInvert.Checked);

        _pictureResult.Image?.Dispose();
        _pictureResult.Image = result.Marked.ToBitmap();

        _labelSummary.Text =
            $"检测到焊点数量: {result.Joints.Count}，合格: {result.CompleteCount}，疑似不完整: {result.IncompleteCount}。" +
            "（建议结合样本调参数：最小面积、最小圆度、阈值）";
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _currentSource?.Dispose();
        _pictureOriginal.Image?.Dispose();
        _pictureResult.Image?.Dispose();
        base.OnFormClosed(e);
    }
}
