using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace GamepadRumbleBridge;

/// <summary>
/// Настройки каждого геймпада: коэффициент вибро (0-100%) и статус подключения.
/// </summary>
public partial class PadSettingsForm : Form
{
    private readonly List<PadSlotInfo> _slots = new();
    private readonly Action<int, int> _onApply; // padIndex, scale(0-100)

    public PadSettingsForm(List<PadConnectionInfo> pads, Action<int, int> onApply)
    {
        _onApply = onApply ?? throw new ArgumentNullException(nameof(onApply));
        InitializeComponent();
        InitializeSlots(pads);
    }

    private void InitializeSlots(List<PadConnectionInfo> pads)
    {
        flowLayoutPanel.Controls.Clear();
        _slots.Clear();

        for (int i = 0; i < MaxPads; i++)
        {
            var info = i < pads.Count ? pads[i] : new PadConnectionInfo { Index = i, Connected = false, RumbleScale = 100 };
            _slots.Add(new PadSlotInfo { Index = i, Connected = info.Connected, Scale = info.RumbleScale });

            var panel = new Panel
            {
                Size = new Size(320, 60),
                Margin = new Padding(4),
                BorderStyle = BorderStyle.FixedSingle,
                Tag = i
            };

            var label = new Label
            {
                Text = info.Connected
                    ? $"Геймпад #{i + 1}: подключён"
                    : $"Слот #{i + 1}: не подключён",
                Location = new Point(10, 8),
                AutoSize = true,
                Font = new Font(Font.FontFamily, 9, FontStyle.Bold)
            };

            var trackBar = new TrackBar
            {
                Minimum = 0,
                Maximum = 100,
                Value = info.Connected ? info.RumbleScale : 0,
                Enabled = info.Connected,
                Location = new Point(10, 34),
                Width = 180,
                TickFrequency = 25
            };
            trackBar.ValueChanged += (s, e) => UpdateScaleLabel(i, trackBar.Value);

            var scaleLabel = new Label
            {
                Text = $"{(info.Connected ? info.RumbleScale : 0)}%",
                Location = new Point(200, 36),
                AutoSize = true,
                Font = new Font(Font.FontFamily, 9)
            };

            panel.Controls.AddRange(new Control[] { label, trackBar, scaleLabel });
            flowLayoutPanel.Controls.Add(panel);
        }
    }

    private void UpdateScaleLabel(int idx, int value)
    {
        foreach (Control c in flowLayoutPanel.Controls)
        {
            if (c.Tag is int tag && tag == idx)
            {
                foreach (Control child in c.Controls)
                {
                    if (child is Label lbl && lbl.Text.Contains("%"))
                        lbl.Text = $"{value}%";
                }
            }
        }
    }

    private void btnApply_Click(object sender, EventArgs e)
    {
        foreach (var slot in _slots)
            _onApply(slot.Index, slot.Scale);
        MessageBox.Show("Настройки применены!", "Gamepad Bridge", MessageBoxButtons.OK, MessageBoxIcon.Information);
        Close();
    }

    private void btnCancel_Click(object sender, EventArgs e)
        => Close();

    private const int MaxPads = 4;

    private class PadSlotInfo
    {
        public int Index;
        public bool Connected;
        public int Scale;
    }
}

public struct PadConnectionInfo
{
    public int Index;
    public bool Connected;
    public int RumbleScale; // 0-100
}
