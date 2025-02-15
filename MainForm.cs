using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using System.Windows.Forms;
using System;
using System.Drawing;

public class MainForm : Form
{
    private NotifyIcon trayIcon;
    private TrackBar volumeSlider;
    private CheckBox lockVolumeCheckbox;
    private MMDeviceEnumerator deviceEnumerator;
    private MMDevice micDevice;
    private System.Windows.Forms.Timer volumeCheckTimer;
    private float lockedVolume = 1.0f;

    public MainForm()
    {
        InitializeComponents();
        InitializeAudio();
        SetupVolumeCheckTimer();
    }

    private void InitializeComponents()
    {
        // Form settings
        this.ShowInTaskbar = false;
        this.WindowState = FormWindowState.Minimized;
        this.FormBorderStyle = FormBorderStyle.FixedToolWindow;
        this.Size = new Size(200, 100);

        // Create tray icon
        trayIcon = new NotifyIcon()
        {
            Icon = SystemIcons.Application,
            Visible = true,
            Text = "Microphone Volume Controller"
        };

        // Create context menu for tray icon
        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Show", null, (s, e) => { this.Show(); this.WindowState = FormWindowState.Normal; });
        contextMenu.Items.Add("Exit", null, (s, e) => Application.Exit());
        trayIcon.ContextMenuStrip = contextMenu;

        // Create volume slider
        volumeSlider = new TrackBar()
        {
            Minimum = 0,
            Maximum = 100,
            Location = new Point(10, 10),
            Width = 150
        };
        volumeSlider.ValueChanged += VolumeSlider_ValueChanged;

        // Create lock checkbox
        lockVolumeCheckbox = new CheckBox()
        {
            Text = "Lock Volume",
            Location = new Point(10, 40)
        };
        lockVolumeCheckbox.CheckedChanged += LockVolumeCheckbox_CheckedChanged;

        // Add controls to form
        this.Controls.Add(volumeSlider);
        this.Controls.Add(lockVolumeCheckbox);
    }

    private void InitializeAudio()
    {
        deviceEnumerator = new MMDeviceEnumerator();
        micDevice = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia);

        // Set initial slider value
        volumeSlider.Value = (int)(micDevice.AudioEndpointVolume.MasterVolumeLevelScalar * 100);
    }

    private void SetupVolumeCheckTimer()
    {
        volumeCheckTimer = new System.Windows.Forms.Timer()
        {
            Interval = 5 * 60 * 1000 // 5 minutes
        };
        volumeCheckTimer.Tick += VolumeCheckTimer_Tick;
    }

    private void VolumeSlider_ValueChanged(object sender, EventArgs e)
    {
        float newVolume = volumeSlider.Value / 100f;
        micDevice.AudioEndpointVolume.MasterVolumeLevelScalar = newVolume;
    }

    private void LockVolumeCheckbox_CheckedChanged(object sender, EventArgs e)
    {
        if (lockVolumeCheckbox.Checked)
        {
            lockedVolume = micDevice.AudioEndpointVolume.MasterVolumeLevelScalar;
            volumeCheckTimer.Start();
        }
        else
        {
            volumeCheckTimer.Stop();
        }
    }

    private void VolumeCheckTimer_Tick(object sender, EventArgs e)
    {
        if (lockVolumeCheckbox.Checked)
        {
            float currentVolume = micDevice.AudioEndpointVolume.MasterVolumeLevelScalar;
            if (Math.Abs(currentVolume - lockedVolume) > 0.01f) // Small threshold for float comparison
            {
                micDevice.AudioEndpointVolume.MasterVolumeLevelScalar = lockedVolume;
                volumeSlider.Value = (int)(lockedVolume * 100);
            }
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            this.Hide();
        }
        else
        {
            trayIcon.Dispose();
            base.OnFormClosing(e);
        }
    }
}