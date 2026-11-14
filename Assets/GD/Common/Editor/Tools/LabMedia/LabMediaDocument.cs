using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace GD
{
    /// <summary>
    /// The kind of image a placeholder asks for.
    /// </summary>
    public enum LabMediaKind
    {
        Screenshot,
        Gif
    }

    /// <summary>
    /// One image in a note or lab: either a placeholder still waiting for its
    /// capture, or an image block that is already in place and can be recaptured.
    /// </summary>
    public class LabMediaItem
    {
        public LabMediaKind Kind;

        /// <summary>The path as written in the Markdown file, for example ../images/labs/lab_7_step_5.png.</summary>
        public string Source;

        /// <summary>The placeholder's description, or the image block's alt text.</summary>
        public string Description;

        /// <summary>The image file on disk.</summary>
        public string FullPath;

        /// <summary>True for a placeholder comment; false for an image block already in place.</summary>
        public bool IsPlaceholder;

        /// <summary>The exact Markdown text this item came from, used to replace it.</summary>
        public string MatchText;

        /// <summary>Where the item starts in the file, so items can be listed in reading order.</summary>
        public int Index;

        /// <summary>The display width of an inserted image block; 0 for a placeholder.</summary>
        public int Width;

        public bool FileExists => File.Exists(FullPath);
        public string FileName => Path.GetFileName(Source);
    }

    /// <summary>
    /// Reads the image placeholders and image blocks in one Markdown file and
    /// replaces a placeholder with its image block once the image is captured.
    /// </summary>
    /// <remarks>
    /// Placeholders take the form
    /// <c>&lt;!-- IMAGE: screenshot "../images/labs/name.png" - description --&gt;</c>,
    /// with <c>gif</c> in place of <c>screenshot</c> for an animated capture.
    /// Placeholders of the form <c>IMAGE: search "..."</c> are for images found
    /// elsewhere, so they are ignored.
    /// </remarks>
    public class LabMediaDocument
    {
        private static readonly Regex PlaceholderPattern = new(
            @"<!--\s*IMAGE:\s*(screenshot|gif)\s+""([^""]+)""\s*-\s*(.*?)\s*-->",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        private static readonly Regex ImageBlockPattern = new(
            @"<p align=""center"">\s*<img src=""([^""]+)"" alt=""([^""]*)"" width=""(\d+)"">\s*</p>",
            RegexOptions.Singleline);

        private static readonly UTF8Encoding Utf8WithoutBom = new(false);

        public string FullPath { get; }
        public IReadOnlyList<LabMediaItem> Items => items;
        public int PlaceholderCount { get; private set; }

        private readonly List<LabMediaItem> items = new();

        public LabMediaDocument(string fullPath)
        {
            FullPath = fullPath;
            Reload();
        }

        /// <summary>
        /// Counts the placeholders in a file without keeping the document.
        /// </summary>
        public static int CountPlaceholders(string fullPath)
        {
            return File.Exists(fullPath) ? PlaceholderPattern.Matches(File.ReadAllText(fullPath)).Count : 0;
        }

        /// <summary>
        /// Re-reads the file, picking up any edits made outside the editor.
        /// </summary>
        public void Reload()
        {
            items.Clear();
            PlaceholderCount = 0;

            if (!File.Exists(FullPath))
                return;

            string text = File.ReadAllText(FullPath);

            foreach (Match match in PlaceholderPattern.Matches(text))
            {
                items.Add(new LabMediaItem
                {
                    Kind = match.Groups[1].Value.Equals("gif", StringComparison.OrdinalIgnoreCase) ? LabMediaKind.Gif : LabMediaKind.Screenshot,
                    Source = match.Groups[2].Value,
                    Description = match.Groups[3].Value,
                    FullPath = ResolvePath(match.Groups[2].Value),
                    IsPlaceholder = true,
                    MatchText = match.Value,
                    Index = match.Index
                });
            }

            PlaceholderCount = items.Count;

            foreach (Match match in ImageBlockPattern.Matches(text))
            {
                string source = match.Groups[1].Value;

                // Web images are not captured in the editor.
                if (source.Contains("://"))
                    continue;

                items.Add(new LabMediaItem
                {
                    Kind = source.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) ? LabMediaKind.Gif : LabMediaKind.Screenshot,
                    Source = source,
                    Description = match.Groups[2].Value,
                    FullPath = ResolvePath(source),
                    IsPlaceholder = false,
                    MatchText = match.Value,
                    Index = match.Index,
                    Width = int.Parse(match.Groups[3].Value)
                });
            }

            items.Sort((a, b) => a.Index.CompareTo(b.Index));
        }

        /// <summary>
        /// Replaces a placeholder with its image block and saves the file.
        /// Returns false if the placeholder is no longer in the file.
        /// </summary>
        public bool ReplacePlaceholder(LabMediaItem item, string altText, int width)
        {
            return item.IsPlaceholder && ReplaceText(item, ImageBlock.Build(item.Source, altText, width));
        }

        /// <summary>
        /// Rewrites an inserted image block with new alt text and width.
        /// Returns false if the block is no longer in the file.
        /// </summary>
        public bool UpdateImageBlock(LabMediaItem item, string altText, int width)
        {
            return !item.IsPlaceholder && ReplaceText(item, ImageBlock.Build(item.Source, altText, width));
        }

        /// <summary>
        /// Turns an inserted image block back into a placeholder, using the
        /// given text as its description. The image file is left on disk.
        /// </summary>
        public bool RevertToPlaceholder(LabMediaItem item, string description)
        {
            if (item.IsPlaceholder)
                return false;

            string kind = item.Kind == LabMediaKind.Gif ? "gif" : "screenshot";

            // "-->" inside the description would end the comment early.
            string safeDescription = ImageBlock.CleanAltText(description).Replace("-->", "- >");

            return ReplaceText(item, $"<!-- IMAGE: {kind} \"{item.Source}\" - {safeDescription} -->");
        }

        private bool ReplaceText(LabMediaItem item, string replacement)
        {
            string text = File.ReadAllText(FullPath);
            int index = text.IndexOf(item.MatchText, StringComparison.Ordinal);

            if (index < 0)
                return false;

            text = text.Substring(0, index) + replacement + text.Substring(index + item.MatchText.Length);

            // Notes and labs are UTF-8 without a byte order mark and use LF line endings.
            File.WriteAllText(FullPath, text, Utf8WithoutBom);
            Reload();
            return true;
        }

        /// <summary>
        /// Finds the placeholder whose image file is the given path.
        /// </summary>
        public LabMediaItem FindPlaceholderFor(string imageFullPath)
        {
            string target = Path.GetFullPath(imageFullPath);

            foreach (LabMediaItem item in items)
            {
                if (item.IsPlaceholder && string.Equals(item.FullPath, target, StringComparison.OrdinalIgnoreCase))
                    return item;
            }

            return null;
        }

        private string ResolvePath(string source)
        {
            string folder = Path.GetDirectoryName(FullPath) ?? string.Empty;
            return Path.GetFullPath(Path.Combine(folder, source.Replace('/', Path.DirectorySeparatorChar)));
        }
    }
}
