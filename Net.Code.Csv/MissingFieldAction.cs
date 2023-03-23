namespace Net.Code.Csv;

/// <summary>
/// Drives the behaviour of the CSV parser when a missing field is encountered
/// </summary>
public enum MissingFieldAction
{
    /// <summary>
    /// Consider a missing field as a parse error and throw a <see cref="MissingFieldCsvException"/>
    /// </summary>
    ParseError = 0,
    /// <summary>
    /// Replace the missing field by an empty string
    /// </summary>
    ReplaceByEmpty = 1,
    /// <summary>
    /// Replace the missing field by a null value
    /// </summary>
    ReplaceByNull = 2,
}

public enum EmptyLineAction
{
    /// <summary>
    /// Do nothing. Behaviour will depend on the MissingFieldAction.
    /// </summary>
    None = 0,
    /// <summary>
    /// Skip empty lines
    /// </summary>
    Skip = 1,
    /// <summary>
    /// an empty line represents another result set
    /// </summary>
    NextResult = 2
}