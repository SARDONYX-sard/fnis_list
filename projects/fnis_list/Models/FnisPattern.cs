namespace fnis_list;

public readonly struct FnisPattern
{
    private readonly FnisAnimType _type;
    private readonly FnisAnimation _animation;

    private FnisPattern(FnisAnimType type, FnisAnimation animation)
    {
        this._type = type;
        this._animation = animation;
    }

    internal static FnisPattern FromAnimation(FnisAnimation animation)
    {
        return new FnisPattern(animation.Type, animation);
    }

    public FnisAnimType Type => this._type;
    public FnisAnimation Animation => this._animation;
}
