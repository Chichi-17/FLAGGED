namespace FLAGGED;

using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

public class AnimatedButton : Button
{
    private bool _isAnimating;
    private Size _normalSize;
    private Point _normalLocation;

    public AnimatedButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        BackColor = Color.FromArgb(19, 64, 52);
        ForeColor = Color.FromArgb(235, 235, 220);
        Font = UiFonts.MenuButton(18f);
        Cursor = Cursors.Hand;
        Margin = new Padding(0, 0, 0, 10);

        MouseEnter += (_, _) => _ = AnimateHover();
        MouseLeave += (_, _) => ResetPosition();
        MouseDown += (_, _) => PulsePress();
    }

    protected override void OnCreateControl()
    {
        base.OnCreateControl();
        _normalSize = Size;
        _normalLocation = Location;
    }

    private async Task AnimateHover()
    {
        if (_isAnimating)
        {
            return;
        }

        _isAnimating = true;
        try
        {
            var targetWidth = _normalSize.Width + 10;
            var targetHeight = _normalSize.Height + 2;

            for (var step = 1; step <= 6; step++)
            {
                var width = _normalSize.Width + (int)((targetWidth - _normalSize.Width) * (step / 6.0));
                var height = _normalSize.Height + (int)((targetHeight - _normalSize.Height) * (step / 6.0));
                var x = _normalLocation.X - (int)((width - _normalSize.Width) / 2.0);
                var y = _normalLocation.Y - (int)((height - _normalSize.Height) / 2.0);

                if (InvokeRequired)
                {
                    BeginInvoke((Action)(() =>
                    {
                        Size = new Size(width, height);
                        Location = new Point(x, y);
                    }));
                }
                else
                {
                    Size = new Size(width, height);
                    Location = new Point(x, y);
                }

                await Task.Delay(8);
            }
        }
        finally
        {
            ResetPosition();
            _isAnimating = false;
        }
    }

    private void ResetPosition()
    {
        if (InvokeRequired)
        {
            BeginInvoke((Action)(() =>
            {
                Size = _normalSize;
                Location = _normalLocation;
            }));
            return;
        }

        Size = _normalSize;
        Location = _normalLocation;
    }

    private void PulsePress()
    {
        if (InvokeRequired)
        {
            BeginInvoke((Action)PulsePress);
            return;
        }

        var timer = new Timer { Interval = 70 };
        var originalSize = Size;
        var originalLocation = Location;

        Size = new Size(Size.Width - 4, Size.Height - 2);
        Location = new Point(Location.X + 2, Location.Y + 1);

        timer.Tick += (_, _) =>
        {
            timer.Stop();
            timer.Dispose();
            Size = originalSize;
            Location = originalLocation;
        };

        timer.Start();
    }
}
