namespace Net.Code.Csv.Impl;

internal record struct Option<T>(T Value) where T : class
{
    public bool HasValue => Value is not null;
}
