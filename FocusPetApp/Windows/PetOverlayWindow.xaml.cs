using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using FocusPetApp.Controllers;
using FocusPetApp.Models;
using FocusPetApp.Services;

namespace FocusPetApp.Windows
{
    public partial class PetOverlayWindow : Window
    {
        // Controllers
        private readonly TimerController _timer = new();
        private readonly EyeExerciseController _exercise = new();

        // Idle blink timer (pet blinks every few seconds)
        private readonly DispatcherTimer _idleBlink = new();

        // Persisted settings
        private UserSettings _settings;

        public PetOverlayWindow()
        {
            InitializeComponent();

            // Load settings (or defaults)
            _settings = SettingsService.Load();

            // Apply customization to the pet
            Pet.SetColor(_settings.PetColor ?? "Mint");
            Pet.SetHat(_settings.HatOn);

            // Start soft breathing (handled inside BlobCat) + set idle blink loop here
            _idleBlink.Interval = TimeSpan.FromSeconds(4);
            _idleBlink.Tick += (s, e) => Pet.Blink();
            _idleBlink.Start();

            HookEvents();

            // TIP for demo recording:
            // If you want a super quick run to test break flow, uncomment:
            // _timer.Start(30); // 30 seconds to break
        }

        /// <summary>
        /// Wire timer + exercise events to UI and pet animations.
        /// </summary>
        private void HookEvents()
        {
            _timer.Tick += secs =>
            {
                // Update bubble with time remaining (MM:SS)
                var mm = secs / 60;
                var ss = secs % 60;
                Bubble.ShowMessage($"{mm}:{ss:D2} left");
            };

            _timer.Completed += () =>
            {
                Bubble.ShowMessage("Break time!");
                StartEyeExercise();
            };

            _exercise.StepChanged += step =>
            {
                // Update guidance text during break steps
                Bubble.ShowMessage(step);
            };

            _exercise.AnimationRequested += anim =>
            {
                // Drive simple pet eye/head movement
                switch (anim)
                {
                    case "left_right":
                        // do a few oscillations L/R
                        Pet.LookLeft();
                        Delay(400, () => Pet.LookRight());
                        Delay(800, () => Pet.LookLeft());
                        Delay(1200, () => Pet.LookRight());
                        break;

                    case "up_down":
                        Pet.LookUp();
                        Delay(400, () => Pet.LookDown());
                        Delay(800, () => Pet.LookUp());
                        Delay(1200, () => Pet.LookDown());
                        break;

                    case "near_far":
                        // squint then relax
                        Pet.Squint();
                        Delay(1200, () => Pet.Idle());
                        break;

                    case "blink":
                        Pet.Blink();
                        Delay(700, () => Pet.Blink());
                        break;

                    default:
                        Pet.Idle();
                        break;
                }
            };

            _exercise.Completed += () =>
            {
                Bubble.ShowMessage("Break over!");
                Pet.Idle();

                // (Optional) You can auto-restart the next work session here:
                // _timer.Start(_settings.WorkMinutes * 60);
            };
        }

        /// <summary>
        /// Start the 2-minute eye exercise sequence (controller owns the step timings).
        /// </summary>
        private void StartEyeExercise()
        {
            Pet.Idle();
            _exercise.Start();
        }

        /// <summary>
        /// Utility to schedule a short delayed action on the UI thread.
        /// </summary>
        private void Delay(int milliseconds, Action action)
        {
            var t = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(milliseconds) };
            t.Tick += (s, e) =>
            {
                (s as DispatcherTimer)!.Stop();
                action();
            };
            t.Start();
        }

        /// <summary>
        /// Allow dragging the borderless window by clicking anywhere.
        /// </summary>
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                try { DragMove(); } catch { /* ignore */ }
            }
        }

        /// <summary>
        /// Keyboard shortcuts (press 'S' to open settings).
        /// </summary>
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.S)
            {
                OpenSettings();
            }
        }

        /// <summary>
        /// Gear button opens the settings window.
        /// </summary>
        private void BtnOpenSettings_Click(object sender, RoutedEventArgs e)
        {
            OpenSettings();
        }

        /// <summary>
        /// Create, wire, and show the Settings window (modal).
        /// </summary>
        private void OpenSettings()
        {
            var sw = new SettingsWindow(_settings);

            // When the user changes color/hat, apply to the pet immediately
            sw.CustomizationChanged += (color, hatOn) =>
            {
                Pet.SetColor(color);
                Pet.SetHat(hatOn);
                _settings.PetColor = color;
                _settings.HatOn = hatOn;
            };

            // When user clicks Start Session in settings
            sw.StartRequested += (workMinutes, breakCount) =>
            {
                // For MVP we ignore breakCount in logic; feel free to extend BreakController.
                StartWorkSession(workMinutes);
            };

            // Pause from settings
            sw.PauseRequested += () =>
            {
                _timer.Stop();
                Bubble.ShowMessage("Paused");
            };

            // Save to disk when requested
            sw.SaveRequested += (settings) =>
            {
                _settings = settings;
                SettingsService.Save(_settings);
            };

            sw.Owner = this;
            sw.ShowDialog();
        }

        /// <summary>
        /// Start a new work session (minutes -> starts a countdown in seconds).
        /// </summary>
        private void StartWorkSession(int minutes)
        {
            // Safety check
            if (minutes <= 0) minutes = 1;

            Pet.Idle();
            Bubble.ShowMessage($"Session started: {minutes} min");

            _timer.Stop(); // ensure clean start
            _timer.Start(minutes * 60);
        }
    }
}

