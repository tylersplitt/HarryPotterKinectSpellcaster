using System;
using System.Collections.Generic;
using System.Linq;
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
        private Storyboard introTextStoryBoard;

        private Storyboard transitionStartStoryBoard;
        private Storyboard transitionEndStoryBoard;

        private MediaElement currentPlayer;

        private bool introTextAnimated = false;

        public MainWindow()
        {
            InitializeComponent();

            introPlayer.Play();

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
            transitionFadeOutAnimation.From = 1.0;
            transitionFadeOutAnimation.To = 0.0;
            transitionFadeOutAnimation.FillBehavior = FillBehavior.Stop;
            transitionFadeOutAnimation.Duration = new Duration(TimeSpan.FromSeconds(1.35));

            DoubleAnimation transitionFadeInAnimation = new DoubleAnimation();
            transitionFadeInAnimation.From = 0.0;
            transitionFadeInAnimation.To = 1.0;
            transitionFadeInAnimation.FillBehavior = FillBehavior.Stop;
            transitionFadeInAnimation.Duration = new Duration(TimeSpan.FromSeconds(1.45));

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
            if (introPlayer.NaturalDuration.HasTimeSpan &&
                introPlayer.IsVisible &&
                TimeSpan.Compare(introPlayer.Position.Add(TimeSpan.FromSeconds(2)), introPlayer.NaturalDuration.TimeSpan) > 0)
            {
                startIdle();
            }

            var introTime = TimeSpan.FromMilliseconds(27500);
            if (!introTextAnimated && TimeSpan.Compare(introPlayer.Position, introTime) > 0)
            {
                introTextStoryBoard.Begin(introText);
                introTextAnimated = true;
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
            player.Visibility = Visibility.Visible;
            player.Play();
            player.MediaEnded += repeatIdle;

            hideIdleIntro();
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
            transitionEndPlayer.Opacity = 1.0;
        }

        private void showIdle()
        {
            introText.Visibility = Visibility.Visible;
            idlePlayer.Visibility = Visibility.Visible;
        }

        private void hideIdleIntro()
        {
            introText.Visibility = Visibility.Hidden;
            introPlayer.Stop();
            introPlayer.Visibility = Visibility.Hidden;
            idlePlayer.Stop();
            idlePlayer.Visibility = Visibility.Hidden;
        }

        private void winLevSuccess(object sender, RoutedEventArgs e)
        {
            startTransition();
            currentPlayer = winLevPlayer;
        }

        private void winLevFail(object sender, RoutedEventArgs e)
        {
            startTransition();
            currentPlayer = winLevFailPlayer;
        }
    }
}
