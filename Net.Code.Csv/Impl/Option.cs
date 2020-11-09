namespace Net.Code.Csv.Impl
{
    internal struct Option<T> where T : class
    {
        public T Value { get; set; }
        public bool HasValue => Value is not null;
        internal Option(T value)
        {
            Value = value;
        }
    }
}