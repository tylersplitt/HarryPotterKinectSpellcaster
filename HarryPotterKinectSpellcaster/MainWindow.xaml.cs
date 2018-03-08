using Microsoft.Kinect;
using Microsoft.Speech.AudioFormat;
using Microsoft.Speech.Recognition;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace HarryPotterKinectSpellcaster
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MediaElement[] players;

        private int expelCount = 0;
        private int expatCount = 0;

        private Storyboard introTextStoryBoard;

        private Storyboard transitionStartStoryBoard;
        private Storyboard transitionEndStoryBoard;

        private MediaElement currentPlayer;

        private bool listening = false;

        private bool introTextAnimated = false;

        /// <summary>
        /// Active Kinect sensor.
        /// </summary>
        private KinectSensor kinectSensor = null;

        /// <summary>
        /// Stream for 32b-16b conversion.
        /// </summary>
        private KinectAudioStream convertStream = null;

        /// <summary>
        /// Speech recognition engine using audio data from Kinect.
        /// </summary>
        private SpeechRecognitionEngine speechEngine = null;

        public MainWindow()
        {
            InitializeComponent();

            Mouse.OverrideCursor = Cursors.None;

            players = new MediaElement[] {
                introPlayer,
                idlePlayer,
                winLevPlayer,
                expelPlayer1,
                expelPlayer2,
                alohomoraPlayer,
                expatPlayer1,
                expatPlayer2,
                avadaPlayer
            };

            foreach(var p in players)
            {
                p.Visibility = Visibility.Hidden;
                p.Volume = 0;
                p.Play();
                p.Stop();
                p.Volume = 1;
            }

            introPlayer.Visibility = Visibility.Visible;
            introPlayer.Play();

            currentPlayer = introPlayer;

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += timer_Tick;
            timer.Start();

            setAnimations();
        }

        private void setAnimations()
        {
            setIntroAnimations();
            setTransitionAnimations();
        }

        private void setIntroAnimations()
        {
            DoubleAnimation introTextFadeInAnimation = new DoubleAnimation();
            introTextFadeInAnimation.From = 0.0;
            introTextFadeInAnimation.To = 1.0;
            introTextFadeInAnimation.Duration = new Duration(TimeSpan.FromSeconds(2));

            DoubleAnimation introTextHeightAnimation = new DoubleAnimation();
            introTextHeightAnimation.From = 300;
            introTextHeightAnimation.To = 400;
            introTextHeightAnimation.Duration = new Duration(TimeSpan.FromSeconds(2));

            DoubleAnimation introTextIdleAnimation = new DoubleAnimation();
            introTextIdleAnimation.From = 0;
            introTextIdleAnimation.To = 10;
            introTextIdleAnimation.AutoReverse = true;
            introTextIdleAnimation.RepeatBehavior = RepeatBehavior.Forever;
            introTextIdleAnimation.Duration = new Duration(TimeSpan.FromSeconds(2));

            introTextStoryBoard = new Storyboard();
            introTextStoryBoard.Children.Add(introTextFadeInAnimation);
            introTextStoryBoard.Children.Add(introTextHeightAnimation);

            Storyboard.SetTargetName(introTextFadeInAnimation, introText.Name);
            Storyboard.SetTargetProperty(introTextFadeInAnimation, new PropertyPath(OpacityProperty));

            Storyboard.SetTargetName(introTextHeightAnimation, introText.Name);
            Storyboard.SetTargetProperty(introTextHeightAnimation, new PropertyPath(MaxHeightProperty));

            var trans = new TranslateTransform();
            introText.RenderTransform = trans;

            trans.BeginAnimation(TranslateTransform.YProperty, introTextIdleAnimation);
        }

        private void setTransitionAnimations()
        {
            DoubleAnimation transitionFadeOutAnimation = new DoubleAnimation();
            transitionFadeOutAnimation.From = 0.7;
            transitionFadeOutAnimation.To = 0.0;
            transitionFadeOutAnimation.FillBehavior = FillBehavior.Stop;
            transitionFadeOutAnimation.Duration = new Duration(TimeSpan.FromSeconds(1.40));

            DoubleAnimation transitionFadeInAnimation = new DoubleAnimation();
            transitionFadeInAnimation.From = 0.0;
            transitionFadeInAnimation.To = 0.7;
            transitionFadeInAnimation.FillBehavior = FillBehavior.Stop;
            transitionFadeInAnimation.Duration = new Duration(TimeSpan.FromSeconds(1.50));

            transitionStartStoryBoard = new Storyboard();
            transitionStartStoryBoard.Children.Add(transitionFadeInAnimation);

            Storyboard.SetTarget(transitionFadeInAnimation, transitionStartPlayer);
            Storyboard.SetTargetProperty(transitionFadeInAnimation, new PropertyPath(OpacityProperty));

            transitionEndStoryBoard = new Storyboard();
            transitionEndStoryBoard.Children.Add(transitionFadeOutAnimation);

            Storyboard.SetTarget(transitionFadeOutAnimation, transitionEndPlayer);
            Storyboard.SetTargetProperty(transitionFadeOutAnimation, new PropertyPath(OpacityProperty));
        }

        private void EscapeHandler(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Application.Current.Shutdown();
            }
        }

        void timer_Tick(object sender, EventArgs e)
        {
            if(kinectSensor != null && !kinectSensor.IsAvailable)
            {
                this.statusBarText.Text = Properties.Resources.NoKinectReady;
            }
            else if (kinectSensor != null)
            { 
                this.statusBarText.Text = "Free to speak";
            }
            if (currentPlayer != null && currentPlayer.NaturalDuration.HasTimeSpan &&
                currentPlayer.IsVisible &&
                TimeSpan.Compare(currentPlayer.Position.Add(TimeSpan.FromSeconds(.5)), currentPlayer.NaturalDuration.TimeSpan) > 0)
            {
                currentPlayer = idlePlayer;
                startTransition();
            }

            var introTime = TimeSpan.FromMilliseconds(27500);
            if (!introTextAnimated && TimeSpan.Compare(introPlayer.Position, introTime) > 0)
            {
                introTextStoryBoard.Begin(introText);
                introTextAnimated = true;
                listening = true;
            }
        }

        private void startIdle()
        {
            showIdle();
            idlePlayer.Play();
            idlePlayer.MediaEnded += repeatIdle;
        }

        private void repeatIdle(object sender, RoutedEventArgs e)
        {
            var player = (MediaElement)sender;
            player.Stop();
            player.Visibility = Visibility.Hidden;

            startIdle();
        }

        private void playVideo(MediaElement player)
        {
            player.Stop();
            player.Visibility = Visibility.Visible;
            player.Play();
            //player.MediaEnded += repeatIdle;

            hideAllOtherPlayers(player);
        }

        private void startTransition()
        {
            transitionStartPlayer.Visibility = Visibility.Visible;
            transitionStartPlayer.Play();
            transitionStartPlayer.MediaEnded += transitionMiddle;

            transitionStartStoryBoard.Begin(transitionStartPlayer);
        }

        private void transitionMiddle(object source, RoutedEventArgs e)
        {
            transitionStartPlayer.Stop();
            transitionStartPlayer.Opacity = 0;
            transitionStartPlayer.Visibility = Visibility.Hidden;

            endTransition();

            playVideo(currentPlayer);
        }

        private void endTransition()
        {
            transitionEndPlayer.Visibility = Visibility.Visible;
            transitionEndPlayer.Play();
            transitionEndPlayer.MediaEnded += transitionEnded;

            transitionEndStoryBoard.Begin(transitionEndPlayer);
        }

        private void transitionEnded(object source, RoutedEventArgs e)
        {
            transitionEndPlayer.Stop();
            transitionEndPlayer.Visibility = Visibility.Hidden;
            transitionEndPlayer.Opacity = 0;
        }

        private void showIdle()
        {
            introText.Visibility = Visibility.Visible;
            idlePlayer.Visibility = Visibility.Visible;
        }

        private void hideAllOtherPlayers(MediaElement player)
        {
            foreach(var p in players)
            {
                if(player != p)
                {
                    p.Stop();
                    p.Visibility = Visibility.Hidden;
                }
            }

            if(player == introPlayer || player == idlePlayer)
            {
                introText.Visibility = Visibility.Visible;
                listening = true;
            }
            else
            {
                introText.Visibility = Visibility.Hidden;
                listening = false;
            }
        }

        private void hideIdleIntro()
        {
            introText.Visibility = Visibility.Hidden;
            introPlayer.Stop();
            introPlayer.Visibility = Visibility.Hidden;
            idlePlayer.Stop();
            idlePlayer.Visibility = Visibility.Hidden;
        }

        private void winLevPlay()
        {
            startTransition();
            currentPlayer = winLevPlayer;
        }

        private void alohomoraPlay()
        {
            startTransition();
            currentPlayer = alohomoraPlayer;
        }

        private void expelPlay()
        {
            startTransition();

            if (expelCount % 2 == 0)
            {
                currentPlayer = expelPlayer1;
            }
            else
            {
                currentPlayer = expelPlayer2;
            }

            expelCount++;
        }

        private void expatPlay()
        {
            startTransition();

            if (expatCount % 2 == 0)
            {
                currentPlayer = expatPlayer1;
            }
            else
            {
                currentPlayer = expatPlayer2;
            }

            expatCount++;
        }

        private void avadaPlay()
        {
            startTransition();
            currentPlayer = avadaPlayer;
        }

        /// <summary>
        /// Gets the metadata for the speech recognizer (acoustic model) most suitable to
        /// process audio from Kinect device.
        /// </summary>
        /// <returns>
        /// RecognizerInfo if found, <code>null</code> otherwise.
        /// </returns>
        private static RecognizerInfo TryGetKinectRecognizer()
        {
            IEnumerable<RecognizerInfo> recognizers;

            // This is required to catch the case when an expected recognizer is not installed.
            // By default - the x86 Speech Runtime is always expected. 
            try
            {
                recognizers = SpeechRecognitionEngine.InstalledRecognizers();
            }
            catch (COMException)
            {
                return null;
            }

            foreach (RecognizerInfo recognizer in recognizers)
            {
                string value;
                recognizer.AdditionalInfo.TryGetValue("Kinect", out value);
                if ("True".Equals(value, StringComparison.OrdinalIgnoreCase) && "en-US".Equals(recognizer.Culture.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return recognizer;
                }
            }

            return null;
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            // Only one sensor is supported
            this.kinectSensor = KinectSensor.GetDefault();

            if (this.kinectSensor != null)
            {
                // open the sensor
                this.kinectSensor.Open();

                // grab the audio stream
                IReadOnlyList<AudioBeam> audioBeamList = this.kinectSensor.AudioSource.AudioBeams;
                System.IO.Stream audioStream = audioBeamList[0].OpenInputStream();

                // create the convert stream
                this.convertStream = new KinectAudioStream(audioStream);
            }
            else
            {
                // on failure, set the status text
                return;
            }

            RecognizerInfo ri = TryGetKinectRecognizer();

            if (null != ri)
            {
                this.speechEngine = new SpeechRecognitionEngine(ri.Id);

                // Create a grammar from grammar definition XML file.
                using (var memoryStream = new MemoryStream(Encoding.ASCII.GetBytes(Properties.Resources.SpeechGrammar)))
                {
                    var g = new Grammar(memoryStream);
                    this.speechEngine.LoadGrammar(g);
                }

                this.speechEngine.SpeechRecognized += this.SpeechRecognized;
                this.speechEngine.SpeechRecognitionRejected += this.SpeechRejected;

                // let the convertStream know speech is going active
                this.convertStream.SpeechActive = true;

                // For long recognition sessions (a few hours or more), it may be beneficial to turn off adaptation of the acoustic model. 
                // This will prevent recognition accuracy from degrading over time.
                ////speechEngine.UpdateRecognizerSetting("AdaptationOn", 0);

                this.speechEngine.SetInputToAudioStream(
                    this.convertStream, new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
                this.speechEngine.RecognizeAsync(RecognizeMode.Multiple);
            }
            else
            {

            }
        }

        /// <summary>
        /// Execute un-initialization tasks.
        /// </summary>
        /// <param name="sender">object sending the event.</param>
        /// <param name="e">event arguments.</param>
        private void WindowClosing(object sender, CancelEventArgs e)
        {
            if (null != this.convertStream)
            {
                this.convertStream.SpeechActive = false;
            }

            if (null != this.speechEngine)
            {
                this.speechEngine.SpeechRecognized -= this.SpeechRecognized;
                this.speechEngine.SpeechRecognitionRejected -= this.SpeechRejected;
                this.speechEngine.RecognizeAsyncStop();
            }

            if (null != this.kinectSensor)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
        }

        /// <summary>
        /// Handler for recognized speech events.
        /// </summary>
        /// <param name="sender">object sending the event.</param>
        /// <param name="e">event arguments.</param>
        private void SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            // Speech utterance confidence below which we treat speech as if it hadn't been heard
            const double ConfidenceThreshold = 0.5;

            this.confidence.Text += "; " + e.Result.Confidence.ToString();

            if (e.Result.Confidence >= ConfidenceThreshold && listening)
            {
                switch (e.Result.Semantics.Value.ToString())
                {
                    case "WINLEV":
                        winLevPlay();
                        break;

                    case "ALO":
                        alohomoraPlay();
                        break;

                    case "EXPEL":
                        expelPlay();
                        break;

                    case "EXPAT":
                        expatPlay();
                        break;

                    case "NONO":
                        avadaPlay();
                        break;
                }
            }
        }

        /// <summary>
        /// Handler for rejected speech events.
        /// </summary>
        /// <param name="sender">object sending the event.</param>
        /// <param name="e">event arguments.</param>
        private void SpeechRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {

        }
    }
}
