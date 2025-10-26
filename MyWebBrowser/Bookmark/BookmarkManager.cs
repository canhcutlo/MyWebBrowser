using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System;
namespace MyWebBrowser
{
    public class Bookmark
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public string IconUrl { get; set; }
    }

    public class BookmarkManager
    {
        private string bookmarkFile = "bookmarks.json";
        public List<Bookmark> Bookmarks { get; private set; } = new List<Bookmark>();

        public BookmarkManager()
        {
            LoadBookmarks();
        }

        public bool AddBookmark(string title, string url)
        {
            if (Bookmarks.Exists(b => b.Url == url)) return false;
            Bookmarks.Add(new Bookmark { Title = title, Url = url });
            SaveBookmarks();
            return true;
        }

        public void RemoveBookmark(Bookmark bookmark)
        {
            Bookmarks.Remove(bookmark);
            SaveBookmarks();
        }

        public void SaveBookmarks()
        {
            File.WriteAllText(bookmarkFile, JsonSerializer.Serialize(Bookmarks));
        }

        public void LoadBookmarks()
        {
            if (File.Exists(bookmarkFile))
            {
                Bookmarks = JsonSerializer.Deserialize<List<Bookmark>>(File.ReadAllText(bookmarkFile)) ?? new List<Bookmark>();
            }
        }

    }
}