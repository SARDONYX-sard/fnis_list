using System;

namespace fnis_list;

/// <summary>
/// Reads a FNIS list file one animation at a time.
/// </summary>
public ref struct FnisListReader
{
    private readonly ReadOnlySpan<char> _source;
    private int _position;
    private int _versionMajor;
    private int _versionMinor;
    /// <summary>
    /// '+' must be follow 's', 'so', '+'.
    /// </summary>
    private bool _canContinueSequence;

    public FnisListReader(ReadOnlySpan<char> source)
    {
        this._source = source;
        this._position = 0;
        this._versionMajor = 0;
        this._versionMinor = 0;
        this._canContinueSequence = false;
    }

    public int VersionMajor => this._versionMajor;
    public int VersionMinor => this._versionMinor;

    public FnisListParseResult<FnisPattern> Read()
    {
        while (true)
        {
            FnisLineParser.SkipWhitespaceAndComments(this._source, ref this._position, this._source.Length);

            if (this._position >= this._source.Length)
            {
                return FnisListParseResult<FnisPattern>.EndOfInput(this._position);
            }

            TextSpan line = this.GetCurrentLine();
            ReadOnlySpan<char> lineText = line.Slice(this._source);

            if (FnisLineParser.IsVersionLine(lineText))
            {
                if (!FnisLineParser.TryParseVersion(lineText, out int major, out int minor))
                {
                    return FnisListParseResult<FnisPattern>.Failure(FnisListParseErrorKind.InvalidSyntax, line.Pos);
                }

                this._versionMajor = major;
                this._versionMinor = minor;

                this._position = FnisLineParser.NextLine(this._source, line.End);

                continue;
            }

            FnisListParseResult<FnisAnimation> result = FnisLineParser.Parse(this._source, line);
            if (result.IsFailure)
            {
                return FnisListParseResult<FnisPattern>.Failure(result.Error, result.Pos);
            }

            FnisAnimation animation = result.Value;
            switch (animation.Type)
            {
                case FnisAnimType.Sequenced:
                case FnisAnimType.SequencedOptimized:
                    this._canContinueSequence = true;
                    break;

                case FnisAnimType.SequencedContinued:
                    if (!this._canContinueSequence)
                    {
                        return FnisListParseResult<FnisPattern>.Failure(FnisListParseErrorKind.InvalidSequence, line.Pos);
                    }
                    this._canContinueSequence = true; // Another '+' may follow this '+'.
                    break;

                default:
                    this._canContinueSequence = false;
                    break;
            }

            this._position = FnisLineParser.NextLine(this._source, line.End);
            return FnisListParseResult<FnisPattern>.Success(FnisPattern.FromAnimation(animation), this._position);
        }
    }

    private TextSpan GetCurrentLine()
    {
        int end = FnisLineParser.FindLineEnd(this._source, this._position);
        return TextSpan.FromRange(this._position, end);
    }
}
