using System.Text.RegularExpressions;

namespace GD
{
    /// <summary>
    /// Builds the house-style HTML image block used in notes and labs, so every
    /// capture tool writes images in exactly the same form.
    /// </summary>
    public static class ImageBlock
    {
        /// <summary>
        /// Returns the three-line block, without a trailing newline.
        /// </summary>
        /// <param name="source">The image path as the Markdown file sees it, for example ../images/labs/lab_7_step_5.png.</param>
        /// <param name="altText">The description read out by a screen reader.</param>
        /// <param name="width">The display width in pixels.</param>
        public static string Build(string source, string altText, int width)
        {
            return
                "<p align=\"center\">\n" +
                $"  <img src=\"{source}\" alt=\"{CleanAltText(altText)}\" width=\"{width}\">\n" +
                "</p>";
        }

        /// <summary>
        /// Puts alt text on one line and swaps double quotes for single ones,
        /// because a double quote would end the alt attribute early.
        /// </summary>
        public static string CleanAltText(string text)
        {
            return Regex.Replace(text ?? string.Empty, @"\s+", " ").Trim().Replace("\"", "'");
        }
    }
}
