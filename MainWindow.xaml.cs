using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace SCLaunch;

public partial class MainWindow : Window
{
    private const string ConfigFile = "apps.json";
    private const string LangFile = "lang.txt";
    private const string SkipLangFile = "skip_lang.txt";
    private List<AppEntry> _apps = new();
    private List<ReleaseItem> _releases = new();
    private bool _isDownloadMode = false;

    public MainWindow()
    {
        InitializeComponent();
        LoadLang();
        LoadConfig();
        AppsListBox.SelectionChanged += AppsListBox_SelectionChanged;
        VersionList.PreviewMouseLeftButtonDown += VersionList_PreviewClick;
        VersionList.PreviewMouseWheel += (s, e) =>
        {
            e.Handled = true;
            var sv = FindChild<ScrollViewer>(VersionList);
            if (sv == null) return;
            sv.ScrollToVerticalOffset(sv.VerticalOffset - e.Delta * 0.15);
        };

        var baseDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
        var idx = baseDir.IndexOf("\\bin\\", StringComparison.OrdinalIgnoreCase);
        var rootDir = idx >= 0 ? baseDir.Substring(0, idx) : baseDir;
        var downloadDir = Path.Combine(rootDir, "Download");
        if (!Directory.Exists(downloadDir))
            Directory.CreateDirectory(downloadDir);
        if (string.IsNullOrEmpty(_selectedSavePath))
            _selectedSavePath = downloadDir;
        SavePathBlock.Text = _selectedSavePath;
        EnsureSevenZipDll();
        ApplyLang();
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        InfoPanel.Visibility = Visibility.Collapsed;
        HintPanel.Visibility = Visibility.Visible;
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        if (File.Exists(Path.Combine(appDir, SkipLangFile)))
        {
            LangOverlay.Visibility = Visibility.Collapsed;
            return;
        }
        if (Lang.Current == "")
        {
            LangOverlay.Visibility = Visibility.Visible;
        }
    }

