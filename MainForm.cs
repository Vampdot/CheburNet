using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace CheburNet
{
    public partial class MainForm : Form
    {
        private TabControl tabControl;
        private TextBox addressBar;
        private Button backButton, forwardButton, refreshButton, homeButton, newTabButton, bookmarksButton, addBookmarkButton;
        private ToolStrip statusStrip;
        private ToolStripStatusLabel statusLabel;
        private ContextMenuStrip tabContextMenu;
        private Panel navigationPanel;
        private FlowLayoutPanel bookmarksPanel;
        private bool bookmarksVisible = false;

        private Dictionary<TabPage, WebView2> webViews = new Dictionary<TabPage, WebView2>();
        private Dictionary<TabPage, string> tabOriginalTitles = new Dictionary<TabPage, string>();

        // Система закладок
        private List<Bookmark> bookmarks = new List<Bookmark>
        {
            new Bookmark("Paradoxie", "https://www.bing.com/search?q=paradoxie+%D1%81%D0%BB%D0%B8%D0%B2&form=QBLH&sp=-1&lq=0&pq=paradoxie+%D1%81%D0%BB%D0%B8&sc=10-13&qs=n&sk=&cvid=2E72DF0CD08C40E298054360B2DB4DE1"),
            new Bookmark("Казино", "https://yandex.ru/games/app/196898#app-id=196898&catalog-session-uid=catalog-944e1d84-31b9-5531-ae64-2908f1d47dbd-1756204716527-aafd&rtx-reqid=9300877378149549759&pos=%7B%22listType%22%3A%22suggested%22%2C%22tabCategory%22%3A%22search%22%7D&redir-data=%7B%22block%22%3A%22search%22%2C%22block_index%22%3A0%2C%22card%22%3A%22adaptive_recommended_new%22%2C%22col%22%3A0%2C%22first_screen%22%3A1%2C%22page%22%3A%22search%22%2C%22rn%22%3A425659422%2C%22row%22%3A0%2C%22rtx_reqid%22%3A%22498365595423610953%22%2C%22same_block_index%22%3A0%2C%22wrapper%22%3A%22grid-list%22%2C%22request_id%22%3A%221756204719901304-11695168087542297590-ajsn2meeqt2qfon2-BAL%22%2C%22games_request_id%22%3A%221756204719890224-16944581534398280672-balancer-l7leveler-kubr-yp-klg-48-BAL%22%2C%22http_ref%22%3A%22https%253A%252F%252Fyandex.ru%252Fgames%252Fsearch%253Fquery%253D%2525D0%2525BA%2525D0%2525B0%2525D0%2525B7%2525D0%2525B8%2525D0%2525BD%2525D0%2525BE%22%7D&search_query=%D0%BA%D0%B0%D0%B7%D0%B8%D0%BD%D0%BE")
        };

        public MainForm()
        {
            InitializeComponent();
            InitializeBrowser();
        }

        private void InitializeComponent()
        {
            this.Text = "CheburNet";
            this.Size = new Size(1400, 900);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(45, 45, 48);
            this.Font = new Font("Segoe UI", 9);
            this.ForeColor = Color.White;
        }

        private void InitializeBrowser()
        {
            // Создаем панель навигации с темным дизайном
            navigationPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.FromArgb(37, 37, 38),
                Padding = new Padding(10, 8, 10, 8)
            };

            // Стилизованные кнопки навигации
            backButton = CreateStyledButton("←", 5, Color.FromArgb(0, 122, 204));
            backButton.Click += BackButton_Click;

            forwardButton = CreateStyledButton("→", 50, Color.FromArgb(0, 122, 204));
            forwardButton.Click += ForwardButton_Click;

            refreshButton = CreateStyledButton("↻", 95, Color.FromArgb(0, 122, 204));
            refreshButton.Click += RefreshButton_Click;

            homeButton = CreateStyledButton("🏠", 140, Color.FromArgb(0, 122, 204));
            homeButton.Click += HomeButton_Click;

            newTabButton = CreateStyledButton("+", 185, Color.FromArgb(0, 122, 204));
            newTabButton.Click += NewTabButton_Click;

            // Кнопка закладок
            bookmarksButton = CreateStyledButton("⭐", 230, Color.FromArgb(255, 193, 7));
            bookmarksButton.Click += BookmarksButton_Click;

            // Кнопка добавления в закладки
            addBookmarkButton = CreateStyledButton("📌", 275, Color.FromArgb(40, 167, 69));
            addBookmarkButton.Click += AddBookmarkButton_Click;

            // Адресная строка по центру
            addressBar = new TextBox
            {
                Size = new Size(600, 34),
                Font = new Font("Segoe UI", 10),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.White,
                Anchor = AnchorStyles.None // Центрирование
            };

            // Размещаем адресную строку по центру
            int addressBarX = (navigationPanel.Width - addressBar.Width) / 2;
            addressBar.Location = new Point(addressBarX, 8);
            addressBar.KeyPress += AddressBar_KeyPress;

            // Добавляем элементы на панель навигации
            navigationPanel.Controls.AddRange(new Control[]
            {
        backButton, forwardButton, refreshButton, homeButton,
        newTabButton, bookmarksButton, addBookmarkButton, addressBar
            });

            // Создаем панель закладок с центрированием
            bookmarksPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 45,
                BackColor = Color.FromArgb(45, 45, 48),
                Visible = false,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(0, 10, 0, 10)
            };

            // Контейнер для центрирования закладок
            var bookmarksContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(45, 45, 48)
            };

            // Заполняем панель закладок
            InitializeBookmarks();

            // Создаем TabControl для вкладок с темным стилем
            tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Location = new Point(0, 50),
                Size = new Size(1400, 800),
                Appearance = TabAppearance.FlatButtons,
                ItemSize = new Size(180, 30),
                SizeMode = TabSizeMode.Fixed,
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White
            };
            tabControl.SelectedIndexChanged += TabControl_SelectedIndexChanged;
            tabControl.MouseClick += TabControl_MouseClick;

            // Настраиваем стиль вкладок
            tabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabControl.DrawItem += TabControl_DrawItem;

            // Создаем контекстное меню для вкладок
            CreateTabContextMenu();

            // Создаем статус бар
            statusStrip = new ToolStrip
            {
                BackColor = Color.FromArgb(37, 37, 38),
                ForeColor = Color.FromArgb(200, 200, 200)
            };
            statusLabel = new ToolStripStatusLabel
            {
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(200, 200, 200)
            };
            statusStrip.Items.Add(statusLabel);
            statusStrip.Dock = DockStyle.Bottom;

            // Добавляем элементы на форму в правильном порядке
            this.Controls.Add(tabControl);
            this.Controls.Add(bookmarksPanel);
            this.Controls.Add(navigationPanel);
            this.Controls.Add(statusStrip);

            // Создаем первую вкладку
            CreateNewTab();

            // Подписываемся на изменение размера для центрирования
            navigationPanel.Resize += (s, e) => CenterAddressBar();
            this.Resize += (s, e) => CenterAddressBar();
        }

        private void CenterAddressBar()
        {
            if (addressBar != null && navigationPanel != null)
            {
                int addressBarX = (navigationPanel.Width - addressBar.Width) / 2;
                addressBar.Location = new Point(addressBarX, 8);
            }
        }

        private void InitializeBookmarks()
        {
            bookmarksPanel.Controls.Clear();

            foreach (var bookmark in bookmarks)
            {
                var bookmarkPanel = new Panel
                {
                    Size = new Size(140, 35),
                    BackColor = Color.FromArgb(62, 62, 64),
                    Margin = new Padding(5, 0, 5, 0),
                    Cursor = Cursors.Hand
                };

                var bookmarkLabel = new Label
                {
                    Text = bookmark.Name,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9),
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Padding = new Padding(5, 0, 20, 0),
                    Cursor = Cursors.Hand
                };

                var deleteButton = new Button
                {
                    Text = "×",
                    Size = new Size(20, 20),
                    Location = new Point(115, 7),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.Transparent,
                    ForeColor = Color.FromArgb(200, 200, 200),
                    Font = new Font("Arial", 10, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    Tag = bookmark
                };

                deleteButton.FlatAppearance.BorderSize = 0;
                deleteButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(100, 100, 100);
                deleteButton.Click += DeleteBookmarkButton_Click;

                bookmarkLabel.Click += (s, e) => NavigateToUrl(bookmark.Url);
                bookmarkPanel.Click += (s, e) => NavigateToUrl(bookmark.Url);

                bookmarkPanel.Controls.Add(bookmarkLabel);
                bookmarkPanel.Controls.Add(deleteButton);
                bookmarksPanel.Controls.Add(bookmarkPanel);
            }

            // Кнопка скрытия закладок
            var closeBookmarksButton = new Button
            {
                Text = "✕",
                Size = new Size(35, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(62, 62, 64),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(10, 0, 0, 0)
            };

            closeBookmarksButton.FlatAppearance.BorderSize = 0;
            closeBookmarksButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(82, 82, 84);
            closeBookmarksButton.Click += (s, e) => ToggleBookmarks(false);

            bookmarksPanel.Controls.Add(closeBookmarksButton);
        }

        private void DeleteBookmarkButton_Click(object sender, EventArgs e)
        {
            var button = (Button)sender;
            var bookmark = (Bookmark)button.Tag;

            bookmarks.Remove(bookmark);
            InitializeBookmarks();
        }

        private void AddBookmarkButton_Click(object sender, EventArgs e)
        {
            var webView = GetCurrentWebView();
            if (webView != null && webView.CoreWebView2 != null)
            {
                string currentUrl = webView.CoreWebView2.Source;
                string currentTitle = webView.CoreWebView2.DocumentTitle;

                if (!string.IsNullOrEmpty(currentUrl) && currentUrl != "about:blank")
                {
                    using (var dialog = new AddBookmarkForm(currentTitle, currentUrl))
                    {
                        if (dialog.ShowDialog() == DialogResult.OK)
                        {
                            bookmarks.Add(new Bookmark(dialog.BookmarkName, dialog.BookmarkUrl));
                            InitializeBookmarks();
                            MessageBox.Show("Закладка добавлена!", "CheburNet",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Невозможно добавить пустую страницу в закладки",
                        "CheburNet", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void ToggleBookmarks(bool show)
        {
            bookmarksVisible = show;
            bookmarksPanel.Visible = show;

            if (show)
            {
                tabControl.Location = new Point(0, 95);
                tabControl.Height = this.Height - 95 - statusStrip.Height;
            }
            else
            {
                tabControl.Location = new Point(0, 50);
                tabControl.Height = this.Height - 50 - statusStrip.Height;
            }
        }

        private Button CreateStyledButton(string text, int x, Color backColor)
        {
            var button = new Button
            {
                Text = text,
                Size = new Size(40, 34),
                Location = new Point(x, 8),
                FlatStyle = FlatStyle.Flat,
                BackColor = backColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10),
                Cursor = Cursors.Hand
            };

            button.FlatAppearance.BorderSize = 0;

            // Исправлено: предотвращаем отрицательные значения и значения больше 255
            int hoverR = Math.Max(0, Math.Min(255, backColor.R + 20));
            int hoverG = Math.Max(0, Math.Min(255, backColor.G + 20));
            int hoverB = Math.Max(0, Math.Min(255, backColor.B + 20));
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(hoverR, hoverG, hoverB);

            int downR = Math.Max(0, Math.Min(255, backColor.R - 20));
            int downG = Math.Max(0, Math.Min(255, backColor.G - 20));
            int downB = Math.Max(0, Math.Min(255, backColor.B - 20));
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(downR, downG, downB);

            return button;
        }

        private void TabControl_DrawItem(object sender, DrawItemEventArgs e)
        {
            var tabPage = tabControl.TabPages[e.Index];
            var bounds = tabControl.GetTabRect(e.Index);

            // Фон вкладки
            if (e.Index == tabControl.SelectedIndex)
            {
                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(0, 122, 204)), bounds);
            }
            else
            {
                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(62, 62, 64)), bounds);
            }

            // Текст вкладки
            var textBounds = new Rectangle(bounds.X + 5, bounds.Y + 5, bounds.Width - 25, bounds.Height - 10);
            TextRenderer.DrawText(e.Graphics, tabPage.Text, tabPage.Font, textBounds,
                Color.White, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

            // Кнопка закрытия
            var closeButtonBounds = new Rectangle(bounds.Right - 20, bounds.Y + 8, 14, 14);
            e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(150, 150, 150)), closeButtonBounds);

            using (var pen = new Pen(Color.White, 2))
            {
                e.Graphics.DrawLine(pen, closeButtonBounds.Left + 3, closeButtonBounds.Top + 3,
                    closeButtonBounds.Right - 3, closeButtonBounds.Bottom - 3);
                e.Graphics.DrawLine(pen, closeButtonBounds.Right - 3, closeButtonBounds.Top + 3,
                    closeButtonBounds.Left + 3, closeButtonBounds.Bottom - 3);
            }
        }

        private void TabControl_MouseClick(object sender, MouseEventArgs e)
        {
            // Обработка клика по кнопке закрытия вкладки
            for (int i = 0; i < tabControl.TabCount; i++)
            {
                var rect = tabControl.GetTabRect(i);
                var closeButtonRect = new Rectangle(rect.Right - 20, rect.Y + 8, 14, 14);

                if (closeButtonRect.Contains(e.Location))
                {
                    if (tabControl.TabCount > 1)
                    {
                        var tabPage = tabControl.TabPages[i];
                        tabControl.TabPages.Remove(tabPage);
                        webViews.Remove(tabPage);
                        tabOriginalTitles.Remove(tabPage);
                        tabPage.Dispose();
                    }
                    return;
                }
            }

            // Контекстное меню по правому клику
            if (e.Button == MouseButtons.Right)
            {
                for (int i = 0; i < tabControl.TabCount; i++)
                {
                    var rect = tabControl.GetTabRect(i);
                    if (rect.Contains(e.Location))
                    {
                        tabControl.SelectedIndex = i;
                        tabContextMenu.Show(tabControl, e.Location);
                        break;
                    }
                }
            }
        }

        private void CreateTabContextMenu()
{
    tabContextMenu = new ContextMenuStrip();
    tabContextMenu.BackColor = Color.FromArgb(45, 45, 48);
    tabContextMenu.ForeColor = Color.White;

    var closeTabItem = new ToolStripMenuItem("Закрыть вкладку");
    closeTabItem.Click += (s, e) => CloseCurrentTab();

    var closeOtherTabsItem = new ToolStripMenuItem("Закрыть другие вкладки");
    closeOtherTabsItem.Click += (s, e) => CloseOtherTabs();

    var closeAllTabsItem = new ToolStripMenuItem("Закрыть все вкладки");
    closeAllTabsItem.Click += (s, e) => CloseAllTabs();

    var newTabItem = new ToolStripMenuItem("Новая вкладка");
    newTabItem.Click += (s, e) => CreateNewTab();

    tabContextMenu.Items.AddRange(new ToolStripItem[]
    {
        newTabItem,
        new ToolStripSeparator(),
        closeTabItem,
        closeOtherTabsItem,
        closeAllTabsItem
    });

    // Исправлено: проверяем тип элемента перед приведением
    foreach (ToolStripItem item in tabContextMenu.Items)
    {
        item.BackColor = Color.FromArgb(45, 45, 48);
        item.ForeColor = Color.White;
        
        // Для ToolStripMenuItem также меняем цвет выделения
        if (item is ToolStripMenuItem menuItem)
        {
            menuItem.BackColor = Color.FromArgb(45, 45, 48);
            menuItem.ForeColor = Color.White;
        }
    }
}

        private TabPage CreateNewTab(string title = "Новая вкладка", string url = null)
        {
            var tabPage = new TabPage(title);
            var webView = new WebView2
            {
                Dock = DockStyle.Fill
            };

            tabPage.Controls.Add(webView);
            tabControl.TabPages.Add(tabPage);
            tabControl.SelectedTab = tabPage;

            webViews[tabPage] = webView;
            tabOriginalTitles[tabPage] = title;

            InitializeWebViewAsync(webView, tabPage, url);
            return tabPage;
        }

        private async void InitializeWebViewAsync(WebView2 webView, TabPage tabPage, string initialUrl = null)
        {
            try
            {
                await webView.EnsureCoreWebView2Async(null);
                webView.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = true;
                webView.CoreWebView2.Settings.AreDevToolsEnabled = true;

                // ПЕРЕХВАТ НОВЫХ ОКОН - ОТКРЫВАЕМ В НОВОЙ ВКЛАДКЕ
                webView.CoreWebView2.NewWindowRequested += (s, e) =>
                {
                    e.Handled = true;
                    var newTab = CreateNewTab("Новая вкладка", e.Uri);
                    tabControl.SelectedTab = newTab;
                };

                // Подписываемся на события
                webView.CoreWebView2.NavigationStarting += (s, e) => WebView_NavigationStarting(s, e, tabPage);
                webView.CoreWebView2.NavigationCompleted += (s, e) => WebView_NavigationCompleted(s, e, tabPage);
                webView.CoreWebView2.HistoryChanged += (s, e) => WebView_HistoryChanged(s, e, tabPage);
                webView.CoreWebView2.DocumentTitleChanged += (s, e) => WebView_DocumentTitleChanged(s, e, tabPage);

                // Загружаем начальный URL или Bing
                if (!string.IsNullOrEmpty(initialUrl))
                {
                    webView.CoreWebView2.Navigate(initialUrl);
                }
                else
                {
                    NavigateToBing(webView);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации: {ex.Message}");
            }
        }

        private void WebView_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e, TabPage tabPage)
        {
            if (tabControl.SelectedTab == tabPage)
            {
                statusLabel.Text = "Загрузка...";
            }
        }

        private void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e, TabPage tabPage)
        {
            var webView = webViews[tabPage];
            if (tabControl.SelectedTab == tabPage)
            {
                addressBar.Text = webView.CoreWebView2.Source;
                statusLabel.Text = "Готово";
                UpdateNavigationButtons();
            }

            UpdateTabTitle(tabPage, webView.CoreWebView2.DocumentTitle);
        }

        private void WebView_HistoryChanged(object sender, object e, TabPage tabPage)
        {
            if (tabControl.SelectedTab == tabPage)
            {
                UpdateNavigationButtons();
            }
        }

        private void WebView_DocumentTitleChanged(object sender, object e, TabPage tabPage)
        {
            var webView = webViews[tabPage];
            UpdateTabTitle(tabPage, webView.CoreWebView2.DocumentTitle);
        }

        private void UpdateTabTitle(TabPage tabPage, string title)
        {
            if (string.IsNullOrEmpty(title) || title == "about:blank")
            {
                tabPage.Text = tabOriginalTitles[tabPage];
            }
            else
            {
                tabPage.Text = title.Length > 20 ? title.Substring(0, 20) + "..." : title;
            }
            tabControl.Invalidate();
        }

        private void TabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl.SelectedTab != null && webViews.ContainsKey(tabControl.SelectedTab))
            {
                var webView = webViews[tabControl.SelectedTab];
                addressBar.Text = webView.CoreWebView2?.Source ?? "";
                UpdateNavigationButtons();
            }
        }

        private void NavigateToBing(WebView2 webView = null)
        {
            var targetWebView = webView ?? GetCurrentWebView();
            if (targetWebView != null && targetWebView.CoreWebView2 != null)
            {
                targetWebView.CoreWebView2.Navigate("https://www.bing.com");
                addressBar.Text = "https://www.bing.com";
            }
        }

        private void NavigateToUrl(string url)
        {
            var webView = GetCurrentWebView();
            if (webView == null) return;

            if (string.IsNullOrEmpty(url)) return;

            if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            {
                if (!url.Contains(".") && !url.Contains("/"))
                {
                    url = $"https://www.bing.com/search?q={Uri.EscapeDataString(url)}";
                }
                else
                {
                    url = "https://" + url;
                }
            }

            webView.CoreWebView2.Navigate(url);
        }

        private WebView2 GetCurrentWebView()
        {
            if (tabControl.SelectedTab != null && webViews.ContainsKey(tabControl.SelectedTab))
            {
                return webViews[tabControl.SelectedTab];
            }
            return null;
        }

        private void UpdateNavigationButtons()
        {
            var webView = GetCurrentWebView();
            if (webView != null && webView.CoreWebView2 != null)
            {
                backButton.Enabled = webView.CoreWebView2.CanGoBack;
                forwardButton.Enabled = webView.CoreWebView2.CanGoForward;
            }
            else
            {
                backButton.Enabled = false;
                forwardButton.Enabled = false;
            }
        }

        private void BackButton_Click(object sender, EventArgs e)
        {
            var webView = GetCurrentWebView();
            if (webView != null && webView.CoreWebView2.CanGoBack)
                webView.CoreWebView2.GoBack();
        }

        private void ForwardButton_Click(object sender, EventArgs e)
        {
            var webView = GetCurrentWebView();
            if (webView != null && webView.CoreWebView2.CanGoForward)
                webView.CoreWebView2.GoForward();
        }

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            var webView = GetCurrentWebView();
            webView?.CoreWebView2?.Reload();
        }

        private void HomeButton_Click(object sender, EventArgs e)
        {
            NavigateToBing();
        }

        private void NewTabButton_Click(object sender, EventArgs e)
        {
            CreateNewTab();
        }

        private void BookmarksButton_Click(object sender, EventArgs e)
        {
            ToggleBookmarks(!bookmarksVisible);
        }

        private void AddressBar_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                NavigateToUrl(addressBar.Text);
                e.Handled = true;
            }
        }

        private void CloseCurrentTab()
        {
            if (tabControl.TabCount > 1)
            {
                var tabPage = tabControl.SelectedTab;
                tabControl.TabPages.Remove(tabPage);
                webViews.Remove(tabPage);
                tabOriginalTitles.Remove(tabPage);
                tabPage.Dispose();
            }
        }

        private void CloseOtherTabs()
        {
            var currentTab = tabControl.SelectedTab;
            for (int i = tabControl.TabCount - 1; i >= 0; i--)
            {
                if (tabControl.TabPages[i] != currentTab)
                {
                    var tabPage = tabControl.TabPages[i];
                    tabControl.TabPages.Remove(tabPage);
                    webViews.Remove(tabPage);
                    tabOriginalTitles.Remove(tabPage);
                    tabPage.Dispose();
                }
            }
        }

        private void CloseAllTabs()
        {
            while (tabControl.TabCount > 0)
            {
                var tabPage = tabControl.TabPages[0];
                tabControl.TabPages.Remove(tabPage);
                webViews.Remove(tabPage);
                tabOriginalTitles.Remove(tabPage);
                tabPage.Dispose();
            }
            CreateNewTab();
        }
    }

    // Класс для хранения закладок
    public class Bookmark
    {
        public string Name { get; set; }
        public string Url { get; set; }

        public Bookmark(string name, string url)
        {
            Name = name;
            Url = url;
        }
    }

    // Форма для добавления закладок
    public class AddBookmarkForm : Form
    {
        public string BookmarkName { get; private set; }
        public string BookmarkUrl { get; private set; }

        private TextBox nameTextBox;
        private TextBox urlTextBox;

        public AddBookmarkForm(string defaultName, string defaultUrl)
        {
            InitializeForm();
            nameTextBox.Text = defaultName;
            urlTextBox.Text = defaultUrl;
        }

        private void InitializeForm()
        {
            this.Text = "Добавить закладку";
            this.Size = new Size(400, 200);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(45, 45, 48);
            this.ForeColor = Color.White;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var nameLabel = new Label
            {
                Text = "Название:",
                Location = new Point(20, 20),
                Size = new Size(100, 20),
                ForeColor = Color.White
            };

            nameTextBox = new TextBox
            {
                Location = new Point(120, 20),
                Size = new Size(250, 25),
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            var urlLabel = new Label
            {
                Text = "URL:",
                Location = new Point(20, 60),
                Size = new Size(100, 20),
                ForeColor = Color.White
            };

            urlTextBox = new TextBox
            {
                Location = new Point(120, 60),
                Size = new Size(250, 25),
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            var okButton = new Button
            {
                Text = "Добавить",
                Location = new Point(120, 100),
                Size = new Size(120, 30),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK
            };

            var cancelButton = new Button
            {
                Text = "Отмена",
                Location = new Point(250, 100),
                Size = new Size(120, 30),
                BackColor = Color.FromArgb(62, 62, 64),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel
            };

            okButton.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(nameTextBox.Text) || string.IsNullOrWhiteSpace(urlTextBox.Text))
                {
                    MessageBox.Show("Заполните все поля", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    this.DialogResult = DialogResult.None;
                }
                else
                {
                    BookmarkName = nameTextBox.Text;
                    BookmarkUrl = urlTextBox.Text;
                }
            };

            this.Controls.AddRange(new Control[] { nameLabel, nameTextBox, urlLabel, urlTextBox, okButton, cancelButton });
        }
    }
}