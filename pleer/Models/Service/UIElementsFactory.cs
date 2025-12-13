using MaterialDesignThemes.Wpf;
using pleer.Models.DatabaseContext;
using pleer.Models.Media;
using pleer.Models.Users;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace pleer.Models.Service
{
    public class UIElementsFactory()
    {
        #region Track Cards
        public static Border CreateTrackCard(
            Media.Track track,
            Action<object, MouseButtonEventArgs> clickHandler,
            CardSize cardSize = CardSize.Small)
        {
            var settings = GetCardSettings(cardSize);
            var grid = TrackGrid(track, settings);

            var border = new Border
            {
                Style = Application.Current.TryFindResource("SimpleFunctionalCard") as Style,
                Margin = new Thickness(0, 0, 5, 5),
                Cursor = Cursors.Hand,
                Child = grid,
                Tag = track
            };
            border.MouseLeftButtonUp += (sender, e) => clickHandler(sender, e);

            return border;
        }

        public static Border CreateTrackCard(
            Media.Track track,
            int index,
            Action<object, MouseButtonEventArgs> clickHandler,
            CardSize cardSize = CardSize.Small)
        {
            var settings = GetCardSettings(cardSize);
            var grid = TrackGrid(track, settings);

            var idText = new TextBlock
            {
                Text = (index + 1).ToString(),
                Margin = new Thickness(15),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Style = (Style)Application.Current.TryFindResource("SmallInfoPanel"),
            };
            Grid.SetColumn(idText, 0);
            grid.Children.Add(idText);

            var border = new Border
            {
                Style = Application.Current.TryFindResource("SimpleFunctionalCard") as Style,
                Margin = new Thickness(0, 0, 5, 5),
                Cursor = Cursors.Hand,
                Child = grid,
                Tag = track
            };
            border.MouseLeftButtonUp += (sender, e) => clickHandler(sender, e);

            return border;
        }

        public static Border CreateTrackCard(
            Media.Track track,
            Listener listener,
            int index,
            Action<object, MouseButtonEventArgs> clickHandler,
            CardSize cardSize = CardSize.Small)
        {
            var settings = GetCardSettings(cardSize);
            var grid = TrackGrid(track, settings);

            var idText = new TextBlock
            {
                Text = (index + 1).ToString(),
                Margin = new Thickness(15),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Style = (Style)Application.Current.TryFindResource("SmallInfoPanel"),
            };
            Grid.SetColumn(idText, 0);
            grid.Children.Add(idText);

            var addButton = CreateAddSongButton(listener, track);
            Grid.SetColumn(addButton, 3);
            grid.Children.Add(addButton);

            var border = new Border
            {
                Style = Application.Current.TryFindResource("SimpleFunctionalCard") as Style,
                Margin = new Thickness(0, 0, 5, 5),
                Cursor = Cursors.Hand,
                Child = grid,
                Tag = track
            };

            if (addButton != null)
            {
                border.MouseEnter += (sender, e) => addButton.Visibility = Visibility.Visible;
                border.MouseLeave += (sender, e) => addButton.Visibility = Visibility.Collapsed;
            }

            border.MouseLeftButtonUp += (sender, e) => clickHandler(sender, e);

            return border;
        }

        static Grid TrackGrid(Media.Track track, CardSettings settings)
        {
            var grid = new Grid
            {
                Height = settings.Height,
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = new GridLength(settings.Height) },
                    new ColumnDefinition(),
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Auto },
                }
            };

            var coverBorder = CreateCoverFromUrl(track.CoverUrl, settings);
            Grid.SetColumn(coverBorder, 1);
            grid.Children.Add(coverBorder);

            var infoPanel = CreateInfoPanel(track.Title, track.Artist);
            Grid.SetColumn(infoPanel, 2);
            grid.Children.Add(infoPanel);

            var durationText = new TextBlock
            {
                Text = track.Duration.ToString(),
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 15, 0),
                Style = (Style)Application.Current.TryFindResource("SmallInfoPanel"),
            };
            Grid.SetColumn(durationText, 4);
            grid.Children.Add(durationText);

            return grid;
        }
        #endregion

        #region Add Song Button
        public static Grid CreateAddSongButton(
            Listener listener,
            Media.Track track,
            PlacementMode Placement = PlacementMode.Bottom)
        {
            if (listener == default)
                return default;

            var grid = new Grid()
            {
                Visibility = Visibility.Collapsed,
            };

            var icon = new PackIcon
            {
                Width = 25,
                Height = 25,
                Kind = PackIconKind.PlaylistPlus
            };

            var toggleButton = new ToggleButton
            {
                Height = 25,
                Width = 25,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(15, 0, 10, 0),
                Content = icon,
                Style = Application.Current.TryFindResource("AddSongButton") as Style
            };

            var border = new Border
            {
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(0, 5, 0, 5),
                BorderThickness = new Thickness(1),
                BorderBrush = ColorConvert("#333"),
                Style = Application.Current.TryFindResource("NonFunctionalField2c") as Style,
                MaxHeight = 300
            };

            var playlistsPanel = new StackPanel();

            var scrollViewer = new ScrollViewer
            {
                Content = playlistsPanel,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                MaxHeight = 350
            };

            border.Child = scrollViewer;

            var popup = new Popup
            {
                PlacementTarget = toggleButton,
                Placement = Placement,
                StaysOpen = false,
                AllowsTransparency = true,
                Child = border
            };

            popup.Opened += (s, e) =>
            {
                grid.Visibility = Visibility.Visible;
                RefreshPlaylistsPanel(playlistsPanel, listener, track);
            };
            popup.Closed += (s, e) => grid.Visibility = Visibility.Collapsed;

            var binding = new Binding("IsChecked")
            {
                Source = toggleButton,
                Mode = BindingMode.TwoWay
            };
            popup.SetBinding(Popup.IsOpenProperty, binding);

            grid.Children.Add(toggleButton);
            grid.Children.Add(popup);

            RefreshPlaylistsPanel(playlistsPanel, listener, track);

            return grid;
        }

        static void RefreshPlaylistsPanel(StackPanel playlistsPanel, Listener listener, Media.Track track)
        {
            playlistsPanel.Children.Clear();

            using var context = new DBContext();

            var playlists = context.Playlists
                .Where(p => p.CreatorId == listener.Id)
                .ToList();

            foreach (var playlist in playlists)
            {
                var isSongInPlaylist = playlist.TracksId.Contains(track.Id);

                var playlistIcon = new PackIcon
                {
                    Kind = isSongInPlaylist ? PackIconKind.Check : PackIconKind.Plus,
                    Foreground = isSongInPlaylist ?
                        ColorConvert("#90ee90") :
                        ColorConvert("#eee"),
                    Width = 20,
                    Height = 20,
                    Margin = new Thickness(0, 0, 15, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                };

                var playlistText = new TextBlock
                {
                    Text = playlist.Title,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Style = (Style)Application.Current.TryFindResource("SongNameLowerPanel")
                };

                var playlistContent = new Grid()
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = GridLength.Auto },
                        new ColumnDefinition(),
                    }
                };
                playlistContent.Children.Add(playlistIcon);
                Grid.SetColumn(playlistIcon, 0);
                playlistContent.Children.Add(playlistText);
                Grid.SetColumn(playlistText, 1);

                var playlistButton = new Button
                {
                    Content = playlistContent,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    Padding = new Thickness(10, 5, 10, 5),
                    Style = (Style)Application.Current.TryFindResource("MenuButton"),
                    Background = ColorConvert("#333"),
                    Tag = playlist.Id
                };

                playlistButton.Click += (s, e) =>
                {
                    var playlistId = (int)(s as Button).Tag;

                    bool isCurrentlyInPlaylist = playlistIcon.Kind == PackIconKind.Check;

                    if (isCurrentlyInPlaylist)
                    {
                        DeleteSongFromPlaylist(playlistId, track.Id);
                        playlistIcon.Kind = PackIconKind.Plus;
                        playlistIcon.Foreground = ColorConvert("#eee");
                    }
                    else
                    {
                        AddSongToPlaylist(playlistId, track.Id);
                        playlistIcon.Kind = PackIconKind.Check;
                        playlistIcon.Foreground = ColorConvert("#90ee90");
                    }
                };

                playlistsPanel.Children.Add(playlistButton);
            }
        }

        static void AddSongToPlaylist(int playlistId, string trackId)
        {
            using var context = new DBContext();

            var playlist = context.Playlists.Find(playlistId);

            if (playlist != null)
            {
                playlist.TracksId.Add(trackId);
                context.SaveChanges();
            }
        }

        static void DeleteSongFromPlaylist(int playlistId, string trackId)
        {
            using var context = new DBContext();

            var playlist = context.Playlists.Find(playlistId);

            if (playlist != null)
            {
                playlist.TracksId.Remove(trackId);
                context.SaveChanges();
            }
        }
        #endregion

        #region Collection Cards (Playlist)
        public static Border CreateCollectionCard(
            Playlist playlist,
            Action<object, MouseButtonEventArgs> clickHandler,
            CardSize cardSize = CardSize.Large)
        {
            var settings = GetCardSettings(cardSize);
            var playlistGrid = CollectionGrid(playlist);

            var border = new Border
            {
                Style = (Style)Application.Current.TryFindResource("SimpleFunctionalCard"),
                Margin = new Thickness(0, 0, 5, 5),
                Cursor = Cursors.Hand,
                Child = playlistGrid,
                Tag = playlist
            };
            border.MouseLeftButtonUp += (sender, e) => clickHandler(sender, e);

            return border;
        }

        public static Grid CollectionGrid(
            Playlist playlist,
            CardSize cardSize = CardSize.Large)
        {
            var settings = GetCardSettings(cardSize);

            using var context = new DBContext();

            var playlistData = context.Playlists
                .Where(p => p.Id == playlist.Id)
                .Select(p => new
                {
                    Playlist = p,
                    p.Title,
                    p.Creator,
                    p.Cover
                })
                .FirstOrDefault();

            var title = playlistData?.Title;
            var creator = playlistData?.Creator;
            var cover = playlistData?.Cover;

            var playlistGrid = new Grid
            {
                Height = settings.Height,
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(settings.Height) },
                    new ColumnDefinition()
                }
            };

            var imageGrid = CreateCoverFromFile(cover?.FilePath, settings);
            Grid.SetColumn(imageGrid, 0);
            playlistGrid.Children.Add(imageGrid);

            var infoPanel = CreateInfoPanel(title, $"Плейлист •︎ {creator?.Name}");
            Grid.SetColumn(infoPanel, 1);
            playlistGrid.Children.Add(infoPanel);

            return playlistGrid;
        }
        #endregion

        #region Album Cards
        public static Border CreateAlbumCard(
            Album album,
            Action<object, MouseButtonEventArgs> clickHandler,
            CardSize cardSize = CardSize.Large)
        {
            var settings = GetCardSettings(cardSize);
            var grid = AlbumGrid(album, settings);

            var border = new Border
            {
                Style = Application.Current.TryFindResource("SimpleFunctionalCard") as Style,
                Margin = new Thickness(0, 0, 5, 5),
                Cursor = Cursors.Hand,
                Child = grid,
                Tag = album
            };

            border.MouseLeftButtonUp += (sender, e) => clickHandler(sender, e);

            return border;
        }

        static Grid AlbumGrid(Album album, CardSettings settings)
        {
            var grid = new Grid
            {
                Height = settings.Height,
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(settings.Height) },
                    new ColumnDefinition(),
                }
            };

            var cover = CreateCoverFromUrl(album.CoverUrl, settings);
            Grid.SetColumn(cover, 0);
            grid.Children.Add(cover);

            var infoPanel = CreateInfoPanel(album.Title, $"Альбом • {album.Artist}");
            Grid.SetColumn(infoPanel, 1);
            grid.Children.Add(infoPanel);

            return grid;
        }
        #endregion

        #region Artist Cards
        public static Border CreateArtistCard(
            Artist artist,
            Action<object, MouseButtonEventArgs> clickHandler,
            CardSize cardSize = CardSize.Large)
        {
            var settings = GetCardSettings(cardSize);
            var grid = ArtistGrid(artist, settings);

            var border = new Border
            {
                Style = Application.Current.TryFindResource("SimpleFunctionalCard") as Style,
                Margin = new Thickness(5, 0, 5, 5),
                Cursor = Cursors.Hand,
                Child = grid,
                Tag = artist
            };

            border.MouseLeftButtonUp += (sender, e) => clickHandler(sender, e);

            return border;
        }

        static Grid ArtistGrid(Artist artist, CardSettings settings)
        {
            var grid = new Grid
            {
                Height = settings.Height,
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition(),
                }
            };

            var imageEllipse = CreateArtistPictureFromUrl(artist.ProfileImageUrl, settings);
            Grid.SetColumn(imageEllipse, 0);
            grid.Children.Add(imageEllipse);

            var infoPanel = CreateInfoPanel(artist.Name);
            Grid.SetColumn(infoPanel, 1);
            grid.Children.Add(infoPanel);

            return grid;
        }

        static Ellipse CreateArtistPictureFromUrl(string imageUrl, CardSettings settings)
        {
            ImageSource imageSource;

            if (!string.IsNullOrEmpty(imageUrl))
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(imageUrl);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.DecodePixelWidth = settings.ImageSize * 2;
                    bitmap.EndInit();
                    imageSource = bitmap;
                }
                catch
                {
                    imageSource = DecodePhoto(
                        InitializeData.GetDefaultProfilePicturePath(),
                        settings.ImageSize * 2);
                }
            }
            else
            {
                imageSource = DecodePhoto(
                    InitializeData.GetDefaultProfilePicturePath(),
                    settings.ImageSize * 2);
            }

            return new Ellipse
            {
                Width = settings.ImageSize,
                Height = settings.ImageSize,
                Margin = new Thickness(10, 5, 10, 5),
                Fill = new ImageBrush
                {
                    ImageSource = imageSource,
                    Stretch = Stretch.UniformToFill
                }
            };
        }
        #endregion

        #region User Cards (Admin)
        public static Border CreateUserCard(
            int listenerId,
            Listener listener,
            Action<object, RoutedEventArgs> clickHandler,
            CardSize cardSize = CardSize.Large)
        {
            var settings = GetCardSettings(cardSize);

            using var context = new DBContext();

            var listenerData = context.Listeners
                .Where(l => l.Id == listener.Id)
                .Select(l => new
                {
                    Listener = l,
                    l.Name,
                    l.ProfilePicture
                })
                .FirstOrDefault();

            var listenerGrid = UserGrid(listenerId, listenerData?.Name, listenerData?.ProfilePicture);

            var border = new Border
            {
                Style = (Style)Application.Current.TryFindResource("SimpleFunctionalCard"),
                Margin = new Thickness(5, 0, 5, 5),
                Child = listenerGrid,
            };

            var banButton = CreateBanButton();
            Grid.SetColumn(banButton, 3);
            listenerGrid.Children.Add(banButton);

            if (listener.Status)
            {
                banButton.Foreground = ColorConvert("#a33");
                banButton.Background = ColorConvert("#eee");
                var infoBlock = new TextBlock
                {
                    Text = "Заблокирован",
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(10),
                    Style = Application.Current.TryFindResource("SmallErrorPanel") as Style
                };
                Grid.SetColumn(infoBlock, 2);
                listenerGrid.Children.Add(infoBlock);
            }

            banButton.Tag = listener;
            banButton.Click += (sender, e) => clickHandler(sender, e);

            return border;
        }

        public static Grid UserGrid(
            int id,
            string Name,
            ProfilePicture ProfilePicture,
            CardSize cardSize = CardSize.Small)
        {
            var settings = GetCardSettings(cardSize);

            var listenerGrid = new Grid
            {
                Height = settings.Height,
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition(),
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Auto },
                }
            };

            var idText = new TextBlock()
            {
                Text = (id + 1).ToString(),
                Margin = new Thickness(15, 0, 15, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Style = (Style)Application.Current.TryFindResource("SmallInfoPanel"),
            };
            Grid.SetColumn(idText, 0);
            listenerGrid.Children.Add(idText);

            var imageGrid = CreateArtistPictureFromFile(ProfilePicture?.FilePath, settings);
            Grid.SetColumn(imageGrid, 0);
            listenerGrid.Children.Add(imageGrid);

            var infoPanel = CreateInfoPanel(Name);
            Grid.SetColumn(infoPanel, 1);
            listenerGrid.Children.Add(infoPanel);

            return listenerGrid;
        }

        static Ellipse CreateArtistPictureFromFile(string imagePath, CardSettings settings)
        {
            string path;

            if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
                path = InitializeData.GetDefaultProfilePicturePath();
            else
                path = imagePath;

            var imageSource = DecodePhoto(path, settings.ImageSize * 2);

            return new Ellipse
            {
                Width = settings.ImageSize,
                Height = settings.ImageSize,
                Margin = new Thickness(10, 5, 10, 5),
                Fill = new ImageBrush
                {
                    ImageSource = imageSource,
                    Stretch = Stretch.UniformToFill
                }
            };
        }

        static Button CreateBanButton()
        {
            var icon = new PackIcon
            {
                Width = 25,
                Height = 25,
                Kind = PackIconKind.BlockHelper,
            };

            var buttonStyle = Application.Current.TryFindResource("DeleteButton") as Style;

            var button = new Button
            {
                Height = 35,
                Width = 35,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(15, 0, 15, 0),
                Content = icon,
                Style = buttonStyle,
            };

            return button;
        }
        #endregion

        #region Cover/Image Helpers
        public static Border CreateCoverFromUrl(string imageUrl, CardSettings settings)
        {
            return new Border
            {
                Width = settings.ImageSize,
                Height = settings.ImageSize,
                Margin = new Thickness(5),
                CornerRadius = new CornerRadius(5),
                Background = CreateImageBrushFromUrl(imageUrl)
            };
        }

        static Border CreateCoverFromFile(string filePath, CardSettings settings)
        {
            var imagePath = filePath ?? InitializeData.GetDefaultCoverPath();

            var imageSource = DecodePhoto(imagePath, settings.ImageSize * 2);

            return new Border
            {
                Width = settings.ImageSize,
                Height = settings.ImageSize,
                Margin = new Thickness(5),
                CornerRadius = new CornerRadius(5),
                Background = new ImageBrush
                {
                    ImageSource = imageSource,
                    Stretch = Stretch.UniformToFill
                }
            };
        }

        static ImageBrush CreateImageBrushFromUrl(string imageUrl)
        {
            ImageSource imageSource;

            if (!string.IsNullOrEmpty(imageUrl))
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(imageUrl);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    imageSource = bitmap;
                }
                catch
                {
                    imageSource = GetDefaultCover();
                }
            }
            else
            {
                imageSource = GetDefaultCover();
            }

            return new ImageBrush
            {
                ImageSource = imageSource,
                Stretch = Stretch.UniformToFill
            };
        }

        static ImageSource GetDefaultCover()
        {
            var visual = new DrawingVisual();
            using (var context = visual.RenderOpen())
            {
                context.DrawRectangle(
                    new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                    null,
                    new Rect(0, 0, 100, 100));

                var text = new FormattedText(
                    "♪",
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Segoe UI"),
                    40,
                    new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                    1);

                context.DrawText(text, new Point(30, 25));
            }

            var bitmap = new RenderTargetBitmap(100, 100, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(visual);

            return bitmap;
        }

        public static BitmapImage DecodePhoto(string resourcePath, int containerWidth, double scaleFactor = 1.75)
        {
            var bitmap = new BitmapImage();

            bitmap.BeginInit();
            bitmap.UriSource = new Uri(resourcePath);
            bitmap.DecodePixelWidth = (int)(containerWidth * scaleFactor);
            bitmap.CreateOptions = BitmapCreateOptions.DelayCreation;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();

            return bitmap;
        }
        #endregion

        #region Info Panel
        public static StackPanel CreateInfoPanel(string title, string subtitle = null)
        {
            var panel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 10, 0)
            };

            var titleText = new TextBlock
            {
                Name = "SongTitle",
                Text = title ?? "Unknown",
                Style = (Style)Application.Current.TryFindResource("SmallMainInfoPanel")
            };
            panel.Children.Add(titleText);

            if (!string.IsNullOrEmpty(subtitle))
            {
                var subtitleText = new TextBlock
                {
                    Text = subtitle,
                    Style = (Style)Application.Current.TryFindResource("SmallInfoPanel")
                };
                panel.Children.Add(subtitleText);
            }

            return panel;
        }
        #endregion

        #region Utility Methods
        public static string FormatReleaseDate(string dateString)
        {
            if (DateTime.TryParse(dateString, out var date))
                return date.ToString("d MMM yyyy");
            return dateString ?? "";
        }

        public static string FormatTotalDuration(List<Media.Track> tracks)
        {
            var total = TimeSpan.Zero;
            foreach (var track in tracks)
                total += (TimeSpan)track.Duration;

            return total.TotalHours >= 1
                ? total.ToString(@"h\:mm\:ss")
                : total.ToString(@"mm\:ss");
        }

        public static SolidColorBrush ColorConvert(string hex)
        {
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        }

        public static void SetCardTitleColor(Border card, string hexColor)
        {
            if (card?.Child is Grid grid)
            {
                foreach (var child in grid.Children)
                {
                    if (child is StackPanel panel)
                    {
                        foreach (var panelChild in panel.Children)
                        {
                            if (panelChild is TextBlock textBlock &&
                                textBlock.Name == "SongTitle")
                            {
                                textBlock.Foreground = ColorConvert(hexColor);
                                return;
                            }
                        }
                    }
                }
            }
        }

        public static void NoContent(string message, UIElement parent)
        {
            var infoPanel = new TextBlock()
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 15, 0, 10),
                HorizontalAlignment = HorizontalAlignment.Center,
                Style = Application.Current.TryFindResource("SmallInfoPanel") as Style,
            };

            if (parent is StackPanel stackPanel)
                stackPanel.Children.Add(infoPanel);
        }
        #endregion

        #region Card Settings
        public enum CardSize
        {
            Small,
            Large
        }

        public class CardSettings
        {
            public int Height { get; set; }
            public int ImageSize { get; set; }
        }

        static CardSettings GetCardSettings(CardSize size)
        {
            return size switch
            {
                CardSize.Small => new CardSettings { Height = 55, ImageSize = 45 },
                CardSize.Large => new CardSettings { Height = 80, ImageSize = 70 },
                _ => throw new ArgumentOutOfRangeException(nameof(size))
            };
        }
        #endregion
    }
}