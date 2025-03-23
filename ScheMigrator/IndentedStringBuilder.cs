using System;
using System.Linq;
using System.Text;

namespace ScheMigrator
{
    /// <summary>
    /// A StringBuilder extension that manages indentation levels automatically.
    /// </summary>
    public class IndentedStringBuilder
    {
        private readonly StringBuilder _builder = new StringBuilder();
        private int _indentLevel = 0;
        private bool _isLineStart = true;
        private readonly string _indentString;

        /// <summary>
        /// Gets the current indentation level.
        /// </summary>
        public int IndentLevel => _indentLevel;

        /// <summary>
        /// Initializes a new instance of the IndentedStringBuilder class.
        /// </summary>
        /// <param name="indentString">The string to use for each level of indentation. Defaults to a tab character.</param>
        public IndentedStringBuilder(string indentString = "\t")
        {
            _indentString = indentString;
        }

        /// <summary>
        /// Increases the indentation level.
        /// </summary>
        /// <returns>The current IndentedStringBuilder instance.</returns>
        public IndentedStringBuilder Indent()
        {
            _indentLevel++;
            return this;
        }

        /// <summary>
        /// Decreases the indentation level.
        /// </summary>
        /// <returns>The current IndentedStringBuilder instance.</returns>
        /// <exception cref="InvalidOperationException">Thrown when trying to decrease below zero indentation.</exception>
        public IndentedStringBuilder Unindent()
        {
            if (_indentLevel <= 0)
                throw new InvalidOperationException("Cannot decrease indentation level below zero.");

            _indentLevel--;
            return this;
        }

        /// <summary>
        /// Appends text to the current line, adding indentation if at the start of a line.
        /// </summary>
        /// <param name="value">The string to append.</param>
        /// <returns>The current IndentedStringBuilder instance.</returns>
        public IndentedStringBuilder Append(string value)
        {
            // Process the input string one char at a time to handle newlines correctly
            foreach (char c in value)
            {
                // Insert indentation if we're at the start of a line
                if (_isLineStart && _indentLevel > 0)
                {
                    _builder.Append(string.Concat(Enumerable.Repeat(_indentString, _indentLevel)));
                    _isLineStart = false;
                }

                _builder.Append(c);

                // If we just appended a newline, mark that the next character will be at the start of a line
                if (c == '\n')
                {
                    _isLineStart = true;
                }
            }

            return this;
        }

        /// <summary>
        /// Appends text followed by a line terminator, adding indentation at the start of the next line.
        /// </summary>
        /// <param name="value">The string to append.</param>
        /// <returns>The current IndentedStringBuilder instance.</returns>
        public IndentedStringBuilder AppendLine(string value = "")
        {
            Append(value);
            _builder.AppendLine();
            _isLineStart = true;
            return this;
        }

        /// <summary>
        /// Returns the string that has been built.
        /// </summary>
        /// <returns>The built string.</returns>
        public override string ToString() => _builder.ToString();

        /// <summary>
        /// Clears the current contents of the IndentedStringBuilder.
        /// </summary>
        /// <returns>The current IndentedStringBuilder instance.</returns>
        public IndentedStringBuilder Clear()
        {
            _builder.Clear();
            _isLineStart = true;
            return this;
        }
    }
}