    private void LangPick_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string code)
        {
            SaveLang(code);
            ApplyLang();
            var skipPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SkipLangFile);
            if (SkipLangCheck.IsChecked == true)
                File.WriteAllText(skipPath, "1");
            else if (File.Exists(skipPath))
                File.Delete(skipPath);
            LangOverlay.Visibility = Visibility.Collapsed;
        }
    }

    private void LoadLang()
    {
        var langPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LangFile);
        if (File.Exists(langPath))
        {
            var lang = File.ReadAllText(langPath).Trim();
            if (lang is "zh" or "en" or "ru")
            {
                Lang.Set(lang);
                return;
            }
        }
        Lang.Set("");
    }

    private void SaveLang(string lang)
    {
        File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LangFile), lang);
        Lang.Set(lang);
    }

    private void LangButton_Click(object sender, RoutedEventArgs e)
    {
        var skipPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SkipLangFile);
        SkipLangCheck.IsChecked = File.Exists(skipPath);
        LangOverlay.Visibility = Visibility.Visible;
    }

    private void ApplyLang()
    {
        DiscoverButton.Content = Lang.T("discover") + " ▾";
        FriendLinksMenu.Header = Lang.T("friend_links");
        ExtractFileMenu.Header = Lang.T("extract_file");
        AboutMenu.Header = Lang.T("about");
        EditMenuItem.Header = Lang.T("edit");
        DeleteMenuItem.Header = Lang.T("delete");
        AddButton.Content = Lang.T("add_path");
        DownloadVersionTitle.Text = Lang.T("download_version");
        DownloadListToggleBtn.Content = Lang.T("download_list");
        TypeLabel.Text = Lang.T("type");
        SourceLabel.Text = Lang.T("source");
        TypeNet.Content = Lang.T("type_net");
        TypeApi.Content = Lang.T("type_api");
        DownloadTasksTitle.Text = Lang.T("download_tasks");
        OpenFolderBtn.Content = Lang.T("open_folder");
        LocationLabel.Text = Lang.T("location");
        SaveToLabel.Text = Lang.T("save_to");
        SavePathBlock.Text = string.IsNullOrEmpty(_selectedSavePath) ? Lang.T("none_selected") : _selectedSavePath;
        SelectSavePathBtn.Content = Lang.T("select");
        DownloadButton.Content = Lang.T("download");
        CancelButton.Content = Lang.T("cancel");
        OpenBrowserButton.Content = Lang.T("browser");
        LaunchButton.Content = Lang.T("launch");
        ProgressText.Text = Lang.T("ready");
        HintStepText.Text = Lang.T("hint_step");
        HintNoteText.Text = Lang.T("hint_note");
    }

    private static T? FindChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T t) return t;
            var result = FindChild<T>(child);
            if (result != null) return result;
        }
        return null;
    }

    private void VersionList_PreviewClick(object sender, MouseButtonEventArgs e)
    {
        var hit = e.OriginalSource as DependencyObject;
        while (hit != null && !(hit is ListBoxItem) && !(hit is Button))
            hit = VisualTreeHelper.GetParent(hit);
        if (hit is Button) return;
        if (hit is not ListBoxItem lbi) return;

        var release = lbi.DataContext as ReleaseItem;
        if (release == null) return;

        if (VersionList.SelectedItem == release)
        {
            e.Handled = true;
            VersionList.UnselectAll();
            release.ShowAssets = Visibility.Collapsed;
            _lastSelectedRelease = null;
        }
    }

    private ReleaseItem? _lastSelectedRelease;

    private void VersionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_lastSelectedRelease != null)
            _lastSelectedRelease.ShowAssets = Visibility.Collapsed;

        if (VersionList.SelectedItem is ReleaseItem selected)
        {
            selected.ShowAssets = Visibility.Visible;
            _lastSelectedRelease = selected;
        }
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed) DragMove();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void Maximize_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    private void DiscoverButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.ContextMenu != null)
        {
            btn.ContextMenu.IsOpen = true;
        }
    }

    private void FriendLinks_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Window
        {
            Title = Lang.T("links_title"),
            Width = 480,
            Height = 600,
            Background = new SolidColorBrush(Color.FromRgb(0x1a, 0x1a, 0x2e)),
            WindowStyle = WindowStyle.None,
            ResizeMode = ResizeMode.NoResize,
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(32) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var titleBar = new Grid { Background = new SolidColorBrush(Color.FromRgb(0x11, 0x18, 0x27)) };
        titleBar.MouseLeftButtonDown += (s, e2) => dlg.DragMove();
        var title = new TextBlock
        {
            Text = Lang.T("links_title"),
            Foreground = new SolidColorBrush(Color.FromRgb(0x4a, 0xde, 0x80)),
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(10, 0, 0, 0)
        };
        var closeBtn = new Button
        {
            Content = "✕",
            Width = 30,
            Height = 24,
            HorizontalAlignment = HorizontalAlignment.Right,
            Background = Brushes.Transparent,
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            FontSize = 12,
            Cursor = Cursors.Hand
        };
        closeBtn.Click += (s, e2) => dlg.Close();
        var closeBtnStyle = new Style(typeof(Button));
        closeBtnStyle.Triggers.Add(new Trigger { Property = IsMouseOverProperty, Value = true, Setters = { new Setter(BackgroundProperty, new SolidColorBrush(Color.FromRgb(0x60, 0x30, 0x30))) } });
        closeBtn.Style = closeBtnStyle;
        titleBar.Children.Add(title);
        titleBar.Children.Add(closeBtn);
        Grid.SetRow(titleBar, 0);

        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var panel = new StackPanel { Margin = new Thickness(15, 10, 15, 10) };

        void AddLink(string label, string url)
        {
            var link = new TextBlock { Foreground = new SolidColorBrush(Color.FromRgb(0x60, 0xa5, 0xfa)), FontSize = 12, Cursor = Cursors.Hand, Margin = new Thickness(0, 2, 0, 2) };
            link.Inlines.Add(new System.Windows.Documents.Run(url));
            link.MouseLeftButtonDown += (s, e2) => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            link.MouseEnter += (s, e2) => link.Foreground = new SolidColorBrush(Color.FromRgb(0x93, 0xc5, 0xfd));
            link.MouseLeave += (s, e2) => link.Foreground = new SolidColorBrush(Color.FromRgb(0x60, 0xa5, 0xfa));
            panel.Children.Add(link);
        }

        void AddLabel(string text)
        {
            panel.Children.Add(new TextBlock { Text = text, Foreground = new SolidColorBrush(Color.FromRgb(0x4a, 0xde, 0x80)), FontSize = 12, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 10, 0, 4) });
        }

        AddLabel(Lang.T("launcher_repo"));
        AddLink("GitHub", "https://github.com/WE666IPS/WE-SC_Launch");
        AddLink("Gitee", "https://gitee.com/we666ips/we-sc_-launch");

        AddLabel(Lang.T("net_label"));
        AddLink("GitHub", "https://github.com/survivalcraft-net/survivalcraft-net");
        AddLink("Gitee", "https://gitee.com/SC-SPM/SurvivalcraftNet");
        AddLabel(Lang.T("api_label"));
        AddLink("Gitee", "https://gitee.com/SC-SPM/SurvivalcraftAPI");
        AddLabel(Lang.T("versions_col_label"));
        AddLink("CSDN", "https://blog.csdn.net/qq_34830893/article/details/131695021");

        panel.Children.Add(new TextBlock { Margin = new Thickness(0, 10, 0, 4) });
        AddLabel(Lang.T("official"));
        AddLink("kaalus.wordpress.com", "https://kaalus.wordpress.com/");
        AddLabel(Lang.T("community"));
        AddLink("test.suancaixianyu.cn", "https://test.suancaixianyu.cn/");
        AddLabel(Lang.T("sckey"));
        AddLink("sckey.net", "https://www.sckey.net/");
        AddLabel(Lang.T("modsite"));
        AddLink("scmod.cn", "https://www.scmod.cn/");
        AddLabel(Lang.T("global_mods"));
        AddLink("survivalcraft2mods.blogspot.com", "https://survivalcraft2mods.blogspot.com/");
        AddLabel(Lang.T("scdev_label"));
        AddLink("survivalcraft.dev", "https://survivalcraft.dev/");
        AddLabel(Lang.T("sc_network"));
        AddLink("yylmzxc.github.io", "https://yylmzxc.github.io/index.html");
        AddLabel(Lang.T("vk_label"));
        AddLink("vk.com/fans_club_survivalcraft", "https://vk.com/fans_club_survivalcraft");
        AddLabel(Lang.T("tapatalk_label"));
        AddLink("tapatalk.com", "https://www.tapatalk.com/groups/survivalcraft/discussion/all");

        panel.Children.Add(new TextBlock { Margin = new Thickness(0, 15, 0, 4) });
        AddLabel(Lang.T("warn_title"));
        var warn = new TextBlock
        {
            Foreground = new SolidColorBrush(Color.FromRgb(0xfb, 0xbc, 0x05)),
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 2, 0, 6)
        };
        warn.Inlines.Add(new System.Windows.Documents.Run(Lang.T("warn_text")));
        panel.Children.Add(warn);

        var detailLink = new TextBlock { Foreground = new SolidColorBrush(Color.FromRgb(0x60, 0xa5, 0xfa)), FontSize = 11, Cursor = Cursors.Hand, Margin = new Thickness(0, 2, 0, 2) };
        detailLink.Inlines.Add(new System.Windows.Documents.Run(Lang.T("warn_link") + "https://test.suancaixianyu.cn/#/postDetails/1749"));
        detailLink.MouseLeftButtonDown += (s, e2) => Process.Start(new ProcessStartInfo("https://test.suancaixianyu.cn/#/postDetails/1749") { UseShellExecute = true });
        detailLink.MouseEnter += (s, e2) => detailLink.Foreground = new SolidColorBrush(Color.FromRgb(0x93, 0xc5, 0xfd));
        detailLink.MouseLeave += (s, e2) => detailLink.Foreground = new SolidColorBrush(Color.FromRgb(0x60, 0xa5, 0xfa));
        panel.Children.Add(detailLink);

        scroll.Content = panel;
        Grid.SetRow(scroll, 1);
        root.Children.Add(titleBar);
        root.Children.Add(scroll);
        dlg.Content = root;
        dlg.ShowDialog();
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Window
        {
            Title = Lang.T("about"),
            Width = 360, Height = 320,
            Background = new SolidColorBrush(Color.FromRgb(0x1a, 0x1a, 0x2e)),
            WindowStyle = WindowStyle.None, ResizeMode = ResizeMode.NoResize,
            Owner = this, WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(32) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var titleBar = new Grid { Background = new SolidColorBrush(Color.FromRgb(0x11, 0x18, 0x27)) };
        titleBar.MouseLeftButtonDown += (s, ev) => dlg.DragMove();
        var title = new TextBlock
        {
            Text = Lang.T("about"),
            Foreground = new SolidColorBrush(Color.FromRgb(0x4a, 0xde, 0x80)),
            FontSize = 13, FontWeight = FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(10, 0, 0, 0)
        };
        var closeBtn = new Button
        {
            Content = "✕", Width = 30, Height = 24,
            HorizontalAlignment = HorizontalAlignment.Right,
            Background = Brushes.Transparent, Foreground = Brushes.White,
            BorderThickness = new Thickness(0), FontSize = 12, Cursor = Cursors.Hand
        };
        closeBtn.Click += (s, ev) => dlg.Close();
        titleBar.Children.Add(title);
        titleBar.Children.Add(closeBtn);
        Grid.SetRow(titleBar, 0);

        var content = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(20) };

        int aboutClicks = 0;
        content.MouseLeftButtonDown += (s, ev) =>
        {
            aboutClicks++;
            if (aboutClicks == 5)
            {
                aboutClicks = 0;
                var egg = Lang.Current == "zh"
                    ? "非常感谢老K，以及MOD大佬还有庞大的玩家们\n给SC添加了色彩，让SC这个世界不再孤单。\n\n祝大家身体健康，万事如意！\n愿SC社区越来越好，有更多的玩家和开发者\n加入我们，一起创造更多精彩的内容！🎮✨\n\n启动器作者喜欢玩MC，可是没人和我玩啊！\nSC有点小众，更不用说~[狗头]"
                    : Lang.Current == "ru"
                    ? "Большое спасибо OldK, моддерам и всем игрокам,\nкоторые делают SC живым и интересным!\n\nВсем здоровья и удачи! Пусть SC-сообщество\nрастёт и процветает! 🎮✨"
                    : "A huge thanks to OldK, modders, and all the players\nwho make SC a living and vibrant world!\n\nWishing everyone health and good fortune!\nMay the SC community keep growing! 🎮✨";
                MessageBox.Show(egg, "🥚", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        };

        try
        {
            var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.png");
            if (File.Exists(iconPath))
            {
                var bytes = File.ReadAllBytes(iconPath);
                var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                using var ms = new System.IO.MemoryStream(bytes);
                bitmap.BeginInit();
                bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bitmap.StreamSource = ms;
                bitmap.EndInit();
                bitmap.Freeze();
                var logo = new System.Windows.Controls.Image
                {
                    Source = bitmap, Width = 64, Height = 64,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 12),
                    Stretch = System.Windows.Media.Stretch.Uniform
                };
                content.Children.Add(logo);
            }
        }
        catch { }

        var appName = new TextBlock
        {
            Text = "SC Launch", Foreground = new SolidColorBrush(Color.FromRgb(0x4a, 0xde, 0x80)),
            FontSize = 20, FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 5)
        };
        content.Children.Add(appName);

        var version = new TextBlock
        {
            Text = "v1.0-Bate", Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
            FontSize = 12, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 15)
        };
        content.Children.Add(version);

        var desc = new TextBlock
        {
            Text = Lang.Current == "zh" ? "Survivalcraft 启动器\n支持多版本管理、多线程下载、自动解压" :
                   Lang.Current == "ru" ? "Лаунчер Survivalcraft\nМультиверсии, загрузка на 32 потока, автопрокачка" :
                   "A Survivalcraft launcher\nMulti-version, 32-thread downloads, auto-extract",
            Foreground = Brushes.White, FontSize = 12, TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap, HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 15)
        };
        content.Children.Add(desc);

        if (Lang.Current == "zh")
        {
            var qqPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 6) };
            var qqText = new TextBlock
            {
                Text = "QQ 1群：188309534",
                Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
                FontSize = 11, VerticalAlignment = VerticalAlignment.Center, Cursor = Cursors.Hand
            };
            qqText.MouseLeftButtonDown += (s, ev) =>
            {
                Clipboard.SetText("188309534");
                qqText.Text = "已复制!";
                var t = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
                t.Tick += (s2, ev2) => { qqText.Text = "QQ 1群：188309534"; t.Stop(); };
                t.Start();
            };
            qqPanel.Children.Add(qqText);
            var joinBtn = new Button
            {
                Content = "加入", Width = 36, Height = 20, Margin = new Thickness(8, 0, 0, 0),
                Background = new SolidColorBrush(Color.FromRgb(0x29, 0x80, 0xb9)),
                Foreground = Brushes.White, BorderThickness = new Thickness(0),
                FontSize = 10, Cursor = Cursors.Hand, VerticalAlignment = VerticalAlignment.Center
            };
            joinBtn.Click += (s, ev) => Process.Start(new ProcessStartInfo("https://qun.qq.com/universal-share/share?ac=1&authKey=Z2AulGi5fJAAaZj3jWKu9i2WT4meJ299zaNXLDWFBmAN8xlXzhfwB/n%2BOi8wDaRj&busi_data=eyJncm91cENvZGUiOiIxODgzMDk1MzQiLCJ0b2tlbiI6Ik1vekJuMGozQjRuVlVheTNlMjYxYmNhL2JVNjM1eGg1VGJvaHN4amdiRFdBbTcvTXEwd2tja1p2U2ZQWWl2ZisiLCJ1aW4iOiIzMDQxMTYyNjI5In0=&data=yuOZCntype6hRkApkYZfDBpqauF-9dWe35fYAkPk49Q-WCJpK4C5mIcE6UQfoP-XgWUefCZXv5M-7DE7o3fQbajLPS5Q-TK-EXYsCRv3EaE&svctype=5&tempid=h5_group_info") { UseShellExecute = true });
            qqPanel.Children.Add(joinBtn);
            content.Children.Add(qqPanel);
        }

        var credit = new TextBlock
        {
            Text = Lang.Current == "zh" ? "作者: WE" : Lang.Current == "ru" ? "Автор: WE" : "Author: WE",
            Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66)),
            FontSize = 11, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 4)
        };
        content.Children.Add(credit);

        var license = new TextBlock
        {
            Text = "\u00a9 MIT License",
            Foreground = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55)),
            FontSize = 10, HorizontalAlignment = HorizontalAlignment.Center
        };
        content.Children.Add(license);

        Grid.SetRow(content, 1);
        root.Children.Add(titleBar);
        root.Children.Add(content);
        dlg.Content = root;
        dlg.ShowDialog();
    }

    private async void ExtractFile_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "ZIP/7z (*.zip;*.7z)|*.zip;*.7z|*.*|*.*",
            Title = Lang.T("select_folder")
        };
        if (dlg.ShowDialog() == true)
        {
            try
            {
                var ok = await ExtractArchive(dlg.FileName);
                if (ok)
                    MessageBox.Show($"{Lang.T("extract_complete_msg")}\n{Path.GetDirectoryName(dlg.FileName)}", Lang.T("success"), MessageBoxButton.OK, MessageBoxImage.Information);
                else
                    MessageBox.Show(Lang.T("extract_fail_msg"), Lang.T("error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{Lang.T("extract_fail")}：{ex.Message}", Lang.T("error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void Title_Click(object sender, MouseButtonEventArgs e)
    {
        if (Lang.Current != "zh") return;
        var dlg = new Window
        {
            Title = "SC Launch",
            Width = 500, Height = 560,
            Background = new SolidColorBrush(Color.FromRgb(0x1a, 0x1a, 0x2e)),
            WindowStyle = WindowStyle.None, ResizeMode = ResizeMode.NoResize,
            Owner = this, WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        var root = new Grid { Margin = new Thickness(15) };
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(32) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var titleBar = new Grid { Background = new SolidColorBrush(Color.FromRgb(0x11, 0x18, 0x27)) };
        titleBar.MouseLeftButtonDown += (s, ev) => dlg.DragMove();
        var title = new TextBlock
        {
            Text = "SC Launch",
            Foreground = new SolidColorBrush(Color.FromRgb(0x4a, 0xde, 0x80)),
            FontSize = 13, FontWeight = FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(10, 0, 0, 0)
        };
        var closeBtn = new Button
        {
            Content = "✕", Width = 30, Height = 24,
            HorizontalAlignment = HorizontalAlignment.Right,
            Background = Brushes.Transparent, Foreground = Brushes.White,
            BorderThickness = new Thickness(0), FontSize = 12, Cursor = Cursors.Hand
        };
        closeBtn.Click += (s, ev) => dlg.Close();
        titleBar.Children.Add(title);
        titleBar.Children.Add(closeBtn);
        Grid.SetRow(titleBar, 0);

        var content = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };

        var creator = new TextBlock
        {
            Text = "制作人 / Creator",
            Foreground = new SolidColorBrush(Color.FromRgb(0x4a, 0xde, 0x80)),
            FontSize = 13, FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 8)
        };
        content.Children.Add(creator);

        var name = new TextBlock
        {
            Text = "WE",
            Foreground = Brushes.White, FontSize = 14,
            HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 15)
        };
        content.Children.Add(name);

        var qrBorder = new Border
        {
            Background = Brushes.White, Width = 340, Height = 340,
            HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 15),
            CornerRadius = new CornerRadius(8)
        };
        try
        {
            var imgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "donate.png");
            if (File.Exists(imgPath))
            {
                var bytes = File.ReadAllBytes(imgPath);
                var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                using var ms = new System.IO.MemoryStream(bytes);
                bitmap.BeginInit();
                bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bitmap.StreamSource = ms;
                bitmap.EndInit();
                bitmap.Freeze();
                var img = new System.Windows.Controls.Image
                {
                    Source = bitmap,
                    Width = 330, Height = 330,
                    Stretch = System.Windows.Media.Stretch.Uniform
                };
                qrBorder.Child = img;
            }
            else
            {
                qrBorder.Child = new TextBlock
                {
                    Text = "QR Code",
                    Foreground = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)),
                    FontSize = 14, HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
            }
        }
        catch
        {
            qrBorder.Child = new TextBlock
            {
                Text = "QR Code",
                Foreground = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)),
                FontSize = 14, HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }
        content.Children.Add(qrBorder);

        var donateText = new TextBlock
        {
            Text = Lang.Current == "zh" ? "如果觉得好用，请作者喝杯咖啡 ☕" :
                   Lang.Current == "ru" ? "Если понравилось, угостите автора кофе ☕" :
                   "If you like it, buy the author a coffee ☕",
            Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
            FontSize = 11, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 5)
        };
        content.Children.Add(donateText);

        var thanks = new TextBlock
        {
            Text = "感谢支持！/ Thanks! / Спасибо!",
            Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66)),
            FontSize = 10, HorizontalAlignment = HorizontalAlignment.Center
        };
        content.Children.Add(thanks);

        Grid.SetRow(content, 1);
        root.Children.Add(titleBar);
        root.Children.Add(content);
        dlg.Content = root;
        dlg.ShowDialog();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void AppsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isDownloadMode) return;
        if (AppsListBox.SelectedIndex < 0)
        {
            GameNameBlock.Text = "";
            VersionBlock.Text = "";
            RightGameBlock.Text = "";
            RightVersionBlock.Text = "";
            PathBlock.Text = "";
            InfoPanel.Visibility = Visibility.Collapsed;
            HintPanel.Visibility = Visibility.Visible;
            return;
        }
        HintPanel.Visibility = Visibility.Collapsed;
        InfoPanel.Visibility = Visibility.Visible;
        var app = GetSelectedApp();
        if (app == null) return;
        var mainVer = GetMainVersion(app.Path);
        var apiVer = GetApiVersion(app.Path);
        var verType = GetVersionType(app.Path);
        GameNameBlock.Text = app.Name;
        VersionBlock.Text = $"{mainVer}-{verType} {(apiVer.StartsWith("x") || apiVer.StartsWith("26") ? apiVer : "v" + apiVer)}";
        RightGameBlock.Text = app.Name;
        RightVersionBlock.Text = $"{mainVer}-{verType} {(apiVer.StartsWith("x") || apiVer.StartsWith("26") ? apiVer : "v" + apiVer)}";
        PathBlock.Text = app.Path;
    }

    private string GetVersionType(string path)
    {
        var dir = Path.GetDirectoryName(path) ?? "";
        var dirName = Path.GetFileName(dir);
        var exeName = Path.GetFileNameWithoutExtension(path);
        var combined = (dirName + exeName).ToUpper();
        if (combined.Contains("SCNET") || combined.Contains("NET") || combined.Contains("ONLINE") || combined.Contains("MULTIPLAYER"))
            return "Net";
        if (combined.Contains("API") || combined.Contains("MOD") || combined.Contains("PLUGIN"))
            return "API";
        return "API";
    }

    private string GetMainVersion(string path)
    {
        if (!File.Exists(path)) return "?";
        try
        {
            var vi = FileVersionInfo.GetVersionInfo(path);
            var ver = vi.FileVersion ?? vi.ProductVersion ?? "";
            var parts = ver.Split('.');
            return parts.Length >= 2 ? $"{parts[0]}.{parts[1]}" : ver;
        }
        catch { return "?"; }
    }

    private string GetApiVersion(string path)
    {
        var dir = Path.GetDirectoryName(path) ?? "";
        if (string.IsNullOrEmpty(dir)) return GetMainVersion(path);

        var exeName = Path.GetFileNameWithoutExtension(path);

        // 0. Net版本格式(x26.07.01.01,_26.06.19等)
        var netMatch = Regex.Match(exeName, @"[_xX](\d{2}\.\d{2}\.\d{2}(?:\.\d{2})?)");
        if (netMatch.Success) return netMatch.Groups[1].Value;

        // 1. 从exe文件名提取
        var match = Regex.Match(exeName, @"(\d+\.\d+(?:\.\d+)*(?:\.\d+)*)");
        if (match.Success) return match.Value;

        // 2. 从文件夹名提取
        var dirName = Path.GetFileName(dir);
        var netMatch2 = Regex.Match(dirName, @"[_xX](\d{2}\.\d{2}\.\d{2}(?:\.\d{2})?)");
        if (netMatch2.Success) return netMatch2.Groups[1].Value;

        var match2 = Regex.Match(dirName, @"(\d+\.\d+(?:\.\d+)*(?:\.\d+)*)");
        if (match2.Success) return match2.Value;

        // 5. 搜索所有dll
        var dllPath = Path.Combine(dir, "Survivalcraft.dll");
        if (File.Exists(dllPath))
        {
            var ver = ReadFileVersion(dllPath);
            if (ver != null) return ver;
        }

        // 4. 从deps.json获取
        var depsPath = Path.Combine(dir, "Survivalcraft.deps.json");
        if (File.Exists(depsPath))
        {
            var ver = ReadVersionFromDeps(depsPath);
            if (ver != null) return ver;
        }

        // 5. 搜索所有dll
        try
        {
            foreach (var dll in Directory.GetFiles(dir, "*.dll"))
            {
                var vi = FileVersionInfo.GetVersionInfo(dll);
                var dllVer = vi.FileVersion ?? vi.ProductVersion;
                if (!string.IsNullOrEmpty(dllVer) && dllVer != GetMainVersion(path))
                    return dllVer;
            }
        }
        catch { }

        return GetMainVersion(path);
    }

    private string? ReadFileVersion(string filePath)
    {
        try
        {
            var vi = FileVersionInfo.GetVersionInfo(filePath);
            return vi.FileVersion ?? vi.ProductVersion;
        }
        catch { return null; }
    }

    private string? ReadVersionFromDeps(string jsonPath)
    {
        try
        {
            var json = File.ReadAllText(jsonPath);
            var match = Regex.Match(json, @"""Survivalcraft(?:\.API)?""\s*:\s*""(\d+\.\d+(?:\.\d+)*(?:\.\d+)*)""");
            if (match.Success) return match.Groups[1].Value;
            var match2 = Regex.Match(json, @"""version""\s*:\s*""(\d+\.\d+(?:\.\d+)*(?:\.\d+)*)""");
            if (match2.Success) return match2.Groups[1].Value;
        }
        catch { }
        return null;
    }

    private string? ReadVersionFromConfig(string jsonPath)
    {
        try
        {
            var json = File.ReadAllText(jsonPath);
            var match = Regex.Match(json, @"""(\d+\.\d+(?:\.\d+)*(?:\.\d+)*)""");
            if (match.Success) return match.Groups[1].Value;
        }
        catch { }
        return null;
    }

    private AppEntry? GetSelectedApp()
    {
        if (AppsListBox.SelectedItem is ListBoxItem item)
            return item.Tag as AppEntry;
        return null;
    }

    private void LaunchButton_Click(object sender, RoutedEventArgs e)
    {
        var app = GetSelectedApp();
        if (app == null) return;
        StartApp(app.Path);
    }

    private void AppsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        // 所有文件不启动
    }

    private void EditMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var app = GetSelectedApp();
        if (app == null) return;
        var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "EXE (*.exe)|*.exe|*.*|*.*" };
        if (dlg.ShowDialog() == true)
        {
            app.Name = Path.GetFileNameWithoutExtension(dlg.FileName);
            app.Path = dlg.FileName;
            SaveConfig();
            RefreshList();
        }
    }

    private void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var app = GetSelectedApp();
        if (app == null) return;
        _apps.Remove(app);
        SaveConfig();
        RefreshList();
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "EXE (*.exe)|*.exe|*.*|*.*" };
        if (dlg.ShowDialog() == true)
        {
            var folder = Path.GetDirectoryName(dlg.FileName);
            if (folder != null)
            {
                var found = Directory.GetFiles(folder, "*", SearchOption.TopDirectoryOnly)
                    .Concat(Directory.GetDirectories(folder, "*", SearchOption.TopDirectoryOnly))
                    .Any(p => Path.GetFileName(p).IndexOf("Survivalcraft", StringComparison.OrdinalIgnoreCase) >= 0);
                if (!found)
                {
                    MessageBox.Show(Lang.T("not_sc_game"), Lang.T("error"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            _apps.Add(new AppEntry { Name = Path.GetFileNameWithoutExtension(dlg.FileName), Path = dlg.FileName });
            SaveConfig();
            RefreshList();
        }
    }

    private void StartApp(string path)
    {
        if (!File.Exists(path))
        {
            MessageBox.Show($"{Lang.T("error")}\n{path}", Lang.T("error"), MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        try { Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true }); }
        catch (Exception ex) { MessageBox.Show($"{Lang.T("error")}：{ex.Message}", Lang.T("error"), MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private void LoadConfig()
    {
        if (File.Exists(ConfigFile))
        {
            try
            {
                var data = JsonSerializer.Deserialize<ConfigData>(File.ReadAllText(ConfigFile));
                if (data != null)
                {
                    _apps = data.Apps ?? new();
                    _selectedSavePath = data.SavePath ?? "";
                }
                else
                {
                    _apps = new();
                }
            }
            catch
            {
                try { _apps = JsonSerializer.Deserialize<List<AppEntry>>(File.ReadAllText(ConfigFile)) ?? new(); }
                catch { _apps = new(); }
            }
        }
        else { _apps = new(); SaveConfig(); }
        RefreshList();
    }

    private void SaveConfig()
    {
        var data = new ConfigData { Apps = _apps, SavePath = _selectedSavePath };
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigFile, json);
    }

    private void RefreshList()
    {
        AppsListBox.Items.Clear();
        foreach (var app in _apps)
        {
            var mainVer = GetMainVersion(app.Path);
            var apiVer = GetApiVersion(app.Path);
            var verType = GetVersionType(app.Path);
            var item = new ListBoxItem
            {
                Content = new StackPanel
                {
                    Children =
                    {
                        new TextBlock { Text = app.Name, Foreground = Brushes.White, FontSize = 14, FontWeight = FontWeights.Bold },
                        new TextBlock { Text = $"{mainVer}-{verType} {(apiVer.StartsWith("x") || apiVer.StartsWith("26") ? apiVer : "v" + apiVer)}", Foreground = Brushes.Gray, FontSize = 11 }
                    }
                },
                Tag = app
            };
            AppsListBox.Items.Add(item);
        }
    }

    // ===== 下载功能 =====

    private string _selectedSavePath = "";
    private string _selectedType = "Net";
    private string _selectedSource = "GitHub";

    private readonly Dictionary<string, Dictionary<string, string>> _releaseUrls = new()
    {
        ["Net"] = new()
        {
            ["GitHub"] = "https://api.github.com/repos/survivalcraft-net/survivalcraft-net/releases",
            ["Gitee"] = "https://gitee.com/api/v5/repos/SC-SPM/SurvivalcraftNet/releases"
        },
        ["API"] = new()
        {
            ["GitHub"] = "https://api.github.com/repos/survivalcraft-api/survivalcraft-api/releases",
            ["Gitee"] = "https://gitee.com/api/v5/repos/SC-SPM/SurvivalcraftAPI/releases"
        },
        ["原版"] = new()
        {
            ["GitHub"] = "https://api.github.com/repos/0x703060/Survivalcraft/releases",
            ["Gitee"] = ""
        }
    };

    private readonly Dictionary<string, Dictionary<string, string>> _browserUrls = new()
    {
        ["Net"] = new()
        {
            ["GitHub"] = "https://github.com/survivalcraft-net/survivalcraft-net/releases",
            ["Gitee"] = "https://gitee.com/SC-SPM/SurvivalcraftNet/releases"
        },
        ["API"] = new()
        {
            ["GitHub"] = "https://github.com/survivalcraft-api/survivalcraft-api/releases",
            ["Gitee"] = "https://gitee.com/SC-SPM/SurvivalcraftAPI/releases"
        },
        ["原版"] = new()
        {
            ["GitHub"] = "https://github.com/0x703060/Survivalcraft/releases",
            ["Gitee"] = ""
        }
    };

    private void DownloadButton_Click(object sender, RoutedEventArgs e)
    {
        _isDownloadMode = true;
        InfoPanel.Visibility = Visibility.Collapsed;
        HintPanel.Visibility = Visibility.Collapsed;
        InfoBorder.Visibility = Visibility.Collapsed;
        PathPanel.Visibility = Visibility.Collapsed;
        DownloadPanel.Visibility = Visibility.Visible;
        VersionListBox.Visibility = Visibility.Visible;
        SavePathPanel.Visibility = Visibility.Visible;
        SelectSavePathBtn.Visibility = Visibility.Visible;
        DownloadButton.Visibility = Visibility.Collapsed;
        CancelButton.Visibility = Visibility.Visible;
        OpenBrowserButton.Visibility = Visibility.Visible;
        LaunchButton.Visibility = Visibility.Collapsed;
        VersionList.Items.Clear();
        _ = LoadReleases(_selectedType, _selectedSource);
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        SwitchToNormalMode();
    }

    private void SwitchToNormalMode()
    {
        _isDownloadMode = false;
        DownloadPanel.Visibility = Visibility.Collapsed;
        VersionListBox.Visibility = Visibility.Collapsed;
        SavePathPanel.Visibility = Visibility.Collapsed;
        SelectSavePathBtn.Visibility = Visibility.Collapsed;
        DownloadButton.Visibility = Visibility.Visible;
        CancelButton.Visibility = Visibility.Collapsed;
        OpenBrowserButton.Visibility = Visibility.Collapsed;
        LaunchButton.Visibility = Visibility.Visible;
        if (AppsListBox.SelectedIndex < 0)
        {
            InfoPanel.Visibility = Visibility.Collapsed;
            HintPanel.Visibility = Visibility.Visible;
            InfoBorder.Visibility = Visibility.Visible;
            PathPanel.Visibility = Visibility.Visible;
        }
        else
        {
            InfoPanel.Visibility = Visibility.Visible;
            HintPanel.Visibility = Visibility.Collapsed;
            InfoBorder.Visibility = Visibility.Visible;
            PathPanel.Visibility = Visibility.Visible;
        }
    }

    private void TypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isDownloadMode) return;
        if (TypeComboBox.SelectedItem is ComboBoxItem item)
        {
            _selectedType = item.Content.ToString() ?? "Net";
            VersionList.Items.Clear();
            _ = LoadReleases(_selectedType, _selectedSource);
        }
    }

    private void SourceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isDownloadMode) return;
        if (SourceComboBox.SelectedItem is ComboBoxItem item)
        {
            _selectedSource = item.Content.ToString() ?? "GitHub";
            VersionList.Items.Clear();
            _ = LoadReleases(_selectedType, _selectedSource);
        }
    }

    private void OpenBrowserButton_Click(object sender, RoutedEventArgs e)
    {
        if (_browserUrls.TryGetValue(_selectedType, out var sources) &&
            sources.TryGetValue(_selectedSource, out var url) &&
            !string.IsNullOrEmpty(url))
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        else
        {
            MessageBox.Show("该类型暂无可用链接", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void SelectSavePath_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFolderDialog { Title = "选择保存位置" };
        if (dlg.ShowDialog() == true)
        {
            _selectedSavePath = dlg.FolderName;
            SavePathBlock.Text = _selectedSavePath;
            SaveConfig();
        }
    }

    private void DownloadListToggle_Click(object sender, RoutedEventArgs e)
    {
        DownloadTaskPanel.Visibility = DownloadTaskPanel.Visibility == Visibility.Visible
            ? Visibility.Collapsed : Visibility.Visible;
    }

    private async Task LoadReleases(string type, string source)
    {
        VersionList.Items.Clear();
        VersionList.Items.Add(new ReleaseItem { Name = Lang.T("loading") });

        try
        {
            if (!_releaseUrls.TryGetValue(type, out var sources) ||
                !sources.TryGetValue(source, out var url) ||
                string.IsNullOrEmpty(url))
            {
                VersionList.Items.Clear();
                VersionList.Items.Add(new ReleaseItem { Name = "该类型/源暂无数据" });
                return;
            }

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "SC-Launch");
            client.Timeout = TimeSpan.FromSeconds(20);

            var json = await client.GetStringAsync(url);
            var releases = JsonSerializer.Deserialize<List<JsonElement>>(json);

            _releases.Clear();
            VersionList.Items.Clear();

            if (releases == null || releases.Count == 0)
            {
                VersionList.Items.Add(new ReleaseItem { Name = Lang.T("no_versions") });
                return;
            }

            foreach (var r in releases)
            {
                try
                {
                    var tag = r.TryGetProperty("tag_name", out var t) ? t.GetString() ?? "" : "";
                    var name = r.TryGetProperty("name", out var n) ? n.GetString() ?? tag : tag;
                    var date = "";
                    if (r.TryGetProperty("published_at", out var d))
                        date = d.GetString()?.Split('T')[0] ?? "";
                    else if (r.TryGetProperty("created_at", out var d2))
                        date = d2.GetString()?.Split('T')[0] ?? "";

                    var assets = new List<AssetItem>();
                    if (r.TryGetProperty("assets", out var assetsArr))
                    {
                        foreach (var a in assetsArr.EnumerateArray())
                        {
                            var aName = a.TryGetProperty("name", out var an) ? an.GetString() ?? "" : "";
                            var aUrl = a.TryGetProperty("browser_download_url", out var au) ? au.GetString() ?? "" : "";
                            var aSize = "";
                            if (a.TryGetProperty("size", out var asProp))
                            {
                                var bytes = asProp.GetInt64();
                                aSize = bytes > 1048576 ? $"{bytes / 1048576.0:F1} MB" : $"{bytes / 1024.0:F0} KB";
                            }
                            if (!string.IsNullOrEmpty(aName) && !string.IsNullOrEmpty(aUrl))
                                assets.Add(new AssetItem { FileName = aName, DownloadUrl = aUrl, Size = aSize });
                        }
                    }

                    if (!string.IsNullOrEmpty(tag))
                    {
                        var item = new ReleaseItem
                        {
                            Tag = tag,
                            Name = name,
                            Date = date,
                            DateDisplay = date,
                            Assets = assets
                        };
                        _releases.Add(item);
                        VersionList.Items.Add(item);
                    }
                }
                catch { }
            }

            if (VersionList.Items.Count == 0)
                VersionList.Items.Add(new ReleaseItem { Name = Lang.T("no_versions") });
        }
        catch (Exception ex)
        {
            VersionList.Items.Clear();
            VersionList.Items.Add(new ReleaseItem { Name = $"{Lang.T("load_failed")}{ex.Message}" });
        }
    }

    private async void AssetDownload_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn)
        {
            var asset = btn.DataContext as AssetItem;
            if (asset == null) return;
            if (string.IsNullOrEmpty(_selectedSavePath))
            {
                MessageBox.Show("请先选择保存位置", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            await DownloadFile(asset.DownloadUrl, asset.FileName);
        }
    }

    private async Task DownloadFile(string url, string fileName)
    {
        var task = new DownloadTask
        {
            FileName = fileName,
            DownloadUrl = url,
            SavePath = Path.Combine(_selectedSavePath, fileName),
            ProgressText = Lang.T("connecting"),
            SpeedText = ""
        };
        task.Cts = new System.Threading.CancellationTokenSource();

        DownloadTaskPanel.Visibility = Visibility.Visible;
        DownloadTaskList.Items.Add(task);

        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "SC-Launch");
            client.Timeout = TimeSpan.FromHours(2);

            long total = -1;
            bool supportsRange = false;

            using (var probe = new HttpRequestMessage(HttpMethod.Get, url))
            {
                probe.Headers.Range = new RangeHeaderValue(0, 0);
                using var pres = await client.SendAsync(probe, HttpCompletionOption.ResponseHeadersRead, task.Cts.Token);
                supportsRange = pres.StatusCode == HttpStatusCode.PartialContent;
                total = pres.Content.Headers.ContentRange?.Length ?? pres.Content.Headers.ContentLength ?? -1;
            }

            bool parallelOk = false;
            if (total > 256 * 1024)
            {
                parallelOk = await DownloadParallel(client, task, url, total);
            }

            if (!parallelOk)
            {
                await DownloadSingle(client, task, url, total);
            }

            task.Progress = 100;
            task.ProgressText = Lang.T("done");
            task.SpeedText = "";
            task.IsDone = true;

            var ext = Path.GetExtension(task.SavePath).ToLower();
            if (ext == ".zip" || ext == ".7z")
            {
                var result = MessageBox.Show($"{Lang.T("download_complete")}\n\n{Lang.T("extract_prompt")}\n{task.SavePath}", Lang.T("extract_title"),
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    task.ProgressText = Lang.T("extracting");
                    var ok = await ExtractArchive(task.SavePath);
                    task.ProgressText = ok ? Lang.T("extract_done") : Lang.T("extract_fail");
                }
            }
            else
            {
                MessageBox.Show($"{Lang.T("download_complete")}\n{task.SavePath}", Lang.T("success"), MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (OperationCanceledException)
        {
            task.Cts?.Cancel();
            if (File.Exists(task.SavePath))
                File.Delete(task.SavePath);
            if (task.IsStopped)
            {
                task.ProgressText = Lang.T("stopped");
                task.SpeedText = "";
            }
            else
            {
                task.ProgressText = Lang.T("paused");
            }
        }
        catch (Exception ex)
        {
            try { if (File.Exists(task.SavePath)) File.Delete(task.SavePath); } catch { }
            task.ProgressText = Lang.T("failed");
            task.SpeedText = ex.Message;
        }
    }

    private async Task DownloadSingle(HttpClient client, DownloadTask task, string url, long total)
    {
        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, task.Cts.Token);
        response.EnsureSuccessStatusCode();
        if (total < 0) total = response.Content.Headers.ContentLength ?? -1;

        using var stream = await response.Content.ReadAsStreamAsync(task.Cts.Token);
        {
            using var fileStream = File.Create(task.SavePath);
            var buffer = new byte[8192];
            long downloaded = 0;
            int bytesRead;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var speedTimer = System.Diagnostics.Stopwatch.StartNew();

            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, task.Cts.Token)) > 0)
            {
                task.Cts.Token.ThrowIfCancellationRequested();
                if (task.IsPaused)
                {
                    task.ProgressText = Lang.T("paused");
                    while (task.IsPaused && !task.Cts.IsCancellationRequested)
                        await Task.Delay(200, task.Cts.Token);
                    speedTimer.Restart();
                    sw.Restart();
                }
                await fileStream.WriteAsync(buffer, 0, bytesRead, task.Cts.Token);
                downloaded += bytesRead;

                if (total > 0)
                {
                    task.Progress = (double)downloaded / total * 100;
                    task.ProgressText = $"{task.Progress:F0}% ({downloaded / 1048576.0:F1}/{total / 1048576.0:F1} MB)";
                }
                else
                {
                    task.ProgressText = $"{downloaded / 1048576.0:F1} MB";
                }

                if (speedTimer.ElapsedMilliseconds >= 500)
                {
                    var elapsed = sw.Elapsed.TotalSeconds;
                    var speed = downloaded / elapsed;
                    task.SpeedText = speed > 1048576 ? $"{speed / 1048576:F1} MB/s" : $"{speed / 1024:F0} KB/s";
                    speedTimer.Restart();
                }
            }
            await fileStream.FlushAsync();
        }
    }

    private async Task<bool> DownloadParallel(HttpClient client, DownloadTask task, string url, long total)
    {
        const int MaxThreads = 32;
        int threads = Math.Max(1, Math.Min(MaxThreads, (int)(total / (256 * 1024))));
        long chunkSize = total / threads;
        var downloaded = 0L;
        var sw = System.Diagnostics.Stopwatch.StartNew();
        int rangeFailed = 0;

        using var fs = new FileStream(task.SavePath, FileMode.Create, FileAccess.Write, FileShare.Write);
        fs.SetLength(total);

        var chunkTasks = new List<Task>();
        for (int i = 0; i < threads; i++)
        {
            long start = i * chunkSize;
            long end = (i == threads - 1) ? total - 1 : start + chunkSize - 1;
            chunkTasks.Add(Task.Run(async () =>
            {
                try
                {
                    var buf = new byte[65536];
                    var req = new HttpRequestMessage(HttpMethod.Get, url);
                    req.Headers.Range = new RangeHeaderValue(start, end);
                    using var resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, task.Cts.Token);
                    if (resp.StatusCode != HttpStatusCode.PartialContent)
                    {
                        Interlocked.Exchange(ref rangeFailed, 1);
                        return;
                    }
                using var src = await resp.Content.ReadAsStreamAsync(task.Cts.Token);
                long pos = start;
                int read;
                while ((read = await src.ReadAsync(buf, 0, buf.Length, task.Cts.Token)) > 0)
                {
                    task.Cts.Token.ThrowIfCancellationRequested();
                    if (task.IsPaused)
                    {
                        while (task.IsPaused && !task.Cts.IsCancellationRequested)
                            await Task.Delay(200, task.Cts.Token);
                    }
                    lock (fs)
                    {
                        fs.Seek(pos, SeekOrigin.Begin);
                        fs.Write(buf, 0, read);
                    }
                    pos += read;
                    Interlocked.Add(ref downloaded, read);
                    }
                }
                catch { }
            }, task.Cts.Token));
        }

        var progTask = Task.Run(async () =>
        {
            while (!task.Cts.IsCancellationRequested && !task.IsDone)
            {
                await Task.Delay(300, task.Cts.Token);
                if (rangeFailed == 1) break;
                var d = Interlocked.Read(ref downloaded);
                if (total > 0)
                {
                    task.Progress = (double)d / total * 100;
                    task.ProgressText = $"{task.Progress:F0}% ({d / 1048576.0:F1}/{total / 1048576.0:F1} MB) [{threads}线程]";
                }
                else
                {
                    task.ProgressText = $"{d / 1048576.0:F1} MB";
                }
                var elapsed = sw.Elapsed.TotalSeconds;
                if (elapsed > 0)
                    task.SpeedText = d / elapsed > 1048576 ? $"{d / elapsed / 1048576:F1} MB/s" : $"{d / elapsed / 1024:F0} KB/s";
            }
        }, task.Cts.Token);

        try { await Task.WhenAll(chunkTasks); }
        finally { fs.Flush(); progTask.Wait(500); }

        return rangeFailed == 0;
    }

    private void OpenDownloadFolder_Click(object sender, RoutedEventArgs e)
    {
        var path = _selectedSavePath;
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
        {
            path = AppDomain.CurrentDomain.BaseDirectory;
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }
        try
        {
            Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"无法打开文件夹：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void TaskPause_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is DownloadTask task)
        {
            if (task.IsDone || task.IsStopped) return;
            task.IsPaused = !task.IsPaused;
        }
    }

    private void TaskDelete_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is DownloadTask task)
        {
            task.IsStopped = true;
            task.Cts?.Cancel();
            DownloadTaskList.Items.Remove(task);
            if (DownloadTaskList.Items.Count == 0)
                DownloadTaskPanel.Visibility = Visibility.Collapsed;
        }
    }

    private void TaskOpenFolder_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is DownloadTask task)
        {
            var dir = Path.GetDirectoryName(task.SavePath);
            if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                Process.Start(new ProcessStartInfo("explorer.exe", dir) { UseShellExecute = true });
        }
    }

    private async Task<bool> ExtractArchive(string archivePath)
    {
        return await Task.Run(() =>
        {
            for (int attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    if (!File.Exists(archivePath) || new FileInfo(archivePath).Length == 0)
                        return false;

                    var destDir = Path.Combine(Path.GetDirectoryName(archivePath) ?? "", Path.GetFileNameWithoutExtension(archivePath));
                    if (Directory.Exists(destDir))
                        Directory.Delete(destDir, true);
                    Directory.CreateDirectory(destDir);

                    var ext = Path.GetExtension(archivePath).ToLower();
                    if (ext == ".zip")
                    {
                        ZipFile.ExtractToDirectory(archivePath, destDir);
                    }
                    else
                    {
                        EnsureSevenZipDll();
                        using var extractor = new SevenZipExtractor.ArchiveFile(archivePath);
                        extractor.Extract(destDir, true, null);
                    }

                    var count = Directory.GetFiles(destDir, "*", SearchOption.AllDirectories).Length;
                    if (count > 0) return true;
                }
                catch
                {
                    // fall through to retry
                }
                System.Threading.Thread.Sleep(400);
            }
            return false;
        });
    }

    private static void EnsureSevenZipDll()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var target = Path.Combine(baseDir, "7z.dll");
        if (File.Exists(target)) return;
        var sub = Environment.Is64BitProcess ? "x64" : "x86";
        var source = Path.Combine(baseDir, sub, "7z.dll");
        if (File.Exists(source))
            File.Copy(source, target);
    }
}

