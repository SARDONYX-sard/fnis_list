using System;

namespace FnisList;

/// <summary>
/// Represents a slice of text by its start position and length.
/// </summary>
public struct TextSpan {
    /// <summary>
    /// Gets the start position of this span.
    /// </summary>
    public int Pos;

    /// <summary>
    /// Gets the length of this span.
    /// </summary>
    public int Len;

    /// <summary>
    /// Initializes a new <see cref="TextSpan"/>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// </exception>
    public TextSpan(int pos, int len) {
        if (pos < 0) {
            throw new ArgumentOutOfRangeException(nameof(pos));
        }

        if (len < 0) {
            throw new ArgumentOutOfRangeException(nameof(len));
        }

        this.Pos = pos;
        this.Len = len;
    }

    /// <summary>
    /// Creates a <see cref="ReadOnlySpan{T}"/> from the specified source.
    /// </summary>
    /// <returns>The represented slice of <paramref name="source"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// </exception>
    public ReadOnlySpan<char> Slice(ReadOnlySpan<char> source) {
        return source.Slice(this.Pos, this.Len);
    }

    /// <summary>
    /// Gets the end position of this span.
    /// </summary>
    public int End => this.Pos + this.Len;

    /// <summary>
    /// Returns an empty text span at the specified position.
    /// </summary>
    /// <param name="pos">The start position.</param>
    public static TextSpan Empty(int pos) => new(pos, 0);

    /// <summary>
    /// Creates a text span covering the range from <paramref name="start"/>
    /// to <paramref name="end"/>.
    /// </summary>
    /// <returns>A text span covering the specified range.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// </exception>
    public static TextSpan FromRange(int start, int end) {
        if (start < 0) {
            throw new ArgumentOutOfRangeException(nameof(start));
        }

        if (end < start) {
            throw new ArgumentOutOfRangeException(nameof(end));
        }

        return new TextSpan(start, end - start);
    }

    public override string ToString() => $"[{this.Pos}..{this.End})";
}