public class AppEntry
{
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
}

public class ConfigData
{
    public List<AppEntry> Apps { get; set; } = new();
    public string SavePath { get; set; } = "";
}

public class AssetItem
{
    public string FileName { get; set; } = "";
    public string DownloadUrl { get; set; } = "";
    public string Size { get; set; } = "";
}

public class DownloadTask : System.ComponentModel.INotifyPropertyChanged
{
    private double _progress;
    public double Progress { get => _progress; set { _progress = value; Notify(nameof(Progress)); } }

    private string _progressText = "";
    public string ProgressText { get => _progressText; set { _progressText = value; Notify(nameof(ProgressText)); } }

    private string _speedText = "";
    public string SpeedText { get => _speedText; set { _speedText = value; Notify(nameof(SpeedText)); } }

    private bool _isPaused;
    public bool IsPaused { get => _isPaused; set { _isPaused = value; Notify(nameof(PauseText)); } }

    public string PauseText { get => _isPaused ? "▶" : "⏸"; }

    public string FileName { get; set; } = "";
    public string DownloadUrl { get; set; } = "";
    public string SavePath { get; set; } = "";
    public bool IsDone { get; set; }
    public bool IsStopped { get; set; }
    public System.Threading.CancellationTokenSource? Cts { get; set; }

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    private void Notify(string name) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
}

public class ReleaseItem : System.ComponentModel.INotifyPropertyChanged
{
    public string Tag { get; set; } = "";
    public string Name { get; set; } = "";
    public string Date { get; set; } = "";
    public string DateDisplay { get; set; } = "";
    public List<AssetItem> Assets { get; set; } = new();

    private Visibility _showAssets = Visibility.Collapsed;
    public Visibility ShowAssets
    {
        get => _showAssets;
        set { _showAssets = value; PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(ShowAssets))); }
    }

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
}


